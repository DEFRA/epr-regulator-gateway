using EprRegulatorGateway.Account.Services;
using EprRegulatorGateway.Utils.Http;
using Microsoft.AspNetCore.Mvc;

namespace EprRegulatorGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AccountController(IAccountClient accountClient) : ControllerBase
{
    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("Account id is required.");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUserId = User.TryGetUserId();
            if (currentUserId is null)
            {
                return Unauthorized("User id claim is missing or invalid.");
            }

            if (currentUserId.Value != id)
            {
                return Forbid();
            }
        }

        var details = await accountClient.GetAccountDetailsAsync(id, cancellationToken);
        return Ok(details);
    }
}
