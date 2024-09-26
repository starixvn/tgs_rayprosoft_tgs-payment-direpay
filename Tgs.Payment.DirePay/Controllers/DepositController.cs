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
public class DepositController(ILogger<DepositController> logger,
                               IGetService<GetDepositFormQuery, DepositForm?> getService,
                               IPaymentServiceClient paymentServiceClient,
                               IPaymentClient paymentClient,
                               DirePayHasher hasher) : ControllerBase
{
    [Consumes("application/json")]
    [Produces("application/json")]
    [HttpPost("callback")]
    public async Task<bool> DepositCallbackAsync(
      [FromServices] TgsLogHandler tgsLog,
      [FromBody] object source,
      [FromHeader(Name = "signature")] string signature)
    {
        logger.LogInformation("DirePay Deposit Callback Body:{Source}", source);
        var request = JsonSerializer.Deserialize<CallbackViewModel>(source.ToString()!)!;
        var formId = Guid.Parse(request.TxId);
        var response = await HandleDepositCallbackAsync(source, signature, request, formId).ConfigureAwait(false);
        tgsLog.LogApi(HttpContext, source, response, formId);
        return response;
    }

    private async Task<bool> HandleDepositCallbackAsync(
        object source,
        string signature,
        CallbackViewModel request,
        Guid formId)
    {
        var depositForm = await getService
                    .GetAsync(new(formId), HttpContext.RequestAborted)
                    .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogError("Can't found deposit form({Id}).", formId);
            return true;
        }

        var secret = depositForm.ApplyInfo!.Settings.Secret;
        var exchangeRate = depositForm.ApplyInfo.Settings.ExchangeRate;

        var parameters = source.ToString();

        var hashSource = $"{secret}{parameters}";
        var hash = hasher.ComputeHash(hashSource);

        logger.LogInformation("Compute hash: {Hash}, sign: {Sign}", hash, signature);

        if (signature != hash)
        {
            logger.LogError("Sign value invalid. input: {Sign}, compute: {Hash}", signature, hash);
            return true;
        }

        var orderAmount = decimal.Parse(request.RequestAmount);

        var paymentReturn = new DirePayDepositReturn()
        {
            OrderAmount = orderAmount,
            FormId = formId,
            Status = request.Status,
            Sign = signature,
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
                logger.LogWarning("FormId:{FormId}, payment Status:{Status}",
                    formId,
                    paymentReturn.Status);
                break;
        }

        return true;
    }
}