namespace Tgs.Payment.DirePay;
public record DepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    Uri RedirectUrl,
    string Currency,
    string Method,
    string PlayerCode,
    string PlayerRealName,
    string BankCode,
    string? PhoneNumber);