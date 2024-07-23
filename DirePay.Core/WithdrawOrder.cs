namespace Tgs.Payment.DirePay;
public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string Currency,
    string PlayerCode,
    string BankAccountName,
    string BankAccountNumber,
    string BankName,
    string BankCode);