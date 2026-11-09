using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Walletly.Api.Responses;

namespace Walletly.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    // Failures keep their HTTP status and use the standard envelope with hasError = true.
    protected IActionResult ToErrorResponse(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, ApiResponse.Failure(error.Description, [new ApiError { Code = error.Code, Message = error.Description }]));
    }
}
