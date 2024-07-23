using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Tgs.Payment.DirePay;
using Tgs.Payment.DirePay.Client;
using Tgs.Payment.DirePay.MessageQueue;
using Tgs.Payment.Protos;

namespace DirePay.Core.UnitTests;

public class DepositProcessorTests
{
    [Fact]
    public async Task 儲值要求處理()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");

        var fakePayClient = new DirePayClient(
            httpClient,
            new DirePayHasher(NullLogger<DirePayHasher>.Instance),
            NullLogger<DirePayClient>.Instance);

        var options = new DirePayOptions
        {
            CallbackBaseUrl = new Uri("https://localhost"),
            PaymentApiUrl = new Uri("https://localhost"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var formId = Guid.Parse("207da357-79cf-48a6-984d-9eb97b3f027e");
        var email = "email";
        var password = "password";
        var secret = "secret";

        var sut = new DepositProcessor(
            fakePayClient,
            new OptionsWrapper<DirePayOptions>(options));

        var applySettings = new DirePayDepositSettings(
            email,
            password,
            secret,
            formId,
            new Uri("https://localhost"),
            "VND",
            "ewallet",
            "4ucmqplxb029",
            "testRealName",
            "momopay_vn",
            null,
            1m);

        var request = new DepositRequest
        {
            Amount = "100000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, DirePayUtility.SerializerOptions)
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
                    "txid": "207da357-79cf-48a6-984d-9eb97b3f027e",
                    "currency": "VND",
                    "amount": "100000",
                    "pay_url": "https:\/\/cashier.nativepay.co\/cashier\/?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvcmRlcl9ubyI6IjI0MDcyMjA5NDg0MDg2NDAiLCJ0eXBlIjoiY2FzaGllciJ9.A2yC8ZcTP5mm93h6XNWAdWh4lP1ilhVwIbY2BFs5wgk&ui_version=v1.1.3",
                    "method": "ewallet",
                    "status": "success"
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "test",
            request,
            null);

        var applyInfo = JsonSerializer.Deserialize<DirePayDepositApplyInfo>(actual.Result, DirePayUtility.SerializerOptions);

        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new DirePayDepositResponse(
                100000m,
                applySettings.FormId,
                new Uri("https://cashier.nativepay.co/cashier/?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvcmRlcl9ubyI6IjI0MDcyMjA5NDg0MDg2NDAiLCJ0eXBlIjoiY2FzaGllciJ9.A2yC8ZcTP5mm93h6XNWAdWh4lP1ilhVwIbY2BFs5wgk&ui_version=v1.1.3"),
                "VND",
                "ewallet",
                null),
            applyInfo.Response);
    }
}