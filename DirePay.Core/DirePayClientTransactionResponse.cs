namespace Tgs.Payment.DirePay;
public record DirePayClientTransactionResponse(
    string Type,
    string TxId,
    string CreatedAt,
    string CompletedAt,
    string RequestAmount,
    string Currency,
    string? ActualAmount,
    string Status,
    string Method);
