namespace Tgs.Payment.DirePay.Client;
public record DirePayDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string Currency,
    string Method,
    string? AuthCode);