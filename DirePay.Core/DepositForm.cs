using Tgs.Payment.Clients;
using Tgs.Payment.DirePay.Client;

namespace Tgs.Payment.DirePay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    DirePayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);