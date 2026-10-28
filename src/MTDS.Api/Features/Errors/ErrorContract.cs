namespace MTDS.Api.Features.Errors;

class ErrorResponse
{
    public string Status { get; set; } = string.Empty;
    public int Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public ErrorDetail Errors { get; set; } = new();
}

class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}