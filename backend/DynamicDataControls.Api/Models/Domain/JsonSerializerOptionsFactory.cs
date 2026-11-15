using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicDataControls.Api.Models.Domain;

/// <summary>
/// Creates the shared <see cref="JsonSerializerOptions"/> used for serializing and
/// deserializing <see cref="ControlDefinition"/> JSON throughout the application.
/// <para>Configured options:</para>
/// <list type="bullet">
///   <item><see cref="JsonNamingPolicy.CamelCase"/> — matches Angular/TypeScript conventions.</item>
///   <item><see cref="JsonStringEnumConverter"/> — enums are serialized as strings (e.g. "LessThan") not integers.</item>
///   <item><see cref="JsonIgnoreCondition.WhenWritingNull"/> — null properties are omitted on write.</item>
/// </list>
/// Requirements: 2.8, 3.7
/// </summary>
public static class JsonSerializerOptionsFactory
{
    /// <summary>
    /// Creates a new, pre-configured <see cref="JsonSerializerOptions"/> instance.
    /// Callers may cache the returned instance — it is not mutated after creation.
    /// </summary>
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy         = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition       = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive  = true,   // tolerate PascalCase from legacy clients
            WriteIndented                = false,
        };

        // Enums as human-readable strings — aligns with OperatorType / LogicalOperator
        // values stored in the DefinitionJson column and exchanged with Angular.
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
