using System.Text;
using System.Text.Json;
using DynamicDataControls.Api.Models.Domain;
using DynamicDataControls.Api.Models.DTOs;
using DynamicDataControls.Api.Services.Interfaces;

namespace DynamicDataControls.Api.Services;

/// <summary>
/// Communicates with the Python FastAPI AI service to parse a free-text description
/// into a structured <see cref="ControlDefinition"/>.
/// <para>
/// Flow:
/// <list type="number">
///   <item>Build <c>AiContextDto</c> from all entities with their fields (metadata whitelist).</item>
///   <item>POST to <c>{AiService:BaseUrl}/parse</c> with a 30-second timeout.</item>
///   <item>Validate the response schema; return field-level error list on any violation.</item>
///   <item>Map the validated response to a <see cref="ControlDefinition"/>.</item>
/// </list>
/// </para>
/// Requirements: 7.2, 7.3, 7.7, 7.8, 7.11
/// </summary>
public class AiProxyService : IAiProxyService
{
    private readonly IMetadataService _metadataService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _aiServiceBaseUrl;
    private readonly int _timeoutSeconds;

    // Shared options instance — thread-safe for concurrent reads.
    private static readonly JsonSerializerOptions _jsonOptions =
        JsonSerializerOptionsFactory.Create();

    public AiProxyService(
        IMetadataService metadataService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _metadataService = metadataService;
        _httpClientFactory = httpClientFactory;

        var section = configuration.GetSection("AiService");
        _aiServiceBaseUrl = section["BaseUrl"] ?? "http://localhost:8000";
        _timeoutSeconds = int.TryParse(section["TimeoutSeconds"], out var t) ? t : 30;
    }

    /// <inheritdoc/>
    public async Task<AiParseResult> ParseDescriptionAsync(
        string description,
        CancellationToken ct = default)
    {
        // 1. Build AiContext from all entities and their fields
        var entities = await _metadataService.GetAllEntitiesWithFieldsAsync();

        var context = new AiContextDto(
            entities.Select(e => new AiEntityDto(
                e.FriendlyName,
                e.Fields
                 .Select(f => new AiFieldDto(f.FriendlyName, f.DataType, f.Description))
                 .ToList()
            )).ToList()
        );

        var requestPayload = new AiParseRequestDto(description, context);

        // 2. POST to Python service with 30-second timeout
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

        try
        {
            var json = JsonSerializer.Serialize(requestPayload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResponse = await client.PostAsync(
                $"{_aiServiceBaseUrl}/parse", content, ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return Failure("שירות ה-AI אינו זמין");
            }

            var responseBody = await httpResponse.Content.ReadAsStringAsync(ct);

            AiParseResponseDto? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<AiParseResponseDto>(responseBody, _jsonOptions);
            }
            catch (JsonException)
            {
                return Failure("תגובה לא תקינה התקבלה מ-AI");
            }

            if (parsed is null)
            {
                return Failure("תגובה ריקה התקבלה מ-AI");
            }

            // 3. Validate response schema
            var missingFields = ValidateResponse(parsed);
            if (missingFields.Count > 0)
            {
                return new AiParseResult(false, null, null, missingFields);
            }

            // 4. Map validated response to ControlDefinition
            var definition = MapToControlDefinition(parsed);
            return new AiParseResult(true, definition, null, null);
        }
        catch (TaskCanceledException)
        {
            // Covers both HttpClient.Timeout expiry and the caller's CancellationToken
            return Failure("שירות ה-AI אינו זמין");
        }
        catch (HttpRequestException)
        {
            // Network-level failures (connection refused, DNS failure, etc.)
            return Failure("שירות ה-AI אינו זמין");
        }
    }

    // ── Schema validation ─────────────────────────────────────────────────────

    /// <summary>
    /// Validates that the AI response conforms to the expected schema.
    /// Returns a list of field paths that failed validation; empty list means valid.
    /// </summary>
    private static List<string> ValidateResponse(AiParseResponseDto response)
    {
        var missing = new List<string>();

        // controlName must be present and non-empty
        if (string.IsNullOrEmpty(response.ControlName))
            missing.Add("controlName");

        // primaryEntity must be present and non-empty
        if (string.IsNullOrEmpty(response.PrimaryEntity))
            missing.Add("primaryEntity");

        // conditions must be a non-empty array
        if (response.Conditions == null || response.Conditions.Count == 0)
        {
            missing.Add("conditions");
        }
        else
        {
            for (int i = 0; i < response.Conditions.Count; i++)
            {
                var cond = response.Conditions[i];

                if (string.IsNullOrEmpty(cond.FieldName))
                    missing.Add($"conditions[{i}].fieldName");

                // Operator must be present and parse to a known OperatorType
                if (string.IsNullOrEmpty(cond.Operator))
                    missing.Add($"conditions[{i}].operator");
                else if (!Enum.TryParse<OperatorType>(cond.Operator, ignoreCase: true, out _))
                    missing.Add($"conditions[{i}].operator");

                // Value must be present (key exists in JSON).
                // JSON null is valid for IsNull/IsNotNull operators (Value.HasValue == true,
                // ValueKind == Null). Only a completely absent key sets Value to C# null.
                if (!cond.Value.HasValue)
                    missing.Add($"conditions[{i}].value");
            }
        }

        return missing;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps a validated <see cref="AiParseResponseDto"/> to a <see cref="ControlDefinition"/>.
    /// All conditions use the primary entity as their <c>EntityName</c>.
    /// All conditions except the last receive a <c>LogicalOperator.And</c> connector.
    /// </summary>
    private static ControlDefinition MapToControlDefinition(AiParseResponseDto response)
    {
        var conditions = response.Conditions
            .Select((c, i) => new ConditionDefinition
            {
                Id              = Guid.NewGuid().ToString(),
                EntityName      = response.PrimaryEntity,
                FieldName       = c.FieldName,
                Operator        = Enum.Parse<OperatorType>(c.Operator, ignoreCase: true),
                Value           = c.Value,
                LogicalOperator = i < response.Conditions.Count - 1
                                      ? LogicalOperator.And
                                      : (LogicalOperator?)null
            })
            .ToList();

        return new ControlDefinition
        {
            PrimaryEntity      = response.PrimaryEntity,
            AdditionalEntities = Array.Empty<string>(),
            Conditions         = conditions
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AiParseResult Failure(string errorMessage)
        => new(false, null, errorMessage, null);
}
