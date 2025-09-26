using backend.Services;

namespace backend.Endpoints;

public static class CollectionEndpoints
{
    public static void MapCollectionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/collections")
            .RequireAuthorization("AnyRolePolicy");

        group.MapGet("/", async (ITileCollectionService collections) =>
            Results.Ok(await collections.GetCollectionsAsync()));

        group.MapPost("/{collectionId}/install", async (string collectionId, ITileCollectionService collections) =>
        {
            await collections.StartInstallationAsync(collectionId);
            return Results.Ok();
        });
        
        group.MapPost("/{collectionId}/uninstall", async (string collectionId, ITileCollectionService collections) =>
        {
            await collections.UninstallAsync(collectionId);
            return Results.Ok();
        });
    }
}