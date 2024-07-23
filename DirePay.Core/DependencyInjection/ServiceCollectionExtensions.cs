using System.Reflection;
using Tgs.Payment.DirePay;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirePay(
        this IServiceCollection services,
        Action<DirePayOptions, IServiceProvider> configureOptions,
        Action<IHttpClientBuilder> ConfigureAiClient)
    {
        ConfigureAiClient?.Invoke(services.AddHttpClient<DirePayClient>());

        return services
            .AddOptions<DirePayOptions>()
            .Configure(configureOptions)
            .Services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<DirePayHasher>();
    }
}