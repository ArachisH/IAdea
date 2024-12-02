using System.Text.Json.Serialization;

namespace IAdea.Json;

public class IAFileResources
{
    [JsonRequired]
    [JsonPropertyName("items")]
    public required IAFileResource[] Resources { get; init; }

    [JsonPropertyName("nextPageToken")]
    public required int NextPageToken { get; init; }
}