using Microsoft.AspNetCore.Mvc;

namespace Scraper.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "API WORKING";
    }
}