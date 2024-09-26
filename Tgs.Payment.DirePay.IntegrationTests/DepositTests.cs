using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.DirePay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.DirePay.IntegrationTests;

public class DepositTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Deposit_TransactionExpired_RejectDeposit()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = DirePayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("207da357-79cf-48a6-984d-9eb97b3f027e");
        var brandId = Guid.NewGuid();
        var secret = "secret";

        var request = """
            {"actual_amount":null,"currency":"VND","customer_uid":"4ucmqplxb029","method":"ewallet","request_amount":"100000.00000","status":"expired","txid":"207da357-79cf-48a6-984d-9eb97b3f027e","type":"deposit"}
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new DirePayDepositApplyInfo(
            new DirePayDepositSettings(
                "email",
                "password",
                secret,
                formId,
                new Uri("https://localhost"),
                "VND",
                "ewallet",
                "4ucmqplxb029",
                "testRealName",
                "momopay_vn",
                null,
                1m),
             new DirePayDepositResponse(
                100000m,
                formId,
                new Uri("https://cashier.nativepay.co/cashier/?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvcmRlcl9ubyI6IjI0MDcyMjA5NDg0MDg2NDAiLCJ0eXBlIjoiY2FzaGllciJ9.A2yC8ZcTP5mm93h6XNWAdWh4lP1ilhVwIbY2BFs5wgk&ui_version=v1.1.3"),
                "VND",
                "ewallet",
                null));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 800m,
                ThirdPartyOrderNo = "207da357-79cf-48a6-984d-9eb97b3f027e",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DirePayUtility.SerializerOptions)
            });

        httpClient.DefaultRequestHeaders.Add("signature", "504e72bc92639c718a40443f1f53161d");
        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.True(bool.Parse(actual));

        var paymentReturn = new DirePayDepositReturn()
        {
            Status = "expired",
            FormId = formId,
            OrderAmount = decimal.Parse("100000.00000"),
            Sign = "504e72bc92639c718a40443f1f53161d",
            Currency = "VND",
            Method = "ewallet",
            PlayerCode = "4ucmqplxb029",
            Type = "deposit",
            ActualAmount = null,
            AfterExchangeOrderAmount = 100000m,
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Deposit_HandleTransactionSuccess_ApproveDeposit()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = DirePayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("485935fb-8f70-4d65-ad35-b37724595d0a");
        var brandId = Guid.NewGuid();
        var secret = "secret";

        var request = """
            {"actual_amount":"100000.00000","currency":"VND","customer_uid":"4ucmqplxb029","method":"ewallet","request_amount":"100000.00000","status":"completed","txid":"485935fb-8f70-4d65-ad35-b37724595d0a","type":"deposit"}
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new DirePayDepositApplyInfo(
            new DirePayDepositSettings(
                "email",
                "password",
                secret,
                formId,
                new Uri("https://localhost"),
                "VND",
                "ewallet",
                "4ucmqplxb029",
                "testRealName",
                "momopay_vn",
                null,
                1m),
             new DirePayDepositResponse(
                100000m,
                formId,
                new Uri("https://cashier.nativepay.co/cashier/?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvcmRlcl9ubyI6IjI0MDcyMjA5NDg0MDg2NDAiLCJ0eXBlIjoiY2FzaGllciJ9.A2yC8ZcTP5mm93h6XNWAdWh4lP1ilhVwIbY2BFs5wgk&ui_version=v1.1.3"),
                "VND",
                "ewallet",
                null));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 800m,
                ThirdPartyOrderNo = "485935fb-8f70-4d65-ad35-b37724595d0a",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DirePayUtility.SerializerOptions)
            });

        httpClient.DefaultRequestHeaders.Add("signature", "0be536ff90d7f189db27ede8f15c9a0d");
        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.True(bool.Parse(actual));

        var paymentReturn = new DirePayDepositReturn()
        {
            Status = "completed",
            FormId = formId,
            OrderAmount = decimal.Parse("100000.00000"),
            Sign = "0be536ff90d7f189db27ede8f15c9a0d",
            Currency = "VND",
            Method = "ewallet",
            PlayerCode = "4ucmqplxb029",
            Type = "deposit",
            ActualAmount = null,
            AfterExchangeOrderAmount = 100000m,
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Is(paymentReturn.OrderAmount),
                Arg.Any<CancellationToken>());
    }
}