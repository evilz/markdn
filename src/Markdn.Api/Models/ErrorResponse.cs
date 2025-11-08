namespace Markdn.Api.Models;

/// <summary>
/// Error response DTO.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error type.
    /// </summary>
    public required string Error { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }
}
