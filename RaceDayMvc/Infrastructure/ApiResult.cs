namespace RaceDayMvc.Infrastructure;

/// <summary>Outcome of a call to the RaceDay API (no response body).</summary>
public class ApiResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }

    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public bool IsNotFound => StatusCode == 404;
    public bool IsConflict => StatusCode == 409;

    public static ApiResult Success(int statusCode = 200) =>
        new() { IsSuccess = true, StatusCode = statusCode };

    public static ApiResult Failure(int statusCode, string error) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };
}

/// <summary>Outcome of a call to the RaceDay API that returns data.</summary>
public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> WithData(T data, int statusCode = 200) =>
        new() { IsSuccess = true, StatusCode = statusCode, Data = data };

    public static ApiResult<T> Failed(int statusCode, string error) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };
}
