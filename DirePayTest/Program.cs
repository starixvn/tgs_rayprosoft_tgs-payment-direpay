using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using Tgs.Payment.Clients;
using Tgs.Payment.DirePay;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()
    .AddKeyedSingleton("Nats",
    (sp, _) => new NatsConnection(NatsOpts.Default with
    {
        Url = "nats://localhost:4222"
    }))
    .AddPhoenixToolkitsMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats")))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();

var token = "token";
var secret = "secret";

//代收
/*
var orderNo = Guid.NewGuid();
var amount = 100000m;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.uat.direpay.app")
};
var client = new DirePayClient(
    httpClient,
    new DirePayHasher(NullLogger<DirePayHasher>.Instance),
    NullLogger<DirePayClient>.Instance);

var depositOrder = new DepositOrder(
    amount,
    Guid.NewGuid(),
    new Uri("https://nj44r5vs-7219.asse.devtunnels.ms/deposit/callback"),
    new Uri("https://localhost"),
    "VND",
    "ewallet",
    "4ucmqplxb029",
    "testRealName",
    "momopay_vn",
    null);

var depositreturn = await client.DepositAsync(
    token,
    secret,
    depositOrder);
logger.LogInformation($"deposit return: {depositreturn}");
*/

//代付

var amount = 50000m;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.uat.direpay.app")
};
var client = new DirePayClient(
    httpClient,
    new DirePayHasher(NullLogger<DirePayHasher>.Instance),
    NullLogger<DirePayClient>.Instance);

var withdrawOrder = new WithdrawOrder(
    Guid.NewGuid(),
    amount,
    new Uri("https://nj44r5vs-7219.asse.devtunnels.ms/withdraw/callback"),
    "VND",
    "4ucmqplxb029",
    "testBankAccountName",
    "112233445566",
    "testAccountName",
    "acb");

var withdrawReturn = await client.WithdrawAsync(token, secret, withdrawOrder);
logger.LogInformation($"withdraw return: {withdrawReturn}");


//交易查詢
/*
var formId = Guid.Parse("485935fb-8f70-4d65-ad35-b37724595d0a");
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.uat.direpay.app")
};
var client = new DirePayClient(
    httpClient,
    new DirePayHasher(NullLogger<DirePayHasher>.Instance),
    NullLogger<DirePayClient>.Instance);

var transactionQuery = await client.QueryTransactionAsync(token, secret, formId);
logger.LogInformation("{transactionQuery}", transactionQuery);
*/
//餘額查詢
/*
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.uat.direpay.app")
};
var client = new DirePayClient(
    httpClient,
    new DirePayHasher(NullLogger<DirePayHasher>.Instance),
    NullLogger<DirePayClient>.Instance);

var balanceQuery = await client.QueryBalanceAsync(token, secret);
logger.LogInformation("{balanceQuery}", balanceQuery);
*/