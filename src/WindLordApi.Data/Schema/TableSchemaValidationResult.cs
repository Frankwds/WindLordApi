namespace WindLordApi.Data.Schema;

public sealed record TableSchemaValidationResult(bool IsValid, string Message)
{
    public static TableSchemaValidationResult Valid(string message) => new(true, message);

    public static TableSchemaValidationResult Invalid(string message) => new(false, message);
}