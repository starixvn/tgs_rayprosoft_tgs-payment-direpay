namespace Tgs.Payment.DirePay;
public record DirePayClientDepositResponse(
    decimal Amount,
    string Currency,
    string TxId,
    string PayUrl,
    string Method,
    string? AuthCode);