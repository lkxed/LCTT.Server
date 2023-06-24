
using Microsoft.AspNetCore.Mvc;
using LCTT.Server.Services;

[ApiController]
[Route("api/collector/[controller]")]
public class BranchController : ControllerBase
{
    [HttpDelete]
    public object Clean()
    {
        return CollectorService.Clean();
    }
}