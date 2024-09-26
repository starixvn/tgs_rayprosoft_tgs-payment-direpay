using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.DirePay.Controllers;

[Route("[controller]")]
[ApiController]
public class CheckIpController: ControllerBase
{

    [HttpGet]
    public async Task<IResult> CheckIpAsync([FromServices] HttpClient httpClient, [FromQuery]string url)
    {
        var ip = HttpContext.Connection.RemoteIpAddress!.ToString();
        var result = await httpClient.GetStringAsync(url);

        return Results.Json(new
        {
            yourIp = ip,
            serviceIp = result
        });
    }
}
