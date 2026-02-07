namespace BookingMeetingRooms.Api.Dtos;

public class ErrorResponseDto
{
    public string Error { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Details { get; set; }
    public int? StatusCode { get; set; }

    public ErrorResponseDto(string error, string? message = null, string? details = null, int? statusCode = null)
    {
        Error = error;
        Message = message;
        Details = details;
        StatusCode = statusCode;
    }
}
