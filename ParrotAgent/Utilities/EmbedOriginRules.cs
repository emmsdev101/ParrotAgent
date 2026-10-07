namespace ParrotAgent.Utilities
{
    public static class EmbedOriginRules
    {
        public const int MaxOrigins = 20;

        public static bool TryParseList(string? raw, out List<string> origins, out string? error)
        {
            origins = new List<string>();
            error = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            var parts = raw.Split(new[] { '\r', '\n', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!TryNormalize(part, out var origin, out error))
                {
                    return false;
                }

                if (!origins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    origins.Add(origin);
                }

                if (origins.Count > MaxOrigins)
                {
                    error = $"Add at most {MaxOrigins} websites.";
                    return false;
                }
            }

            return true;
        }

        public static bool TryNormalize(string raw, out string origin, out string? error)
        {
            origin = string.Empty;
            error = null;
            var value = raw.Trim();
            if (value.Contains('*') || value.Contains('@'))
            {
                error = "Enter a site origin such as https://example.com. Wildcards are not allowed.";
                return false;
            }

            if (!value.Contains("://", StringComparison.Ordinal))
            {
                value = "https://" + value;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            {
                error = $"\"{raw.Trim()}\" is not a valid website.";
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                error = "Use an https website.";
                return false;
            }

            var local = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host == "127.0.0.1"
                || uri.Host == "::1";
            if (uri.Scheme == Uri.UriSchemeHttp && !local)
            {
                error = "Public websites must use https.";
                return false;
            }

            var port = uri.IsDefaultPort ? string.Empty : ":" + uri.Port;
            origin = $"{uri.Scheme}://{uri.IdnHost}{port}";
            return true;
        }

        public static bool IsAllowed(string? requestOrigin, string? storedOrigins)
        {
            if (string.IsNullOrWhiteSpace(requestOrigin) || string.IsNullOrWhiteSpace(storedOrigins))
            {
                return false;
            }

            if (!TryNormalize(requestOrigin, out var origin, out _))
            {
                return false;
            }

            return storedOrigins
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(saved => string.Equals(saved, origin, StringComparison.OrdinalIgnoreCase));
        }
    }
}
