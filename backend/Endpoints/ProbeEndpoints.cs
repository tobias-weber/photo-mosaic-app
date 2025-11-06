using backend.Data;

namespace backend.Endpoints;

public static class ProbeEndpoints
{
    public static void MapProbeEndpoints(this IEndpointRouteBuilder routes)
    {
        // health check endpoint
        routes.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

        // readiness check endpoint (database connectivity)
        routes.MapGet("/readyz", async (AppDbContext db) =>
        {
            try
            {
                await db.Database.CanConnectAsync();
                return Results.Ok(new { status = "ready" });
            }
            catch
            {
                return Results.StatusCode(503);
            }
        });
    }
    
}