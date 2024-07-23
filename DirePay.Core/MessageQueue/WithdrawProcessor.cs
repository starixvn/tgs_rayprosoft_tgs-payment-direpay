using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.DirePay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DirePay.MessageQueue;

internal class WithdrawProcessor(
    DirePayClient direPayClient,
    IOptions<DirePayOptions> optionsAccessor,
    IPaymentClient paymentClient)
    : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly DirePayOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<DirePayWithdrawSettings>(data.ApplyInfo, DirePayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"),
            settings.Currency,
            settings.PlayerCode,
            settings.BankAccountName,
            settings.BankAccountNumber,
            settings.BankName,
            settings.BankCode);

        var token = await direPayClient
            .GetAuthorizationTokenAsync(settings.Email, settings.Password, cancellationToken)
            .ConfigureAwait(false);

        var withdrawResponse = await direPayClient
            .WithdrawAsync(token, settings.Secret, order, cancellationToken)
            .ConfigureAwait(false);

        var applyInfo = new DirePayWithdrawApplyInfo(
            settings,
            new DirePayWithdrawResponse(Guid.Parse(withdrawResponse.TxId), withdrawResponse.Amount, withdrawResponse.Currency, withdrawResponse.Method));

        await paymentClient
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                settings.FormId,
                withdrawResponse.TxId,
                cancellationToken)
            .ConfigureAwait(false);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, DirePayUtility.SerializerOptions)
        };
    }
}