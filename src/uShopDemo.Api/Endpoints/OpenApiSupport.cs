using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace uShopDemo.Api.Endpoints;

public static class OpenApiSupport
{
    /// <summary>
    /// <c>/health</c> is mapped by the health-check middleware, which ApiExplorer does not see, so the document
    /// transformer adds it by hand.
    /// </summary>
    public static IServiceCollection AddApiDocument(this IServiceCollection services) =>
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Paths ??= [];
            document.Paths["/health"] = new OpenApiPathItem
            {
                Operations = new Dictionary<HttpMethod, OpenApiOperation>
                {
                    [HttpMethod.Get] = new()
                    {
                        OperationId = "Health",
                        Summary = "Liveness and database health",
                        Tags = new HashSet<OpenApiTagReference> { new("Health") },
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse { Description = "Healthy" },
                            ["503"] = new OpenApiResponse { Description = "Unhealthy" },
                        },
                    },
                },
            };
            return Task.CompletedTask;
        }));
}
