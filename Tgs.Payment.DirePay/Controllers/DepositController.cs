using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogWarning("Can't found deposit form({id}).", formId);
            return false;
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
            logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return false;
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
            AfterExchangeOrderAmount = orderAmount / exchangeRate,
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case "completed"://完成
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

            case "expired"://付款等待超过截止时间，只适用于付款回调
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
                break;
        }

        return true;
    }
}