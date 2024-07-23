using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.DirePay;

namespace DirePay.Core.UnitTests;

public class DirePayClientTests
{
    [Fact]
    public async Task 授權()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var sut = new DirePayClient(httpClient, signer, NullLogger<DirePayClient>.Instance);

        var email = "email";
        var password = "password";

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/login")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "access_token": "token"
                }
                """));

        var actual = await sut.GetAuthorizationTokenAsync(email, password);

        Assert.Equal("token", actual);
    }

    [Fact]
    public async Task 餘額查詢()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var sut = new DirePayClient(httpClient, signer, NullLogger<DirePayClient>.Instance);

        var token = "token";
        var secret = "secret";

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
                        "VND": "0.00",
                        "KRW": "0.00",
                        "BRL": "0.00",
                        "BDT": "0.00",
                        "INR": "0.00"
                    }
                }
                """));

        var actual = await sut.QueryBalanceAsync(token, secret);

        Assert.Equal(25, actual.Count);
    }

    [Fact]
    public async Task 交易查詢_代收expired()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var sut = new DirePayClient(httpClient, signer, NullLogger<DirePayClient>.Instance);

        var token = "token";
        var secret = "secret";
        var formId = Guid.Parse("1b63fa0f-3bab-4d1d-a06e-b1767baf1785");

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/callBack")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "type": "deposit",
                    "txid": "1b63fa0f-3bab-4d1d-a06e-b1767baf1785",
                    "created_at": "2024-07-22 02:56:05",
                    "completed_at": "2024-07-22 02:56:05",
                    "request_amount": "100000.00000",
                    "currency": "VND",
                    "actual_amount": null,
                    "status": "expired",
                    "method": "ewallet"
                }
                """));

        var actual = await sut.QueryTransactionAsync(token, secret, formId);

        Assert.Equal("expired", actual.Status);
        Assert.Equal("deposit", actual.Type);
    }

    [Fact]
    public async Task 交易查詢_代收pending()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var sut = new DirePayClient(httpClient, signer, NullLogger<DirePayClient>.Instance);

        var token = "token";
        var secret = "secret";
        var formId = Guid.Parse("485935fb-8f70-4d65-ad35-b37724595d0a");

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/callBack")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "type": "deposit",
                    "txid": "485935fb-8f70-4d65-ad35-b37724595d0a",
                    "created_at": "2024-07-22 09:00:44",
                    "completed_at": "2024-07-22 09:00:44",
                    "request_amount": "100000.00000",
                    "currency": "VND",
                    "actual_amount": null,
                    "status": "pending",
                    "method": "ewallet"
                }
                """));

        var actual = await sut.QueryTransactionAsync(token, secret, formId);

        Assert.Equal("pending", actual.Status);
        Assert.Equal("deposit", actual.Type);
    }

    [Fact]
    public async Task 交易查詢_代付pending()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var sut = new DirePayClient(httpClient, signer, NullLogger<DirePayClient>.Instance);

        var token = "token";
        var secret = "secret";
        var formId = Guid.Parse("42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9");

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/callBack")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "type": "withdrawal",
                    "txid": "42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9",
                    "created_at": "2024-07-22 09:28:46",
                    "completed_at": "2024-07-22 09:28:46",
                    "request_amount": "50000.00000",
                    "currency": "VND",
                    "actual_amount": null,
                    "status": "pending",
                    "method": "online_banking"
                }
                """));

        var actual = await sut.QueryTransactionAsync(token, secret, formId);

        Assert.Equal("pending", actual.Status);
        Assert.Equal("withdrawal", actual.Type);
    }

    [Fact]
    public async Task 代收()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var sut = new DirePayClient(httpClient, signer, NullLogger<DirePayClient>.Instance);

        var token = "token";
        var secret = "secret";
        var order = new DepositOrder(
            100000,
            Guid.Parse("207da357-79cf-48a6-984d-9eb97b3f027e"),
            new Uri("https://localhost"),
            new Uri("https://localhost"),
            "VND",
            "ewallet",
            "4ucmqplxb029",
            "testRealName",
            "momopay_vn",
            null);

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/callBack")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "txid": "207da357-79cf-48a6-984d-9eb97b3f027e",
                    "currency": "VND",
                    "amount": "100000",
                    "pay_url": "https:\/\/cashier.nativepay.co\/cashier\/?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvcmRlcl9ubyI6IjI0MDcyMjA5NDg0MDg2NDAiLCJ0eXBlIjoiY2FzaGllciJ9.A2yC8ZcTP5mm93h6XNWAdWh4lP1ilhVwIbY2BFs5wgk&ui_version=v1.1.3",
                    "method": "ewallet",
                    "status": "success"
                }
                """));

        var actual = await sut.DepositAsync(token, secret, order);

        Assert.Equal(
            "https://cashier.nativepay.co/cashier/?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvcmRlcl9ubyI6IjI0MDcyMjA5NDg0MDg2NDAiLCJ0eXBlIjoiY2FzaGllciJ9.A2yC8ZcTP5mm93h6XNWAdWh4lP1ilhVwIbY2BFs5wgk&ui_version=v1.1.3",
            actual.PayUrl);
    }

    [Fact]
    public async Task 代付()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);

        var sut = new DirePayClient(httpClient, signer, NullLogger<DirePayClient>.Instance);

        var token = "token";
        var secret = "secret";
        var order = new WithdrawOrder(
            Guid.Parse("42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9"),
            50000,
            new Uri("https://localhost"),
            "VND",
            "4ucmqplxb029",
            "testBankAccountName",
            "112233445566",
            "testAccountName",
            "acb");

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/callBack")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "txid": "42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9",
                    "currency": "VND",
                    "amount": "50000",
                    "method": "online_banking",
                    "status": "success"
                }
                """));

        var actual = await sut.WithdrawAsync(token, secret, order);

        Assert.Equal(50000m, actual.Amount);
        Assert.Equal("42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9", actual.TxId);
    }
}