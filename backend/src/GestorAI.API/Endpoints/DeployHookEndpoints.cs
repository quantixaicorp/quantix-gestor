using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GestorAI.API.Endpoints;

public static class DeployHookEndpoints
{
    public static void MapDeployHook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/deploy-hook", async (
            HttpContext ctx,
            IConfiguration config,
            ILogger<Program> logger) =>
        {
            var provided = ctx.Request.Headers["X-Deploy-Token"].ToString();
            var expected = config["Deploy:WebhookToken"];

            if (string.IsNullOrWhiteSpace(expected))
            {
                logger.LogError("Deploy:WebhookToken não configurado — deploy-hook rejeitado.");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
                return Results.Unauthorized();

            var body = await JsonSerializer.DeserializeAsync<DeployHookBody>(
                ctx.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var sha = body?.Sha?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(sha))
                return Results.BadRequest(new { error = "sha obrigatório" });

            var scriptPath = config["Deploy:ScriptPath"] ?? "/opt/gestorai/deploy.sh";

            logger.LogInformation("Deploy hook acionado — SHA: {Sha}, script: {Script}", sha, scriptPath);

            // Dispara o deploy em background para responder imediatamente ao caller (curl).
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                try
                {
                    using var proc = new Process();
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"{scriptPath} {sha}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    };
                    proc.Start();
                    var stdout = await proc.StandardOutput.ReadToEndAsync();
                    var stderr = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (proc.ExitCode == 0)
                        logger.LogInformation("Deploy concluído.\n{Output}", stdout);
                    else
                        logger.LogError("Deploy falhou (exit {Code}):\n{Stderr}", proc.ExitCode, stderr);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao executar deploy");
                }
            });

            return Results.Ok(new { status = "deploying", sha });
        }).AllowAnonymous();
    }
}

public record DeployHookBody(string Sha);
