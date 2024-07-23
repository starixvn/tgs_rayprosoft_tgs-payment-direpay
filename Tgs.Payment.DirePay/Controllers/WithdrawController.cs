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
public class WithdrawController(ILogger<WithdrawController> logger) : ControllerBase
{
    [Consumes("application/json")]
    [Produces("application/json")]
    [HttpPost("callback")]
    public async Task<bool> WithdrawCallbackAsync(
      [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
      [FromServices] IPaymentServiceClient paymentServiceClient,
      [FromServices] IPaymentClient paymentClient,
      [FromServices] DirePayHasher hasher,
      [FromBody] object source)
    {
        logger.LogInformation("DirePay Withdraw Callback Body:{source}", source);

        var request = JsonSerializer.Deserialize<CallbackViewModel>(source.ToString()!)!;

        var formId = Guid.Parse(request.TxId);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            logger.LogWarning("Can't found withdraw form({id}).", formId);
            return false;
        }

        var secret = withdrawForm.ApplyInfo!.Settings.Secret;
        var exchangeRate = withdrawForm.ApplyInfo.Settings.ExchangeRate;

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

        var paymentReturn = new DirePayWithdrawReturn()
        {
            OrderAmount = orderAmount,
            FormId = formId,
            Status = request.Status,
            Sign = sign,
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
            case "completed"://完成
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

            case "rejected"://拒絕
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
                break;
        }

        return true;
    }
}