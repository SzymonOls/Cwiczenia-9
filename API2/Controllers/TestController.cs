namespace API2.Controllers;

using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("test");
    }
}