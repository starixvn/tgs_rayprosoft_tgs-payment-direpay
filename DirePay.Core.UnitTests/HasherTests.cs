using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.DirePay;

namespace DirePay.Core.UnitTests;

public class HasherTests
{
    [Fact]
    public void 餘額查詢()
    {
        var secret = "secret";
        var source = $"balances{secret}";

        var sut = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var actual = sut.ComputeHash(source);

        Assert.Equal("4321ca06b58408a8c3d472f07e88a3a0", actual);
    }

    [Fact]
    public void 可用銀行()
    {
        var secret = "secret";
        var source = $"bank_list{secret}";

        var sut = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var actual = sut.ComputeHash(source);

        Assert.Equal("39c9341c665462da4171d98bc32c4f63", actual);
    }

    [Fact]
    public void 代收回調()
    {
        var secret = "secret";
        var callback = """
            {"actual_amount":null,"currency":"VND","customer_uid":"4ucmqplxb029","method":"ewallet","request_amount":"100000.00000","status":"expired","txid":"207da357-79cf-48a6-984d-9eb97b3f027e","type":"deposit"}
            """;
        var source = $"{secret}{callback}";

        var sut = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var actual = sut.ComputeHash(source);

        var expected = "504e72bc92639c718a40443f1f53161d";

        Assert.Equal(expected, actual);
    }
}