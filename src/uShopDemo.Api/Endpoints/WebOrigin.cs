namespace uShopDemo.Api.Endpoints;

/// <summary>The configured site origin (USHOP_WEB_ORIGIN), normalised; null when unset.</summary>
public static class WebOrigin
{
    public static string? Read(IConfiguration configuration)
    {
        var value = configuration["USHOP_WEB_ORIGIN"]?.Trim().TrimEnd('/');
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static bool Matches(string? origin, string? configured) =>
        configured is not null && origin is not null
        && string.Equals(origin.Trim().TrimEnd('/'), configured, StringComparison.OrdinalIgnoreCase);
}
