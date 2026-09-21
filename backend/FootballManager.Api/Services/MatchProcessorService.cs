using System.Globalization;
using System.Text;
using System.Text.Json;
using FootballManager.Application.Exceptions;
using Google.GenAI;
using Google.GenAI.Types;
using GenAiType = Google.GenAI.Types.Type;

namespace FootballManager.Api.Services;

/// <summary>
/// Reads a results sheet image with Gemini and returns the CSV already consumed by the results importer.
/// </summary>
public sealed class MatchProcessorService
{
    public const string DefaultModel = "gemini-3.6-flash";
    public const string CsvHeader = "fecha,division,Equipo 1,goles equipo 1,equipo 2,goles equipo 2,estado";

    private const int TimeoutMs = 90_000;

    private readonly IConfiguration _configuration;
    private readonly ILogger<MatchProcessorService> _logger;

    public MatchProcessorService(IConfiguration configuration, ILogger<MatchProcessorService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ProcessAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Tenés que subir una imagen de la planilla.");

        var mime = NormalizeMime(mimeType);
        if (mime == null)
            throw new ArgumentException("La imagen tiene que ser JPG, PNG, WebP o GIF.");

        var apiKey = FirstConfigured("GEMINI_API_KEY", "Gemini:ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new BusinessException("Falta configurar GEMINI_API_KEY.");

        var model = FirstConfigured("GEMINI_MODEL", "Gemini:Model");
        if (string.IsNullOrWhiteSpace(model))
            model = DefaultModel;

        var prompt = new Content
        {
            Role = "user",
            Parts = new List<Part>
            {
                Part.FromText(
                    "Leé esta planilla de resultados y devolvé todos los partidos, de arriba hacia abajo. " +
                    "Si hay varias tablas o varios títulos de fecha en la misma imagen, incluí cada partido con la fecha y la división que le corresponden."),
                Part.FromBytes(imageBytes, mime),
            },
        };

        var config = new GenerateContentConfig
        {
            Temperature = 0,
            MaxOutputTokens = 8192,
            ResponseMimeType = "application/json",
            ResponseSchema = BuildSchema(),
            ThinkingConfig = new ThinkingConfig
            {
                IncludeThoughts = false,
                ThinkingBudget = 0,
            },
            SystemInstruction = new Content
            {
                Parts = new List<Part> { Part.FromText(SystemPrompt) },
            },
        };

        string raw;
        try
        {
            raw = await GenerateAsync(apiKey, model, prompt, config, cancellationToken);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini no pudo leer la planilla con el modelo {Model}.", model);
            throw new BusinessException("No se pudo leer la planilla.");
        }

        var rows = ParseRows(raw);
        if (rows.Count == 0)
            throw new BusinessException("No se encontraron partidos en la imagen.");

        _logger.LogInformation("Planilla procesada con {Model}: {RowCount} partido(s).", model, rows.Count);
        return BuildCsv(rows);
    }

    private async Task<string> GenerateAsync(
        string apiKey,
        string model,
        Content prompt,
        GenerateContentConfig config,
        CancellationToken cancellationToken)
    {
        const int attempts = 2;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var client = new Client(apiKey: apiKey, httpOptions: new HttpOptions { Timeout = TimeoutMs });
                var response = await client.Models.GenerateContentAsync(model, prompt, config, cancellationToken);
                return ReadModelText(response);
            }
            catch (ServerError ex) when (attempt < attempts)
            {
                _logger.LogWarning(ex, "Gemini no respondió (intento {Attempt}). Reintentando.", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    internal static string BuildCsv(IReadOnlyList<SheetRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append(CsvHeader);
        foreach (var row in rows)
        {
            sb.Append('\n');
            sb.Append(row.Fecha.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(CsvCell(row.Division));
            sb.Append(',');
            sb.Append(CsvCell(row.Equipo1));
            sb.Append(',');
            sb.Append(row.Goles1?.ToString(CultureInfo.InvariantCulture) ?? "");
            sb.Append(',');
            sb.Append(CsvCell(row.Equipo2));
            sb.Append(',');
            sb.Append(row.Goles2?.ToString(CultureInfo.InvariantCulture) ?? "");
            sb.Append(',');
            sb.Append(CsvCell(row.Estado));
        }
        return sb.ToString();
    }

    internal static List<SheetRow> ParseRows(string raw)
    {
        var json = ExtractJson(raw);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new BusinessException("La lectura de la planilla no devolvió un resultado utilizable. Probá con una imagen más nítida.");
        }

        using (document)
        {
            var root = document.RootElement;
            JsonElement array;
            if (root.ValueKind == JsonValueKind.Array)
                array = root;
            else if (root.ValueKind == JsonValueKind.Object && TryProperty(root, out array, "rows", "partidos", "matches"))
            {
                if (array.ValueKind != JsonValueKind.Array)
                    throw new BusinessException("La lectura de la planilla no devolvió partidos.");
            }
            else
                throw new BusinessException("La lectura de la planilla no devolvió partidos.");

            var rows = new List<SheetRow>();
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                var row = MapRow(item);
                if (row != null)
                    rows.Add(row);
            }
            return rows;
        }
    }

    private static SheetRow? MapRow(JsonElement item)
    {
        if (!TryProperty(item, out var fechaEl, "fecha", "round") || !TryReadInt(fechaEl, out var fecha) || fecha <= 0)
            return null;

        var division = ReadString(item, "division", "división", "categoria", "categoría");
        var equipo1 = ReadString(item, "equipo1", "equipo_1", "equipoA", "equipo_a");
        var equipo2 = ReadString(item, "equipo2", "equipo_2", "equipoB", "equipo_b");
        int? goles1 = TryProperty(item, out var g1, "goles1", "goles_1", "golesEquipo1") && TryReadInt(g1, out var hg) ? hg : null;
        int? goles2 = TryProperty(item, out var g2, "goles2", "goles_2", "golesEquipo2") && TryReadInt(g2, out var ag) ? ag : null;
        var estadoRaw = ReadString(item, "estado", "status");

        if (string.IsNullOrWhiteSpace(division) || (string.IsNullOrWhiteSpace(equipo1) && string.IsNullOrWhiteSpace(equipo2)))
            return null;
        if (division.Length > 40 || equipo1.Length > 120 || equipo2.Length > 120)
            return null;
        if (goles1 < 0 || goles2 < 0)
            return null;

        return new SheetRow
        {
            Fecha = fecha,
            Division = division,
            Equipo1 = equipo1,
            Goles1 = goles1,
            Goles2 = goles2,
            Equipo2 = equipo2,
            Estado = NormalizeEstado(estadoRaw, equipo1, equipo2, goles1, goles2),
        };
    }

    internal static string NormalizeEstado(string raw, string equipo1, string equipo2, int? goles1, int? goles2)
    {
        var s = Fold(raw);
        var hasTeams = !string.IsNullOrWhiteSpace(equipo1) && !string.IsNullOrWhiteSpace(equipo2);
        var hasScore = goles1 is not null && goles2 is not null;

        if (s.Contains("libre") || s.Contains("bye") || s.Contains("descansa") || !hasTeams)
            return "Libre";
        if (hasScore)
            return "Finalizado";
        return "Partido Suspendido";
    }

    private static Schema BuildSchema()
    {
        return new Schema
        {
            Type = GenAiType.Object,
            Title = "PlanillaResultados",
            Required = new List<string> { "rows" },
            Properties = new Dictionary<string, Schema>
            {
                ["rows"] = new Schema
                {
                    Type = GenAiType.Array,
                    Items = new Schema
                    {
                        Type = GenAiType.Object,
                        Required = new List<string> { "fecha", "division", "equipo1", "equipo2", "estado" },
                        PropertyOrdering = new List<string> { "fecha", "division", "equipo1", "goles1", "equipo2", "goles2", "estado" },
                        Properties = new Dictionary<string, Schema>
                        {
                            ["fecha"] = new Schema
                            {
                                Type = GenAiType.Integer,
                                Description = "Número de fecha del torneo tomado del título más cercano arriba de la fila. RESULTADOS FECHA 7 => 7. Nunca uses la fecha calendario de la columna FECHA (19/9/2026).",
                            },
                            ["division"] = new Schema
                            {
                                Type = GenAiType.String,
                                Description = "Texto exacto de la columna DIVISION de esa fila, por ejemplo A, B, C, SUPER 45 A, SUPER 45 B o SENIOR.",
                            },
                            ["equipo1"] = new Schema
                            {
                                Type = GenAiType.String,
                                Description = "Nombre exacto del equipo de la columna EQUIPO A, con tildes y mayúsculas.",
                            },
                            ["goles1"] = new Schema
                            {
                                Type = GenAiType.Integer,
                                Nullable = true,
                                Description = "Goles de EQUIPO A. En un resultado 5 VS 0, goles1 es 5. Null si no hay marcador.",
                            },
                            ["equipo2"] = new Schema
                            {
                                Type = GenAiType.String,
                                Description = "Nombre exacto del equipo de la columna EQUIPO B.",
                            },
                            ["goles2"] = new Schema
                            {
                                Type = GenAiType.Integer,
                                Nullable = true,
                                Description = "Goles de EQUIPO B. En un resultado 5 VS 0, goles2 es 0. Null si no hay marcador.",
                            },
                            ["estado"] = new Schema
                            {
                                Type = GenAiType.String,
                                Enum = new List<string> { "Finalizado", "Partido Suspendido", "Libre" },
                                Description = "Finalizado si hay marcador. Partido Suspendido si esa fila no se jugó, se suspendió o un equipo no se presentó. Libre si un equipo estuvo libre. Un cartel suelto entre tablas no cambia partidos que ya muestran goles.",
                            },
                        },
                    },
                },
            },
        };
    }

    private const string SystemPrompt =
        "Extraés partidos de planillas de resultados de fútbol amateur. " +
        "No inventes equipos ni partidos que no estén escritos. " +
        "fecha es el número de fecha del torneo del título más cercano por encima de la fila, nunca la fecha calendario. " +
        "division sale de la columna DIVISION, tal cual. " +
        "equipo1 y goles1 son EQUIPO A; equipo2 y goles2 son EQUIPO B. " +
        "Si el marcador está escrito como '5 VS 0', los goles son 5 y 0. " +
        "estado es Finalizado cuando hay goles de los dos equipos, Partido Suspendido cuando esa fila no se jugó o dice suspendido o no se presentó, y Libre cuando un equipo está libre. " +
        "Un cartel entre tablas, como PARTIDO SUSPENDIDO o un equipo que no se presentó, no pisa filas que ya tienen marcador y no justifica inventar un partido si no se identifican los dos equipos.";

    private string? FirstConfigured(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    private static string ReadModelText(GenerateContentResponse response)
    {
        var parts = response.Candidates?.FirstOrDefault()?.Content?.Parts;
        if (parts != null)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Thought == true || string.IsNullOrEmpty(part.Text))
                    continue;
                sb.Append(part.Text);
            }
            if (sb.Length > 0)
                return sb.ToString();
        }

        if (!string.IsNullOrWhiteSpace(response.Text))
            return response.Text;

        throw new BusinessException("Gemini no devolvió texto para esta imagen.");
    }

