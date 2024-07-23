using System.ComponentModel;

namespace Tgs.Payment.DirePay.Client;
public record DirePayWithdrawSettings(
    string Email,
    string Password,
    string Secret,
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string Currency,
    string PlayerCode,
    string BankAccountName,
    string BankAccountNumber,
    string BankName,
    string BankCode,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);