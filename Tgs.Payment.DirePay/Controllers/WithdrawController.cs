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
public class WithdrawController(ILogger<WithdrawController> logger,
                            IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
                            IPaymentServiceClient paymentServiceClient,
                            IPaymentClient paymentClient,
                            DirePayHasher hasher) : ControllerBase
{
    [Consumes("application/json")]
    [Produces("application/json")]
    [HttpPost("callback")]
    public async Task<bool> WithdrawCallbackAsync(
            [FromServices] TgsLogHandler tgsLog,
            [FromHeader(Name = "signature")] string signature,
            [FromBody] object source)
    {
        logger.LogInformation("DirePay Withdraw Callback Body:{Source}", source);

        var request = JsonSerializer.Deserialize<CallbackViewModel>(source.ToString()!)!;
        var formId = Guid.Parse(request.TxId);
        var response = await HandleWithdrawCallbackAsync(signature, source, request, formId).ConfigureAwait(false);

        tgsLog.LogApi(HttpContext, source, response, formId);

        return response;
    }

    private async Task<bool> HandleWithdrawCallbackAsync(
        string signature,
        object source,
        CallbackViewModel request,
        Guid formId)
    {
        var withdrawForm = await getService
                    .GetAsync(new(formId), HttpContext.RequestAborted)
                    .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            logger.LogWarning("Can't found withdraw form({Id}).", formId);
            return false;
        }

        var secret = withdrawForm.ApplyInfo!.Settings.Secret;
        var exchangeRate = withdrawForm.ApplyInfo.Settings.ExchangeRate;

        var parameters = source.ToString();

        var hashSource = $"{secret}{parameters}";
        var hash = hasher.ComputeHash(hashSource);

        logger.LogInformation("Compute hash: {Hash}, sign: {Sign}", hash, signature);

        if (signature != hash)
        {
            logger.LogWarning("Sign value invalid. input: {Sign}, compute: {Hash}", signature, hash);
            return false;
        }

        var orderAmount = decimal.Parse(request.RequestAmount);

        var paymentReturn = new DirePayWithdrawReturn()
        {
            OrderAmount = orderAmount,
            FormId = formId,
            Status = request.Status,
            Sign = signature,
            ActualAmount = request.ActualAmount,
            Currency = request.Currency,
            Method = request.Method,
            PlayerCode = request.CustomerUid,
            Type = request.Type,
            AfterExchangeOrderAmount = orderAmount / exchangeRate,
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (paymentReturn.Status)
        {
            case "completed":
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(paymentReturn, DirePayUtility.SerializerOptions)).ConfigureAwait(false);

                    await paymentClient
                        .PaidWithdrawAsync(
                            withdrawForm.Id,
                            withdrawForm.BrandId,
                            HttpContext.RequestAborted)
                        .ConfigureAwait(false);
                }

                break;
            case "rejected":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(paymentReturn, DirePayUtility.SerializerOptions)).ConfigureAwait(false);

                    await paymentClient
                        .RejectWithdrawAsync(withdrawForm.Id, withdrawForm.BrandId)
                        .ConfigureAwait(false);
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