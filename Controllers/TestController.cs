using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? load, [FromQuery] string? query, [FromQuery] string? secure, [FromQuery] int? delay)
    {
        // Simulate heavy load -> return 500 to trigger circuit-breaker middleware
        if (!string.IsNullOrEmpty(load) && load.Equals("heavy", StringComparison.OrdinalIgnoreCase))
        {
            // Simulate some work
            await Task.Delay(50);
            // Return 500 so the upstream middleware can react
            return StatusCode(StatusCodes.Status500InternalServerError, "Simulated heavy load error.");
        }

        // If delay specified, simulate processing delay
        if (delay.HasValue && delay.Value > 0)
        {
            await Task.Delay(delay.Value);
        }

        // Echo back received values to help testing middleware behavior
        var result = new
        {
            Path = Request.Path,
            QueryString = Request.QueryString.Value,
            Load = load,
            Query = query,
            Secure = secure,
            Delay = delay,
            Message = "Processed"
        };

        return Ok(result);
    }
}
