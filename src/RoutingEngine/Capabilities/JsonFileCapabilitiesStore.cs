using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoutingEngine.Capabilities;

public sealed class JsonFileCapabilitiesStore : ICapabilitiesStore
{
    private readonly string _filePath;
    private long _version;
    private DateTimeOffset _timestamp;
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, CurrencyCapability>> _cache =
        new Dictionary<string, IReadOnlyDictionary<string, CurrencyCapability>>(System.StringComparer.OrdinalIgnoreCase);

    public JsonFileCapabilitiesStore(string filePath)
    {
        _filePath = filePath;
        _version = 0;
        _timestamp = DateTimeOffset.MinValue;
    }

    public async Task<CorridorCapabilitiesSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        // Simple file timestamp-based reload; content-hash can be added later.
        var info = new FileInfo(_filePath);
        if (!info.Exists)
        {
            return new CorridorCapabilitiesSnapshot(_version, _timestamp, _cache);
        }

        var lastWrite = info.LastWriteTimeUtc;
        if (_timestamp != DateTimeOffset.FromUnixTimeSeconds(0) && _timestamp.UtcDateTime == lastWrite)
        {
            return new CorridorCapabilitiesSnapshot(_version, _timestamp, _cache);
        }

        await using var fs = File.OpenRead(_filePath);
        var model = await JsonSerializer.DeserializeAsync<CapabilitiesFileModel>(fs, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, ct).ConfigureAwait(false);

        var dict = new Dictionary<string, IReadOnlyDictionary<string, CurrencyCapability>>(StringComparer.OrdinalIgnoreCase);
        if (model?.Correspondents != null)
        {
            foreach (var c in model.Correspondents)
            {
                var currencies = new Dictionary<string, CurrencyCapability>(StringComparer.OrdinalIgnoreCase);
                if (c.Currencies != null)
                {
                    foreach (var cur in c.Currencies)
                    {
                        var normalizedCharges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (cur.SupportedCharges != null)
                        {
                            foreach (var ch in cur.SupportedCharges)
                            {
                                var v = NormalizeChargeBearer(ch);
                                if (v != null) normalizedCharges.Add(v);
                            }
                        }
                        currencies[cur.Code?.ToUpperInvariant() ?? string.Empty] = new CurrencyCapability(
                            cur.NostroIban ?? string.Empty,
                            normalizedCharges
                        );
                    }
                }
                dict[c.Bic ?? string.Empty] = currencies;
            }
        }

        _cache = dict;
        _version++;
        _timestamp = new DateTimeOffset(lastWrite, TimeSpan.Zero);
        return new CorridorCapabilitiesSnapshot(_version, _timestamp, _cache);
    }

    private static string? NormalizeChargeBearer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().ToUpperInvariant();
        return v switch
        {
            "BEN" => "BEN",
            "SHA" => "SHA",
            "OWN" => "OUR",
            "OUR" => "OUR",
            _ => null
        };
    }

    private sealed class CapabilitiesFileModel
    {
        public List<Correspondent>? Correspondents { get; set; }
    }

    private sealed class Correspondent
    {
        public string? Bic { get; set; }
        public List<Currency>? Currencies { get; set; }
    }

    private sealed class Currency
    {
        public string? Code { get; set; }
        public string? NostroIban { get; set; }
        public List<string>? SupportedCharges { get; set; }
    }
}
