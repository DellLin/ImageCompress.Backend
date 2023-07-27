
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [Authorize]
    [HttpGet("Authorize")]
    public String TestAuthorize ()
    {
        return $"Hi {User.Identity!.Name}";
    }
    [HttpGet]
    public String Test ()
    {
        return $"Test";
    }
}
