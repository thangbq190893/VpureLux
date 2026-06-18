namespace VPureLux.Catalog;

public sealed class CatalogApiResponse<T>
{
    public bool Success { get; init; } = true;
    public required T Data { get; init; }
    public string? Message { get; init; }

    public static CatalogApiResponse<T> From(T data) => new() { Data = data };
}
