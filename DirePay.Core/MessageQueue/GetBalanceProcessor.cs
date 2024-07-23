using System.Text.Json;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.DirePay.MessageQueue;

internal class GetBalanceProcessor(DirePayClient direPayClient)
    : IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
    public async ValueTask<PaymentBalanceResponse> HandleAsync(
        string subject,
        PaymentBalanceRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
            data.BrandPaymentSettings)!;

        var token = await direPayClient
            .GetAuthorizationTokenAsync(settings.Email, settings.Password, cancellationToken)
            .ConfigureAwait(false);

        var result = await direPayClient.QueryBalanceAsync(
            token,
            settings.Secret,
            cancellationToken).ConfigureAwait(false);

        return new PaymentBalanceResponse
        {
            Balance = result[settings.Currency]
        };
    }
}