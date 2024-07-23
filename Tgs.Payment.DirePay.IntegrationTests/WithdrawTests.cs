using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.DirePay.Client;
using Tgs.Payment.Clients;
using Xunit.Abstractions;

namespace Tgs.Payment.DirePay.IntegrationTests;

public class WithdrawTests(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

    [Fact]
    public async Task 代付回調_訂單狀態失敗()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = DirePayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9");
        var brandId = Guid.NewGuid();
        var email = "email";
        var password = "password";
        var secret = "secret";

        var settings = new DirePayWithdrawSettings(
            email,
            password,
            secret,
            formId,            
            "VND",
            "4ucmqplxb029",
            "testBankAccountName",
            "112233445566",
            "testAccountName",
            "acb",
            1m);
        var withdrawResponse = new DirePayWithdrawResponse(formId, 50000m, "VND", "online_banking");

        var applyInfo = new DirePayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DirePayUtility.SerializerOptions)
            });

        var request = """
            {"actual_amount":"0.00000","currency":"VND","customer_uid":"4ucmqplxb029","method":"online_banking","request_amount":"50000.00000","status":"rejected","txid":"42ab1126-001e-42d8-8cdc-8d8c2ff1a7b9","type":"withdrawal"}
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        httpClient.DefaultRequestHeaders.Add("signature", "63323f8bae51545af05be3be8d95c6fa");
        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new DirePayWithdrawReturn()
        {
            FormId = formId,
            OrderAmount = decimal.Parse("50000.00000"),
            Sign = "63323f8bae51545af05be3be8d95c6fa",
            Status = "rejected",
            ActualAmount = "0.00000",
            Currency = "VND",
            Method = "online_banking",
            PlayerCode = "4ucmqplxb029",
            Type = "withdrawal",
            AfterExchangeOrderAmount = 50000.00000m
        };

        Assert.True(bool.Parse(actual));
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, DirePayUtility.SerializerOptions)),                
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = DirePayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("a18af61e-6073-4268-a6e3-c2d8ffbd5daa");
        var brandId = Guid.NewGuid();
        var email = "email";
        var password = "password";
        var secret = "secret";

        var settings = new DirePayWithdrawSettings(
            email,
            password,
            secret,
            formId,            
            "VND",
            "4ucmqplxb029",
            "testBankAccountName",
            "112233445566",
            "testAccountName",
            "acb",
            1m);
        var withdrawResponse = new DirePayWithdrawResponse(formId, 50000m, "VND", "online_banking");

        var applyInfo = new DirePayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, DirePayUtility.SerializerOptions)
            });

        var request = """
            {"actual_amount":"50000.00000","currency":"VND","customer_uid":"4ucmqplxb029","method":"online_banking","request_amount":"50000.00000","status":"completed","txid":"a18af61e-6073-4268-a6e3-c2d8ffbd5daa","type":"withdrawal"}
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        httpClient.DefaultRequestHeaders.Add("signature", "e0af2eb8366ebe577d961a83740fddf2");

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new DirePayWithdrawReturn()
        {
            FormId = formId,
            OrderAmount = decimal.Parse("50000.00000"),
            Sign = "e0af2eb8366ebe577d961a83740fddf2",
            Status = "completed",
            ActualAmount = "50000.00000",
            Currency = "VND",
            Method = "online_banking",
            PlayerCode = "4ucmqplxb029",
            Type = "withdrawal",
            AfterExchangeOrderAmount = 50000.00000m
        };

        Assert.True(bool.Parse(actual));
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, DirePayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
}