    private static string ExtractJson(string text)
    {
        var t = text.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = t.IndexOf('\n');
            if (firstNl >= 0)
                t = t[(firstNl + 1)..];
            if (t.EndsWith("```", StringComparison.Ordinal))
                t = t[..^3];
            t = t.Trim();
        }

        var startObj = t.IndexOf('{');
        var startArr = t.IndexOf('[');
        var start = startObj < 0 ? startArr : startArr < 0 ? startObj : Math.Min(startObj, startArr);
        if (start < 0)
            return t;

        var endObj = t.LastIndexOf('}');
        var endArr = t.LastIndexOf(']');
        var end = Math.Max(endObj, endArr);
        if (end <= start)
            return t;
        return t[start..(end + 1)];
    }

    private static bool TryProperty(JsonElement obj, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.TryGetProperty(name, out value))
                return true;
        }

        foreach (var prop in obj.EnumerateObject())
        {
            foreach (var name in names)
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string ReadString(JsonElement obj, params string[] names)
    {
        if (!TryProperty(obj, out var el, names) || el.ValueKind != JsonValueKind.String)
            return "";
        return CollapseSpaces(el.GetString());
    }

    private static bool TryReadInt(JsonElement el, out int value)
    {
        value = 0;
        if (el.ValueKind == JsonValueKind.Number)
            return el.TryGetInt32(out value);
        if (el.ValueKind == JsonValueKind.String)
            return int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        return false;
    }

    private static string CollapseSpaces(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var parts = value.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    private static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var form = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CsvCell(string? value)
    {
        var v = (value ?? "").Replace("\r", " ").Replace("\n", " ");
        if (v.Contains('"') || v.Contains(','))
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }

    private static string? NormalizeMime(string? mimeType)
    {
        var mime = (mimeType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        return mime switch
        {
            "image/jpg" or "image/jpeg" => "image/jpeg",
            "image/png" => "image/png",
            "image/webp" => "image/webp",
            "image/gif" => "image/gif",
            _ => null,
        };
    }

    internal sealed class SheetRow
    {
        public int Fecha { get; set; }
        public string Division { get; set; } = "";
        public string Equipo1 { get; set; } = "";
        public int? Goles1 { get; set; }
        public string Equipo2 { get; set; } = "";
        public int? Goles2 { get; set; }
        public string Estado { get; set; } = "";
    }
}
