namespace Tgs.Payment.DirePay.Client;
public record DirePayDepositReturn
{
    public decimal OrderAmount { get; init; }

    public Guid FormId { get; init; }

    public string Status { get; init; } = default!;

    public string Sign { get; init; } = default!;

    public string? ActualAmount { get; set; }

    public string Currency { get; set; } = default!;

    public string PlayerCode { get; set; } = default!;

    public string Method { get; set; } = default!;

    public string Type { get; set; } = default!;

    public decimal AfterExchangeOrderAmount { get; set; }
}