using Microsoft.AspNetCore.Mvc;
using Scraper.API.DTOs;
using Scraper.API.Services;

namespace Scraper.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly ScraperFacade _facade;

    public ScraperController()
    {
        _facade = new ScraperFacade();
    }

    [HttpPost("scrape")]
    public async Task<IActionResult> Scrape([FromBody] ScrapeRequest request)
    {
        var result = await _facade.ScrapeAsync(
            request.Url,
            request.Selector,
            request.UseJavaScript
        );

        return Ok(result);
    }
}