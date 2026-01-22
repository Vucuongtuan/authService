
namespace authModule.Common;

public class ServiceResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public Paginations? Pagination { get; set; } // optional if fetch all data without pagination

    // Helper static method for quick return

    // success response with single data
    public static ServiceResponse<T> Ok(T data, string message = "Success")
        => new() { Data = data, Success = true, Message = message };

    // success response with pagination
    public static ServiceResponse<T> Ok(T data, Paginations pagination, string message = "Success")
        => new() { Data = data, Pagination = pagination, Success = true, Message = message };



    // fail response
    public static ServiceResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
    // -- end helper methods
}
