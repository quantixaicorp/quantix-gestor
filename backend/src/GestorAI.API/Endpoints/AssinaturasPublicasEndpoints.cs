namespace GestorAI.API.Endpoints;

public static class AssinaturasPublicasEndpoints
{
    public static void MapAssinaturasPublicas(this IEndpointRouteBuilder app)
    {
        // Public subscription routes are registered in PublicBookingEndpoints
        // to avoid duplicate /public/{slug} route group prefix.
    }
}
