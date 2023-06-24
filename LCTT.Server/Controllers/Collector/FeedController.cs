using Microsoft.AspNetCore.Mvc;
using LCTT.Server.Services;

namespace LCTT.Server.Controllers;

[ApiController]
[Route("api/collector/[controller]")]
public class FeedController : ControllerBase
{
    private readonly ILogger<FeedController> _logger;

    public FeedController(ILogger<FeedController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public object Feed(string startDate, string? endDate, bool? groupBy)
    {
        return CollectorService.Feed(startDate, endDate, groupBy);
    }
}
