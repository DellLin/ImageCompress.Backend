using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(ILogger<ClaimsController> logger)
    {
        _logger = logger;
    }
    [Authorize]
    [HttpGet]
    public ActionResult GetClaims()
    {
        return Ok(User.Claims.Select(p => new { p.Type, p.Value }));
    }
    [Authorize]
    [HttpGet("Username")]
    public IActionResult GetUserName()
    {
        return Ok(User.Identity!.Name);
    }
}
