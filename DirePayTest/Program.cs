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

var token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImp0aSI6ImI0YzAzZDE5ZmRkMTYyZWNiYWI1MTNkYmUxMTA3MmQ0OGMxYTk1NDE4OTQ1ODgyMDlmODU2MzBmOTAxOWI4MDQzNGI1YzQzYWU3ZDdhZDQzIn0.eyJhdWQiOiIxIiwianRpIjoiYjRjMDNkMTlmZGQxNjJlY2JhYjUxM2RiZTExMDcyZDQ4YzFhOTU0MTg5NDU4ODIwOWY4NTYzMGY5MDE5YjgwNDM0YjVjNDNhZTdkN2FkNDMiLCJpYXQiOjE3MjEzNzAwNjcsIm5iZiI6MTcyMTM3MDA2NywiZXhwIjoxNzIxOTc0ODY3LCJzdWIiOiI1MDEiLCJzY29wZXMiOltdfQ.uBL0INaTGqK6l_8cZTUO77pFQV7EW__3TUpZsS8Q6EoN2hGejmCJQug_QYsaq8j2dY5LlMvlmv2rCFSYrvW7NkHIq3x9ZJx0MdXJfzeEo6YsMaAvZ3CbmQygd9qE7MUO_kgpRUKkQ8UpAuuPVVAiz22NEfzAbxL74W300ZNyPnnxeR00Kc0rTtKyUFQNJ4qaGQw3dCkuxmT61_j246GxMiuvhjOiqHrCRNugmndyxE0QNHqlUp53JfDQgcxT7hz_IVoB4kgNAc0zJLO3S36tKs25-kg3TYFavzNdg0CbsZACLMiJD1HNMixyHjaogneGH464pOV68t0VCC64MKA6RXXaJSyfzzOoMiyIHmTcz1bJ3kJApZyQNdQ2lpaAZxK3FxtGOPBHq20JxO1t-IascaDwb4gYIU0mGRgoPEOv8qtDLM4joc7-JY2gQAq8-2jQTqDEKpQnBkQOEL88qH9y0Y5kyCzrplTqddvmeGlVpp8w3JVbWELaVh5U3NjZJy1F8KdHzeASigoJjsgb6qh-_7GV5oet490Cc5GnAH5w7E9yPnvkZciachvLflNzDlpg3urBevmUlm-DKGWQR9NX1e3wIuAB9Kua1mGQUbs_JiBm8xCOEI9uPohoT_rnt-AIRcktU871Cm4KW2an0J_0m2qqygYpFmxFR6MmuOEQTRc";
var secret = "HlgY5hW";

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