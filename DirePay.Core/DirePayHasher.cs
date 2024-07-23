using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.DirePay;

public class DirePayHasher(ILogger<DirePayHasher> logger)
{
    public string ComputeHash(string source)
    {
        logger.LogTrace("ComputeHash source: {source}", source);

        var data = MD5.HashData(Encoding.UTF8.GetBytes(source));

        return Convert.ToHexString(data).ToLower();
    }
}