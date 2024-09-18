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

        return Results.Json(new
        {
            yourIp = ip
        });
    }
}
