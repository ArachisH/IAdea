using System.Net.Http.Json;
using System.Text.Json.Nodes;

using IAdea.Json;

namespace IAdea;

public sealed class DeviceSession
{
    private const string DEFAULT_USERNAME = "admin";

    private static readonly HttpClient _httpClient;
    private static readonly HttpClientHandler _httpClientHandler;

    public readonly Uri _baseUri;
    private readonly string _password;

    public string Username { get; }
    public string? AccessToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    static DeviceSession()
    {
        _httpClientHandler = new HttpClientHandler();
        _httpClient = new HttpClient(_httpClientHandler)
        {
            Timeout = TimeSpan.FromSeconds(5 * 60) // 5 Minute Timeout
        };
    }
    public DeviceSession(string address)
        : this(address, DEFAULT_USERNAME, string.Empty)
    { }
    public DeviceSession(string address, string username, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        Username = username;
        _password = password ?? string.Empty; // Assume no password has been set on the device.
        _baseUri = new Uri($"http://{address}:8080/v2");
    }

    public async Task<bool> AuthenticateAsync()
    {
        var formData = new KeyValuePair<string, string>[3]
        {
            new("grant_type", "password"),
            new("username", Username),
            new("password", _password)
        };
        using var formContent = new FormUrlEncodedContent(formData);

        using HttpResponseMessage response = await _httpClient.PostAsync(_baseUri.AbsoluteUri + "/oauth2/token", formContent).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return false;

        var responseJsonContent = await response.Content.ReadFromJsonAsync<JsonObject>().ConfigureAwait(false);
        if (responseJsonContent == null) return false;

        if (!responseJsonContent.TryGetPropertyValue("access_token", out JsonNode? accessTokenNode)) return false;
        AccessToken = accessTokenNode!.GetValue<string>();

        return true;
    }

    #region Media Transfer
    public async Task<IAFileResources?> FindFilesAsync(int maxResult = 500, int pageToken = 0)
    {
        var formData = new KeyValuePair<string, string>[2]
        {
            new("maxResults", maxResult.ToString()),
            new("pageToken", pageToken.ToString())
        };
        using var formContent = new FormUrlEncodedContent(formData);

        using HttpResponseMessage response = await _httpClient.PostAsync(GetPathWithToken("/files/find"), formContent).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<IAFileResources>().ConfigureAwait(false) : null;
    }
    #endregion

    #region Playback
    #endregion

    #region Hardware Management
    #endregion

    #region System Management
    #endregion

    #region App Solutions
    #endregion

    public static async IAsyncEnumerable<IAFileResource> DownloadFilesAsync(IEnumerable<IAFileResource> fileResources, Uri baseUri, string? token, string outputDirectory)
    {
        DirectoryInfo outputDirectoryInfo = new DirectoryInfo(outputDirectory);
        outputDirectoryInfo.Create();

        if (!fileResources.TryGetNonEnumeratedCount(out int totalFiles))
        {
            totalFiles = fileResources.Count();
        }

        var downloadTasks = new List<Task<byte[]>>(totalFiles);
        var resources = new Dictionary<Task<byte[]>, IAFileResource>(totalFiles);
        foreach (IAFileResource fileResource in fileResources)
        {
            Task<byte[]> resourceDownloadTask = _httpClient.GetByteArrayAsync(GetPathWithToken(baseUri, fileResource.DownloadPath, token));

            downloadTasks.Add(resourceDownloadTask);
            resources.Add(resourceDownloadTask, fileResource);
        }

        await foreach (Task<byte[]> completedDownload in Task.WhenEach(downloadTasks))
        {
            IAFileResource resource = resources[completedDownload];
            string localPath = Path.Combine(outputDirectoryInfo.FullName, Path.GetFileName(resource.DownloadPath));

            await File.WriteAllBytesAsync(localPath, await completedDownload.ConfigureAwait(false)).ConfigureAwait(false);
            yield return resources[completedDownload];
        }
    }
    public IAsyncEnumerable<IAFileResource> DownloadFilesAsync(IEnumerable<IAFileResource> fileResources, string outputDirectory) => DownloadFilesAsync(fileResources, _baseUri, AccessToken, outputDirectory);

    public string GetPathWithToken(string path) => GetPathWithToken(_baseUri, path, AccessToken);
    public static string GetPathWithToken(Uri baseUri, string path, string? token) => $"{baseUri.AbsoluteUri}{path}?access_token={token}";
}