using System.Net;
using System.Net.Sockets;
using GestorAI.API.Shared.Exceptions;

namespace GestorAI.API.Shared.Net;

/// <summary>
/// Valida URLs informadas pelo usuário antes de requisições server-side,
/// mitigando SSRF para a rede interna / endpoints de metadados de nuvem.
/// </summary>
public static class OutboundUrlGuard
{
    public static Uri EnsurePublicHttp(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new AppException("URL inválida.", 400);

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new AppException("Apenas URLs http/https são permitidas.", 400);

        // Resolve o host e rejeita se qualquer endereço for privado/loopback/link-local.
        IPAddress[] addresses;
        if (IPAddress.TryParse(uri.Host, out var literal))
        {
            addresses = [literal];
        }
        else
        {
            try { addresses = Dns.GetHostAddresses(uri.Host); }
            catch { throw new AppException("Não foi possível resolver o host.", 400); }
        }

        if (addresses.Length == 0 || addresses.Any(IsPrivate))
            throw new AppException("Destino não permitido.", 400);

        return uri;
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            // 10.0.0.0/8
            if (b[0] == 10) return true;
            // 172.16.0.0/12
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            // 192.168.0.0/16
            if (b[0] == 192 && b[1] == 168) return true;
            // 169.254.0.0/16 (link-local / metadados de nuvem)
            if (b[0] == 169 && b[1] == 254) return true;
            // 127.0.0.0/8
            if (b[0] == 127) return true;
            // 0.0.0.0/8
            if (b[0] == 0) return true;
            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
            // Unique local address fc00::/7
            var b = ip.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return true;
            // Mapeia IPv4-mapped (::ffff:a.b.c.d) e revalida
            if (ip.IsIPv4MappedToIPv6) return IsPrivate(ip.MapToIPv4());
            return false;
        }

        return true;
    }
}
