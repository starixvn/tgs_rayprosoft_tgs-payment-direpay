using Tgs.Payment.Clients;
using Tgs.Payment.DirePay.Client;

namespace Tgs.Payment.DirePay;
public record WithdrawForm(
      Guid Id,
      string? UserId,
      UserType UserType,
      Guid BrandId,
      Guid? BrandPaymentId,
      decimal Amount,
      PaymentFlowState Status,
      Guid BankInfoId,
      string? BankInfo,
      DirePayWithdrawApplyInfo? ApplyInfo,
      string? PaymentReturn = null);