using GestorAI.API.Domain.Entities;

namespace GestorAI.API.Services.Shared;

public static class HtmlDocumentoBase
{
    public static string WrapDocument(
        string titulo, string corpo, ConfiguracaoEmpresa? cfg, string apiBase)
    {
        var cor = cfg?.CorPrimaria ?? "#2563eb";
        var nome = cfg?.NomeFantasia ?? cfg?.RazaoSocial ?? "Empresa";
        var cabecalho = BuildCabecalho(cfg, cor, nome, apiBase);
        var rodape = BuildRodape(cfg);

        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8">
            <style>
              * { box-sizing: border-box; margin: 0; padding: 0; }
              body { font-family: sans-serif; color: #111; }
              .content { padding: 32px; }
              h1 { font-size: 20px; margin-bottom: 4px; }
              .meta { color: #555; font-size: 13px; margin-bottom: 24px; margin-top: 8px; }
              table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
              th, td { border: 1px solid #ddd; padding: 8px; text-align: left; font-size: 13px; }
              th { background: #f5f5f5; }
              .total { text-align: right; font-weight: bold; font-size: 15px; margin-top: 8px; }
              .objeto { background: #f9f9f9; border: 1px solid #ddd; padding: 16px; margin: 16px 0; font-size: 13px; white-space: pre-wrap; }
              .assinatura { margin-top: 60px; display: flex; gap: 60px; }
              .assinatura div { border-top: 1px solid #333; padding-top: 8px; font-size: 12px; min-width: 200px; }
              .obs { margin-top: 16px; font-size: 13px; color: #555; }
            </style>
            </head>
            <body>
              {{cabecalho}}
              <div class="content">
                {{corpo}}
              </div>
              {{rodape}}
            </body>
            </html>
            """;
    }

    private static string BuildCabecalho(
        ConfiguracaoEmpresa? cfg, string cor, string nome, string apiBase)
    {
        var logoHtml = "";
        if (!string.IsNullOrWhiteSpace(cfg?.LogoUrl))
        {
            var src = cfg.LogoUrl.StartsWith("http")
                ? cfg.LogoUrl
                : $"{apiBase}{cfg.LogoUrl}";
            logoHtml = $"""<img src="{src}" style="height:48px;object-fit:contain;margin-right:12px;" />""";
        }

        var linhasDireita = new List<string>();
        if (!string.IsNullOrWhiteSpace(cfg?.Cnpj))
            linhasDireita.Add($"CNPJ: {cfg.Cnpj}");
        var contato = string.Join(" | ", new[] { cfg?.Telefone, cfg?.Email }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!));
        if (!string.IsNullOrWhiteSpace(contato))
            linhasDireita.Add(contato);

        var direita = linhasDireita.Count > 0
            ? $"""
              <div style="color:white;text-align:right;font-size:12px;line-height:1.7;">
                <div style="font-weight:bold;font-size:14px;">{nome}</div>
                {string.Join("", linhasDireita.Select(l => $"<div>{l}</div>"))}
              </div>
              """
            : $"""<div style="color:white;font-weight:bold;font-size:14px;">{nome}</div>""";

        return $"""
            <div style="background:{cor};padding:16px 32px;display:flex;align-items:center;justify-content:space-between;">
              <div style="display:flex;align-items:center;">
                {logoHtml}
                {(linhasDireita.Count > 0 ? "" : $"""<span style="color:white;font-size:20px;font-weight:bold;">{nome}</span>""")}
              </div>
              {(linhasDireita.Count > 0 ? direita : "")}
            </div>
            """;
    }

    private static string BuildRodape(ConfiguracaoEmpresa? cfg)
    {
        if (cfg is null) return "";

        var partes = new List<string>();
        var end = string.Join(", ", new[]
        {
            cfg.Logradouro,
            string.IsNullOrWhiteSpace(cfg.Numero) ? null : $"nº {cfg.Numero}",
            cfg.Complemento,
            cfg.Bairro
        }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!));

        if (!string.IsNullOrWhiteSpace(end)) partes.Add(end);

        var cidadeUf = string.Join("/", new[] { cfg.Municipio, cfg.Uf }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!));
        if (!string.IsNullOrWhiteSpace(cidadeUf)) partes.Add(cidadeUf);
        if (!string.IsNullOrWhiteSpace(cfg.Cep)) partes.Add($"CEP {cfg.Cep}");

        if (partes.Count == 0) return "";

        return $"""
            <div class="footer" style="margin-top:40px;border-top:1px solid #ddd;padding-top:8px;text-align:center;font-size:11px;color:#888;">
              {string.Join(" — ", partes)}
            </div>
            """;
    }
}
