using System.Text.Json.Serialization;

namespace Tgs.Payment.DirePay;
public record BrandPaymentMerchantInfo(
    [property: JsonPropertyName("email")]
    string Email,
    [property: JsonPropertyName("password")]
    string Password,
    [property: JsonPropertyName("secret")]
    string Secret,
    [property: JsonPropertyName("currency")]
    string Currency);