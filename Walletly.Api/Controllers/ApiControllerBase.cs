using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Walletly.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(title: error.Code, detail: error.Description, statusCode: statusCode);
    }
}
