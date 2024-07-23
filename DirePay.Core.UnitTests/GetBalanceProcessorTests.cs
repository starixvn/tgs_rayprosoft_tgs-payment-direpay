using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.DirePay;
using Tgs.Payment.DirePay.MessageQueue;
using Tgs.Payment.Protos;

namespace DirePay.Core.UnitTests;

public class GetBalanceProcessorTests
{
    [Fact]
    public async Task GetBalanceProcessor_取得商戶餘額()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");

        var fakePayClient = new DirePayClient(
            httpClient,
            new DirePayHasher(NullLogger<DirePayHasher>.Instance),
            NullLogger<DirePayClient>.Instance);

        var sut = new GetBalanceProcessor(fakePayClient);

        var request = new PaymentBalanceRequest
        {
            BrandPaymentSettings = """
                {
                    "email": "email",
                    "password": "password",
                    "secret": "secret",
                    "currency": "VND"
                }
                """
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/login")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "access_token": "token"
                }
                """));

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/callBack")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "crypto": {
                        "ERC20-USDT": "0.000000000000000000",
                        "ERC20-USDC": "0.000000000000000000",
                        "ETH": "0.000000000000000000",
                        "TRC20-USDT": "0.000000000000000000",
                        "TRC20-USDC": "0.000000000000000000",
                        "TRX": "0.000000000000000000",
                        "BTC": "0.000000000000000000",
                        "BSC-BNB": "0.000000000000000000",
                        "BEP20-DOGE": "0.000000000000000000",
                        "BEP20-EOS": "0.000000000000000000",
                        "BEP20-BUSD": "0.000000000000000000",
                        "ERC20-DAI": "0.000000000000000000",
                        "ERC20-SHIB": "0.000000000000000000",
                        "ERC20-BUSD": "0.000000000000000000",
                        "XRP": "0.000000000000000000",
                        "ADA": "0.000000000000000000",
                        "NETWORK-USDT": "0.000000000000000000"
                    },
                    "fiat": {
                        "MYR": "0.00",
                        "THB": "0.00",
                        "IDR": "0.00",
                        "VND": "100000.00",
                        "KRW": "0.00",
                        "BRL": "0.00",
                        "BDT": "0.00",
                        "INR": "0.00"
                    }
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);

        // Assert
        Assert.Equal(100000M, actual.Balance.ToDecimal());
    }
}