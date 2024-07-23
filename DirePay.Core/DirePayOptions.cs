namespace Tgs.Payment.DirePay;
public class DirePayOptions
{
    public required Uri CallbackBaseUrl { get; set; }

    public required Uri PaymentApiUrl { get; set; }

    public required Uri PaymentServiceBaseUrl { get; set; }
}
