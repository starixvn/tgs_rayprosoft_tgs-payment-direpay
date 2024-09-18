using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
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

    public static string JsonEncodeObject(object obj)
    {
        var paramsRequest = JsonSerializer.Deserialize<Dictionary<string, JsonNode>>(obj.ToString()!);
        var orderedParamRequest = paramsRequest!.OrderBy(x => x.Key).Aggregate(new JsonObject(), (accumulate, item) =>
        {
            accumulate.Add(item.Key, item.Value);
            return accumulate;
        });

        return JsonSerializer.Serialize(orderedParamRequest);
    }
}