namespace ApiRefactor.Infrastructure.Middleware;

public sealed record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string? Detail = null,
    IDictionary<string, string[]>? Errors = null);
