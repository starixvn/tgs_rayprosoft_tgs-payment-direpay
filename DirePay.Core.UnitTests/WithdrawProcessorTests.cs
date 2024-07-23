using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.DirePay;
using Tgs.Payment.DirePay.Client;
using Tgs.Payment.DirePay.MessageQueue;
using Tgs.Payment.Protos;

namespace DirePay.Core.UnitTests;
public class WithdrawProcessorTests
{
    [Fact]
    public async Task 代付()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");

        var fakePayClient = new DirePayClient(
            httpClient,
            new DirePayHasher(NullLogger<DirePayHasher>.Instance),            
            NullLogger<DirePayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new DirePayOptions
        {
            CallbackBaseUrl = new Uri("https://localhost"),
            PaymentApiUrl = new Uri("https://localhost"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var sut = new WithdrawProcessor(
            fakePayClient,
            new OptionsWrapper<DirePayOptions>(options),
            fakePaymentClient);

        var formId = Guid.Parse("94182b09-5375-4d21-86ce-9e06492e3cda");
        var email = "email";
        var password = "password";
        var secret = "secret";

        var applySettings = new DirePayWithdrawSettings(
            email,
            password,
            secret,
            formId,
            50000m,
            new Uri("https://localhost"),
            "VND",
            "4ucmqplxb029",
            "testBankAccountName",
            "112233445566",
            "testBackName",
            "acb",
            1m);

        var request = new WithdrawRequest
        {
            Amount = "50000",
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
                    "txid": "42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9",
                    "currency": "VND",
                    "amount": "50000",
                    "method": "online_banking",
                    "status": "success"
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "test",
            request,
            null);

        var applyInfo = JsonSerializer.Deserialize<DirePayWithdrawApplyInfo>(actual.Result, DirePayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }
}
