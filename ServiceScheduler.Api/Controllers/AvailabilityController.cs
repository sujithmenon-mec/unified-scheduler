using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ServiceScheduler.Api.Application;

namespace ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/availability")]
public class AvailabilityController(AvailabilityService availabilityService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilitySlot>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSlots(
        [FromQuery, BindRequired] Guid dealershipId,
        [FromQuery, BindRequired] Guid serviceTypeId,
        [FromQuery, BindRequired] DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await availabilityService.GetSlotsAsync(
            dealershipId, serviceTypeId, date, cancellationToken);
        if (!result.Succeeded)
        {
            var problem = ApiProblem.FromError(result.Error!);
            return new ObjectResult(problem) { StatusCode = problem.Status };
        }

        return Ok(result.Slots);
    }
}