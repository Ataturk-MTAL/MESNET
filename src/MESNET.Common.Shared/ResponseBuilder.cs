namespace MESNET.Common.Shared;

public sealed class ResponseBuilder
{
    private int _code;
    private string _type = default!;
    private string? _message;
    private object? _data;
    private object? _errors;

    private ResponseBuilder() { }

    public static ResponseBuilder Success(int code = 200) =>
        new() { _code = code, _type = "Success" };

    public static ResponseBuilder Fail(int code = 400) =>
        new() { _code = code, _type = "Error" };

    public ResponseBuilder AddData(object data)
    {
        _data = data;
        return this;
    }

    public ResponseBuilder AddMessage(string message)
    {
        _message = message;
        return this;
    }

    public ResponseBuilder AddErrors(object errors)
    {
        _errors = errors;
        return this;
    }

    public ResponseBuilder WithCode(int code)
    {
        _code = code;
        return this;
    }

    public ApiResponse Build() =>
        new()
        {
            Code = _code,
            Type = _type,
            Message = _message,
            Data = _data,
            Errors = _errors
        };
}
