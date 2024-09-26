using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Logging;
using Tgs.Payment.DirePay;

namespace DirePay.Core.UnitTests;
public static class DirePayClientFactory
{
    public static DirePayClient CreateDirePayClient(MockHttpMessageHandler mockHttp)
    {
        var httpClient = mockHttp.ToHttpClient();
        var mockTgsLog = Substitute.For<ITgsLog>();
        var mockMemoryCache = Substitute.For<IMemoryCache>();
        httpClient.BaseAddress = new Uri(uriString: "https://localhost");
        var signer = new DirePayHasher(NullLogger<DirePayHasher>.Instance);
        var sut = new DirePayClient(httpClient, mockMemoryCache, signer, mockTgsLog, NullLogger<DirePayClient>.Instance);

        return sut;
    }
}
