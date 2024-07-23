namespace Tgs.Payment.DirePay.Client;
public record DirePayWithdrawResponse(
    Guid FormId,
    decimal Amount,
    string Currency,
    string Method);