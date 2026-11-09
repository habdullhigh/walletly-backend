using System.Text.Json.Serialization;

namespace Walletly.Api.Responses;

// The shape every API response uses. errors is only written when hasError is true.
public sealed class ApiResponse<T>
{
    public bool HasError { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Result { get; init; }
    public int TotalCount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ApiError>? Errors { get; init; }
}

public sealed class ApiError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    // Only set for errors tied to a request field, such as model-binding and validation failures.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Field { get; init; }
}

public static class ApiResponse
{
    public const string DefaultSuccessMessage = "Request successful.";

    public static ApiResponse<T> Success<T>(T? result, string message = DefaultSuccessMessage, int totalCount = 0) => new()
    {
        HasError = false,
        Message = message,
        Result = result,
        TotalCount = totalCount
    };

    public static ApiResponse<object> Failure(string message, IReadOnlyList<ApiError> errors) => new()
    {
        HasError = true,
        Message = message,
        Result = null,
        Errors = errors
    };
}
