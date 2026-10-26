using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Application;
using ServiceScheduler.Api.Infrastructure;

namespace ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(BookingService bookingService, SchedulerDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Book(
        [FromBody] BookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookingService.BookAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            var problem = ApiProblem.FromError(result.Error!);
            return new ObjectResult(problem) { StatusCode = problem.Status };
        }

        var response = AppointmentResponse.From(result.Appointment!);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Appointments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (appointment is null)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "APPOINTMENT_NOT_FOUND",
                Detail = "APPOINTMENT_NOT_FOUND"
            };
            problem.Extensions["code"] = "APPOINTMENT_NOT_FOUND";
            return NotFound(problem);
        }

        return Ok(AppointmentResponse.From(appointment));
    }
}