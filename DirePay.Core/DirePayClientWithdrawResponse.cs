namespace Tgs.Payment.DirePay;
public record DirePayClientWithdrawResponse(
    string TxId,
    decimal Amount,
    string Currency,
    string Method);