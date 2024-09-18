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

        using HttpClient client = new HttpClient();
        var result = await client.GetStringAsync("https://api.ipify.org?format=json");

        return Results.Json(new
        {
            yourIp = ip,
            serviceIp = result
        });
    }
}
