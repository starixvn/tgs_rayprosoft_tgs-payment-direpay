using System.Net;
using System.Net.Mime;
using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Nats.Serializers;
using Tgs.Payment.DirePay;
using Tgs.Payment.DirePay.MessageQueue;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
            fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
                "MachineName",
                () => Environment.MachineName));
        });
    });
builder.Services.AddMemoryCache();
builder.Services
    .AddTgsLog()
    .AddSingleton<TgsLogHandler>()
    .AddControllers();

builder.Services
    .AddDirePay(
        (options, _) => builder.Configuration.GetSection("DirePay").Bind(options),
        clientBuilder =>
        {
            clientBuilder
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<DirePayOptions>>().Value;

                http.BaseAddress = options.PaymentApiUrl;
            })
            .AddLoggingHandler<DirePayOptions>();

            var proxy = builder.Configuration.GetValue<string>("Proxy");

            if (proxy != null)
            {
                Console.WriteLine($"Use Proxy: {proxy}");
                _ = clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    Proxy = new WebProxy(proxy)
                });
            }
        })
    .AddPaymentClient()
    .AddPaymentServiceClient(httpBuilder => httpBuilder
        .ConfigureHttpClient((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<DirePayOptions>>().Value;

            http.BaseAddress = options.PaymentServiceBaseUrl;
        }));

builder.Services
    .AddKeyedSingleton("Nats",
    (sp, _) => new NatsConnection(NatsOpts.Default with
    {
        Url = builder.Configuration.GetConnectionString("Nats")!,
        SerializerRegistry = TgsNatsSerializerRegistration.Default
    }))
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
        .AddProcessor(
            ProcessorTypes.DepositProcessorType,
            "tgs.payment.direpay.deposit",
            "tgs.payment.direpay.deposit")
        .AddProcessor(
            ProcessorTypes.WithdrawProcessorType,
            "tgs.payment.direpay.withdraw",
            "tgs.payment.direpay.withdraw")
        .AddProcessor(
            ProcessorTypes.GetBalanceProcessorType,
            "tgs.payment.direpay.balance",
            "tgs.payment.direpay.balance"))
    .AddNatsGlobPatternExchange("*");

builder.Services
    .AddProfileViewer()
    .AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
        logging.MediaTypeOptions.AddText(MediaTypeNames.Application.FormUrlEncoded);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Application.Json);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Text.Html);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Multipart.FormData);
    })
    .AddHealthChecks()
    .Services
    .AddOpenTelemetry()
    .WithTracing(traceBuilder => traceBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("Valhalla.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .WithMetrics(meterBuiler => meterBuiler
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .Services
    .Configure<AspNetCoreTraceInstrumentationOptions>(options => options.Filter =
        httpContext =>
        {
            var path = httpContext.Request.Path;

            if (!path.HasValue)
            {
                return true;
            }

            List<string> segmentExclude = ["/swagger", "/healthz", "/metrics", "/profiler/"];
            if (segmentExclude.Exists(segment => path.StartsWithSegments(segment)))
            {
                return false;
            }

            List<string> postfixExlcude = ["html", "ico", "png", "jpg"];

            return !postfixExlcude.Exists(postfix => path.Value.EndsWith(postfix));
        });

var app = builder.Build();

app.UseWhen(context => !(context.Request.Path.StartsWithSegments("/healthz") && context.Response.StatusCode == ((int)HttpStatusCode.OK)), app => app.UseHttpLogging());
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Use(async (context, next) =>
{
    await next();

    var logger = context.RequestServices.GetService<ILoggerFactory>()
        ?.CreateLogger("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");

    if (logger?.IsEnabled(LogLevel.Information) == true)
    {
        using var stream = new StreamReader(context.Request.Body);

        _ = await stream.ReadToEndAsync().ConfigureAwait(false);
    }
});

await app.RunAsync();