using Microsoft.AspNetCore.Http.Extensions;
using System.Net;
using System.Text.Json;
using Tgs.Logging;

namespace Tgs.Payment.DirePay;

public class TgsLogHandler(ITgsLog tgsLog)
{
    private readonly JsonSerializerOptions _jsonOption = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public void LogApi(HttpContext context, object? request, bool response, Guid trackingId)
    {
        var apiCall = new ApiLogEvent
        {
            Ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            Method = HttpMethod.Post.Method,
            Path = context.Request.Path,
            Prefix = context.Request.GetDisplayUrl().Replace(context.Request.Path, string.Empty),
            RecordedOn = DateTimeOffset.Now,
            RequestContent = JsonSerializer.Serialize(request, _jsonOption),
            RequestHeaders = JsonSerializer.Serialize(context.Request.Headers),
            ResponseCode = ((short)HttpStatusCode.OK),
            ResponseContent = response.ToString(),
            ResponseHeaders = JsonSerializer.Serialize(context.Response.Headers),
            ServerName = Environment.MachineName,
            TrackingId = trackingId
        };

        tgsLog.LogApi(apiCall);
    }
}
