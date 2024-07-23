using System.ComponentModel;

namespace Tgs.Payment.DirePay.Client;

public record DirePayDepositSettings(
    string Email,
    string Password,
    string Secret,
    Guid FormId,
    Uri RedirectUrl,
    string Currency,
    string Method,
    string PlayerCode,
    string PlayerRealName,
    string BankCode,
    string? PhoneNumber,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);