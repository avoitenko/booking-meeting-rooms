using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BookingMeetingRooms.Api.Dtos;

public class ErrorResponseDto
{
    public string Error { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Details { get; set; }
    public int? StatusCode { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public ErrorResponseDto(string error, string? message = null, string? details = null, int? statusCode = null, Dictionary<string, string[]>? validationErrors = null)
    {
        Error = error;
        Message = message;
        Details = details;
        StatusCode = statusCode;
        ValidationErrors = validationErrors;
    }

    public static ErrorResponseDto FromModelState(ModelStateDictionary modelState, string error = "ValidationError")
    {
        var validationErrors = new Dictionary<string, string[]>();

        foreach (var keyValuePair in modelState)
        {
            var key = keyValuePair.Key;
            var errors = keyValuePair.Value.Errors
                .Select(e => e.ErrorMessage)
                .Where(e => !string.IsNullOrEmpty(e))
                .ToArray();

            if (errors.Length > 0)
            {
                validationErrors[key] = errors;
            }
        }

        var allMessages = validationErrors
            .SelectMany(kvp => kvp.Value)
            .ToList();

        return new ErrorResponseDto(
            error,
            allMessages.Count > 0 ? string.Join("; ", allMessages) : "Validation failed",
            null,
            400,
            validationErrors.Count > 0 ? validationErrors : null);
    }
}
