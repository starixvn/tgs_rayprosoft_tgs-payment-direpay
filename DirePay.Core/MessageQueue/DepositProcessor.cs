using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.DirePay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DirePay.MessageQueue;

internal class DepositProcessor(
    DirePayClient direPayClient,
    IOptions<DirePayOptions> optionsAccessor)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly DirePayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<DirePayDepositSettings>(
            data.ApplyInfo,
            DirePayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.RedirectUrl,
            settings.Currency,
            settings.Method,
            settings.PlayerCode,
            settings.PlayerRealName,
            settings.BankCode,
            settings.PhoneNumber);

        var token = await direPayClient
            .GetAuthorizationTokenAsync(settings.Email, settings.Password, cancellationToken)
            .ConfigureAwait(false);

        var depositResponse = await direPayClient
            .DepositAsync(token, settings.Secret, order, cancellationToken)
            .ConfigureAwait(false);

        var applyInfo = new DirePayDepositApplyInfo(
            settings,
            new DirePayDepositResponse(
                depositResponse.Amount,
                Guid.Parse(depositResponse.TxId),
                new Uri(depositResponse.PayUrl),
                depositResponse.Currency,
                depositResponse.Method,
                depositResponse.AuthCode));

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, DirePayUtility.SerializerOptions)
        };
    }
}