using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

/// <summary>
/// Resolves the transport described by <see cref="PublicationWebBinding"/> into an
/// embedded source snapshot. The binding is deliberately independent from charts so
/// later web-content and streaming adapters can reuse the same request contract.
/// </summary>
public sealed class PublicationWebDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PublicationWebhookStore _webhooks;
    private readonly PublicationDataService _data;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bindingLocks = new();

    public PublicationWebDataService(IHttpClientFactory httpClientFactory, PublicationWebhookStore webhooks, PublicationDataService data)
    {
        _httpClientFactory = httpClientFactory;
        _webhooks = webhooks;
        _data = data;
    }

    public async Task RefreshAsync(PublicationDataObject dataObject, CancellationToken cancellationToken = default)
    {
        if (dataObject.SourceKind != PublicationDataSourceKind.Web)
            throw new InvalidOperationException("The selected data object is not web-backed.");
        var binding = dataObject.Web ??= new PublicationWebBinding();
        if (!binding.Enabled) return;

        var gate = _bindingLocks.GetOrAdd(binding.Id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            string content;
            string contentType;
            if (binding.Transport == PublicationWebTransportKind.Webhook)
            {
                _webhooks.Register(binding.Id, binding.WebhookToken);
                if (!_webhooks.TryGet(binding.Id, out var payload))
                    throw new InvalidOperationException("No webhook payload has been received yet.");
                content = payload.Content;
                contentType = payload.ContentType;
            }
            else if (binding.Transport == PublicationWebTransportKind.Stream)
            {
                throw new NotSupportedException("Continuous stream transport is reserved for the future streaming workbench. Use REST polling for chart data in this release.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(binding.Url)) throw new InvalidDataException("Enter a web data URL.");
                var requestUri = ResolveUri(binding.Url, binding.Transport);
                using var request = new HttpRequestMessage(ToHttpMethod(binding.Method), requestUri);
                if (binding.Method is PublicationWebHttpMethod.Post or PublicationWebHttpMethod.Put or PublicationWebHttpMethod.Patch)
                    request.Content = new StringContent(binding.RequestBody ?? string.Empty, Encoding.UTF8, "application/json");

                foreach (var header in binding.Headers.Where(header => !string.IsNullOrWhiteSpace(header.Name)))
                {
                    var name = header.Name.Trim();
                    var value = header.Value ?? string.Empty;
                    if (request.Headers.TryAddWithoutValidation(name, value)) continue;
                    request.Content ??= new ByteArrayContent([]);
                    request.Content.Headers.Remove(name);
                    request.Content.Headers.TryAddWithoutValidation(name, value);
                }

                var client = _httpClientFactory.CreateClient(nameof(PublicationWebDataService));
                client.Timeout = binding.TimeoutSeconds <= 0
                    ? Timeout.InfiniteTimeSpan
                    : TimeSpan.FromSeconds(binding.TimeoutSeconds);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var responseReader = new StreamReader(responseStream, detectEncodingFromByteOrderMarks: true);
                content = await responseReader.ReadToEndAsync(cancellationToken);
                contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"The endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForError(content)}");
            }

            dataObject.RawSource = content;
            binding.LastContentType = contentType;
            binding.LastSuccessUtc = DateTimeOffset.UtcNow;
            binding.LastError = string.Empty;
            _data.ParseInto(dataObject);
        }
        catch (Exception ex)
        {
            binding.LastError = ex.Message;
            if (!binding.UseSnapshotOnFailure || dataObject.Rows.Count == 0) throw;
        }
        finally
        {
            gate.Release();
        }
    }

    public bool IsDue(PublicationDataObject dataObject, DateTimeOffset now)
    {
        if (dataObject.SourceKind != PublicationDataSourceKind.Web || !dataObject.Web.Enabled) return false;
        if (dataObject.Web.RefreshIntervalSeconds <= 0) return false;
        return dataObject.Web.LastSuccessUtc is null || now - dataObject.Web.LastSuccessUtc.Value >= TimeSpan.FromSeconds(dataObject.Web.RefreshIntervalSeconds);
    }

    private static Uri ResolveUri(string value, PublicationWebTransportKind transport)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute)) return absolute;
        if (transport != PublicationWebTransportKind.MonolithApi)
            throw new InvalidDataException("REST API URLs must be absolute. Relative URLs are reserved for monolith API routes.");
        var baseUrl = RuntimeEndpointStore.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new InvalidOperationException("The local PublisherStudio server address is not available yet.");
        return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), value.TrimStart('/'));
    }

    private static HttpMethod ToHttpMethod(PublicationWebHttpMethod method) => method switch
    {
        PublicationWebHttpMethod.Post => HttpMethod.Post,
        PublicationWebHttpMethod.Put => HttpMethod.Put,
        PublicationWebHttpMethod.Patch => HttpMethod.Patch,
        PublicationWebHttpMethod.Delete => HttpMethod.Delete,
        _ => HttpMethod.Get
    };

    private static string TrimForError(string value)
    {
        value = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        return value.Length <= 300 ? value : value[..300] + "…";
    }
}

public static class RuntimeEndpointStore
{
    public static string BaseUrl { get; set; } = string.Empty;
}
