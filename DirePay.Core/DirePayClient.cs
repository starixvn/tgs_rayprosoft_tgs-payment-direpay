using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.DirePay;

public class DirePayClient(
    HttpClient httpClient,
    DirePayHasher hasher,
    ILogger<DirePayClient> logger)
{
    public async ValueTask<string> GetAuthorizationTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

        var requestBody = new Dictionary<string, string>
        {
            { "email", email},
            { "password", password }
        };

        var response = await httpClient
            .PostAsJsonAsync($"/api/login", requestBody, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DirePayClient Login response Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Login response can't read.");

        return node["access_token"]!.GetValue<string>();
    }

    public async ValueTask<DirePayClientDepositResponse> DepositAsync(
        string token,
        string secret,
        DepositOrder order,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var source = $"fiat_payment{secret}";
        var hashCode = hasher.ComputeHash(source);

        var requestBody = new Dictionary<string, string?>
        {
            ["command"] = "fiat_payment",
            ["hashCode"] = hashCode,
            ["callback_url"] = order.CallbackUrl.AbsoluteUri,
            ["redirect_url"] = order.RedirectUrl.AbsoluteUri,
            ["currency"] = order.Currency,
            ["method"] = order.Method,
            ["customer_uid"] = order.PlayerCode,
            ["depositor_name"] = order.PlayerRealName,
            ["amount"] = order.Amount.ToString(),
            ["txid"] = order.FormId.ToString(),
            ["bank_code"] = order.BankCode,
            ["phone_number"] = order.Currency == "BDT" ? order.PhoneNumber : ""
        };

        var response = await httpClient
            .PostAsJsonAsync("/api/callBack", requestBody, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DirePayClient Deposit response Body:{body}", body);

        if (response.IsSuccessStatusCode is false)
            throw new ApiFailException(body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Deposit response can't read.");

        return new DirePayClientDepositResponse(
            Amount: decimal.Parse(node["amount"]!.GetValue<string>()),
            Currency: node["currency"]!.GetValue<string>(),
            TxId: node["txid"]!.GetValue<string>(),
            PayUrl: node["pay_url"]!.GetValue<string>(),
            Method: node["method"]!.GetValue<string>(),
            AuthCode: node["auth_code"]?.GetValue<string>());
    }

    public async ValueTask<DirePayClientWithdrawResponse> WithdrawAsync(
        string token,
        string secret,
        WithdrawOrder order,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var source = $"fiat_withdrawal{secret}";
        var hashCode = hasher.ComputeHash(source);

        var requestBody = new Dictionary<string, string?>
        {
            ["command"] = "fiat_withdrawal",
            ["hashCode"] = hashCode,
            ["callback_url"] = order.CallbackUrl.AbsoluteUri,
            ["currency"] = order.Currency,
            ["customer_uid"] = order.PlayerCode,
            ["amount"] = order.Amount.ToString(),
            ["bank_account_name"] = order.BankAccountName,
            ["bank_account_number"] = order.BankAccountNumber,
            ["bank_name"] = order.BankName,
            ["txid"] = order.FormId.ToString(),
            ["bank_code"] = order.BankCode,
        };

        var response = await httpClient
            .PostAsJsonAsync("/api/callBack", requestBody, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DirePayClient Withdraw response Body:{body}", body);

        if (response.IsSuccessStatusCode is false)
            throw new ApiFailException(body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        return new DirePayClientWithdrawResponse(
            TxId: node["txid"]!.GetValue<string>(),
            Amount: decimal.Parse(node["amount"]!.GetValue<string>()),
            Currency: node["currency"]!.GetValue<string>(),
            Method: node["method"]!.GetValue<string>());
    }

    public async ValueTask<DirePayClientTransactionResponse> QueryTransactionAsync(
        string token,
        string secret,
        Guid formId,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var source = $"fiat_payment_status{secret}";
        var hashCode = hasher.ComputeHash(source);

        var requestBody = new Dictionary<string, string>
        {
            { "command", "fiat_payment_status"},
            { "hashCode", hashCode },
            { "txid", formId.ToString() }
        };

        var response = await httpClient
            .PostAsJsonAsync($"/api/callBack", requestBody, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DirePayClient QueryTransaction response Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Transaction response can't read.");

        return new DirePayClientTransactionResponse(
            node["type"]!.GetValue<string>(),
            node["txid"]!.GetValue<string>(),
            node["created_at"]!.GetValue<string>(),
            node["completed_at"]!.GetValue<string>(),
            node["request_amount"]!.GetValue<string>(),
            node["currency"]!.GetValue<string>(),
            node["actual_amount"]?.GetValue<string>(),
            node["status"]!.GetValue<string>(),
            node["method"]!.GetValue<string>());
    }

    public async ValueTask<IReadOnlyDictionary<string, decimal>> QueryBalanceAsync(
        string token,
        string secret,
        CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var source = $"balances{secret}";
        var hashCode = hasher.ComputeHash(source);

        var requestBody = new Dictionary<string, string>
        {
            { "command", "balances"},
            { "hashCode", hashCode }
        };

        var response = await httpClient
            .PostAsJsonAsync($"/api/callBack", requestBody, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DirePayClient QueryBalance response Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Balance response can't read.");

        var dict = new Dictionary<string, decimal>();
        var cryptos = node["crypto"]!;

        foreach (var crypto in cryptos.AsObject())
        {
            dict[crypto.Key] = decimal.Parse(crypto.Value!.GetValue<string>());
        }

        var fiats = node["fiat"]!;
        foreach (var fiat in fiats.AsObject())
        {
            dict[fiat.Key] = decimal.Parse(fiat.Value!.GetValue<string>());
        }

        return dict;
    }
}