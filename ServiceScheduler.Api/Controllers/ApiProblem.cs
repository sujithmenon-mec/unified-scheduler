using Microsoft.AspNetCore.Mvc;
using ServiceScheduler.Api.Application;

namespace ServiceScheduler.Api.Controllers;

internal static class ApiProblem
{
    public static ProblemDetails FromError(ServiceError error)
    {
        var status = error.Category switch
        {
            ServiceErrorCategory.BadRequest => StatusCodes.Status400BadRequest,
            ServiceErrorCategory.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorCategory.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Code
        };
        problem.Extensions["code"] = error.Code;
        return problem;
    }
}