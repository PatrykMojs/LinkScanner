using LinkScanner.Domain.Entities;

namespace LinkScanner.Infrastructure.Scanning.Analyzers;

public sealed class SecurityHeadersAnalyzer
{
    public SecurityHeaders Analyze(HttpResponseMessage response)
    {
        return new SecurityHeaders
        {
            HasCSP = HasHeader(response, "Content-Security-Policy"),
            HasHSTS = HasHeader(response, "Strict-Transport-Security"),
            HasXFO = HasHeader(response, "X-Frame-Options"),
            HasXCTO = HasHeader(response, "X-Content-Type-Options"),
            HasReferrerPolicy = HasHeader(response, "Referrer-Policy"),
            HasPermissionsPolicy = HasHeader(response, "Permissions-Policy")
        };
    }

    private static bool HasHeader(HttpResponseMessage response, string name)
    {
        return response.Headers.Contains(name) ||
               response.Content.Headers.Contains(name);
    }
}