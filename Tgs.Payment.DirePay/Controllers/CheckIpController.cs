using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.DirePay.Controllers;

[Route("[controller]")]
[ApiController]
public class CheckIpController: ControllerBase
{
    [HttpGet]
    public async Task<IResult> CheckIpAsync()
    {
        var ip = HttpContext.Connection.RemoteIpAddress!.ToString();

        using HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://api64.ipify.org?format=json")
        };
        var result = await client.GetStringAsync(ip);

        return Results.Json(new
        {
            yourIp = ip,
            serviceIp = result
        });
    }
}
