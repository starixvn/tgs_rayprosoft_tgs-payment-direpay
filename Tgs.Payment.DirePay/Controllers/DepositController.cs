using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Logging.Extensions;
using Tgs.Payment.Clients;
using Tgs.Payment.DirePay.ApplicationServices;
using Tgs.Payment.DirePay.Client;
using Tgs.Payment.DirePay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.DirePay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController(ILogger<DepositController> logger) : ControllerBase
{
    [Consumes("application/json")]
    [Produces("application/json")]
    [HttpPost("callback")]
    public async Task<bool> DepositCallbackAsync(
      [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
      [FromServices] IPaymentServiceClient paymentServiceClient,
      [FromServices] IPaymentClient paymentClient,
      [FromServices] DirePayHasher hasher,
      [FromBody] object source)
    {
        logger.LogInformation("DirePay Deposit Callback Body:{source}", source);

        var request = JsonSerializer.Deserialize<CallbackViewModel>(source.ToString()!)!;

        var formId = Guid.Parse(request.TxId);
        HttpContext.AddTrackingId(formId);
        HttpContext.AddRequestContent(source.ToString() ?? string.Empty);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogError("Can't found deposit form({id}).", formId);
            return true;
        }

        var secret = depositForm.ApplyInfo!.Settings.Secret;
        var exchangeRate = depositForm.ApplyInfo.Settings.ExchangeRate;

        var sign = HttpContext.Request.Headers["signature"].ToString();

        var parameters = source.ToString();

        var hashSource = $"{secret}{parameters}";
        var hash = hasher.ComputeHash(hashSource);

        logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            logger.LogError("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return true;
        }

        var orderAmount = decimal.Parse(request.RequestAmount);

        var paymentReturn = new DirePayDepositReturn()
        {
            OrderAmount = orderAmount,
            FormId = formId,
            Status = request.Status,
            Sign = sign,
            ActualAmount = request.ActualAmount,
            Currency = request.Currency,
            PlayerCode = request.CustomerUid,
            Method = request.Method,
            Type = request.Type,
            ReceiveAccountName = "DirePay",
            ReceiveBankAccount = depositForm.ApplyInfo?.Settings?.Email ?? "",
            AfterExchangeOrderAmount = orderAmount / exchangeRate,
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case "completed":
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(paymentReturn, DirePayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.AfterExchangeOrderAmount,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
            case "rejected":
            case "expired":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            DirePayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                logger.LogWarning("FormId:{formId}, payment Status:{status}",
                    formId,
                    paymentReturn.Status);
                break;
        }

        HttpContext.AddResponseContent("true");
        return true;
    }
}