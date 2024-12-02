using System.Diagnostics;
using System.Text.Json.Serialization;

namespace IAdea.Json;

[DebuggerDisplay("DownloadPath = {DownloadPath}")]
public class IAFileResource
{
    [JsonPropertyName("fileSize")]
    public required int FileSize { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("etag")]
    public required string ETag { get; init; }

    [JsonPropertyName("downloadPath")]
    public required string DownloadPath { get; init; }

    [JsonPropertyName("createdDate")]
    public required DateTime Created { get; init; }

    [JsonPropertyName("transferredSize")]
    public required int TransferredSize { get; init; }

    [JsonPropertyName("modifiedDate")]
    public required DateTime Modified { get; init; }

    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    [JsonPropertyName("completed")]
    public required bool IsCompleted { get; init; }
}