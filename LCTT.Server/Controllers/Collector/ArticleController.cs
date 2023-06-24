using LCTT.Server.Models;
using LCTT.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace LCTT.Server.Controllers;

[ApiController]
[Route("api/collector/[controller]")]
public class ArticleController : ControllerBase
{
    private readonly ILogger<ArticleController> _logger;

    public ArticleController(ILogger<ArticleController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public object Preview(string url, bool json = true)
    {
        var (_, article, template) = CollectorService.Parse(url);
        var content = CollectorService.Generate(article, template);
        return json ? Result.Success(content) : content;
    }

    [HttpPost]
    public object Collect(DCUCRequest request)
    {
        var difficulty = request.Difficulty;
        var category = request.Category;
        var url = request.URL;
        var content = request.Content;
        return CollectorService.Collect(difficulty, category, url, content);
    }
}
