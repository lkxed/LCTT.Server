public class Result
{
    Result(string status, string? message, object? data)
    {
        Status = status;
        Message = message;
        Data = data;
    }

    public string Status { get; private set; }
    public string? Message { get; private set; }
    public object? Data { get; private set; }

    public static Result Success(object data) => new Result("success", null, data);

    public static Result Error(string message) => new Result("error", message, null);
}