using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.DirePay.Controllers;

[Route("[controller]")]
[ApiController]
public class CheckIpController: ControllerBase
{
    [HttpGet]
    public IResult CheckIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress!.ToString();

        using HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://api64.ipify.org?format=json")
        };
        var result = client.GetStringAsync(ip);

        return Results.Json(new
        {
            yourIp = ip,
            serviceIp = result.ToString()
        });
    }
}
