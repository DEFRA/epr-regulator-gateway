using EprRegulatorGateway.Account.Contracts.Responses;
using EprRegulatorGateway.Account.Services;
using EprRegulatorGateway.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EprRegulatorGateway.Controllers;

[ApiController]
[Route("api/account")]
[Authorize(Policy = PolicyNames.Read)]
public sealed class AccountController(IAccountClient accountClient) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid account id",
                detail: "Account id must not be empty.");
        }

        try
        {
            var details = await accountClient.GetAccountDetailsAsync(id, cancellationToken);
            return Ok(details);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Translate downstream 404 from the User Service to an upstream 404.
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "Account was not found.");
        }
    }
}
