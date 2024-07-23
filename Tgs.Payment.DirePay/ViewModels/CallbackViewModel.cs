using System.Text.Json.Serialization;

namespace Tgs.Payment.DirePay.ViewModels;

public class CallbackViewModel
{
    [JsonPropertyName("actual_amount")]
    public string? ActualAmount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = default!;

    [JsonPropertyName("customer_uid")]
    public string CustomerUid { get; set; } = default!;

    [JsonPropertyName("method")]
    public string Method { get; set; } = default!;

    [JsonPropertyName("request_amount")]
    public string RequestAmount { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("txid")]
    public string TxId { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
}