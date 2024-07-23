namespace Tgs.Payment.DirePay.Client;
public record DirePayWithdrawApplyInfo(
    DirePayWithdrawSettings Settings,
    DirePayWithdrawResponse Response);