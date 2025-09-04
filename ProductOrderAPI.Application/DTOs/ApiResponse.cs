public class ApiResponse<T>
{
    public T? Data { get; private set; }
    public bool Success { get; private set; }
    public string? Message { get; private set; }

    // Private constructor to force use of static helpers
    private ApiResponse(T? data, bool success, string? message)
    {
        Data = data;
        Success = success;
        Message = message;
    }

    private ApiResponse(string message, bool success)
    {
        Data = default;
        Success = success;
        Message = message;
    }

    // Static factory methods
    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>(data, true, message);
    }

    public static ApiResponse<T> Fail(string message, string v)
    {
        return new ApiResponse<T>(message, false);
    }
}
