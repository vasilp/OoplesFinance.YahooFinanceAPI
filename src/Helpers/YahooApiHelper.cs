using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("OoplesFinance.YahooFinanceAPI.Tests.Unit")]

namespace OoplesFinance.YahooFinanceAPI.Helpers;

public sealed class YahooApiHelper(HttpClient client)
{
    private string? _crumb;

    public HttpClient Client = client;

    /// <summary> Crumb value for the Yahoo Finance API </summary>
    public async Task<string> GetCrumb()
    {
        if (!string.IsNullOrWhiteSpace(_crumb))
            return _crumb;

        // GET request to 'https://www.yahoo.com/' to get necessary cookies
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        var yahooResponse = await Client.GetAsync("https://finance.yahoo.com/");

        if (yahooResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Error accessing finance.yahoo.com: {yahooResponse.ReasonPhrase}");
        }

        // GET request to fetch the 'crumb' value
        Client.DefaultRequestHeaders.Add("Origin", "https://finance.yahoo.com");
        Client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
        Client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        Client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        var crumbResponse = await Client.GetAsync("https://query2.finance.yahoo.com/v1/test/getcrumb");

        if (crumbResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Error accessing query2.finance.yahoo.com/v1/test/getcrumb: {crumbResponse.ReasonPhrase}");
        }

        Client.DefaultRequestHeaders.Accept.Clear();

        var crumbContent = await crumbResponse.Content.ReadAsStringAsync();
        _crumb = Uri.EscapeDataString(crumbContent);

        return _crumb;
    }
}

internal class RedirectHandler : HttpClientHandler
{
    private string? _gcrumb = "";

    public RedirectHandler(CookieContainer cookieContainer)
    {
        UseCookies = true;
        CookieContainer = cookieContainer;
        AllowAutoRedirect = false;
        MaxAutomaticRedirections = 10;
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var redirectCount = 0;
        const int maxRedirects = 10;
        var currentRequest = request;

        while (true)
        {
            // Attach cookies to the request
            AttachCookies(currentRequest);

            var response = await base.SendAsync(currentRequest, cancellationToken);

            // Process cookies from the response
            ProcessResponseCookies(response, currentRequest.RequestUri);

            if (IsRedirect(response.StatusCode))
            {
                if (++redirectCount > maxRedirects)
                {
                    throw new HttpRequestException("Too many redirects");
                }

                var redirectUri = response.Headers.Location;

                if (redirectUri != null && !redirectUri.IsAbsoluteUri)
                {
                    if (currentRequest.RequestUri != null)
                        redirectUri = new Uri(currentRequest.RequestUri, redirectUri);
                }

                if (redirectUri != null && redirectUri.AbsolutePath == "/consent")
                {
                    var queryParams = HttpUtility.ParseQueryString(redirectUri.Query);
                    _gcrumb = queryParams["gcrumb"];

                    currentRequest = new HttpRequestMessage(HttpMethod.Get, redirectUri);
                }
                else if (redirectUri != null && redirectUri.AbsolutePath == "/v2/collectConsent")
                {
                    var queryParams = HttpUtility.ParseQueryString(redirectUri.Query);
                    var sessionId = queryParams["sessionId"];

                    var formData = new Dictionary<string, string>
                        {
                            {"csrfToken", _gcrumb ?? ""},
                            {"sessionId", sessionId ?? ""},
                            {"originalDoneUrl", "https://www.yahoo.com/?guccounter=1"},
                            {"namespace", "yahoo"},
                            // Choose to 'agree' or 'reject' consent
                            {"accept", "accept"}
                        };

                    var content = new FormUrlEncodedContent(formData);

                    currentRequest = new HttpRequestMessage(HttpMethod.Post, redirectUri)
                    {
                        Content = content
                    };
                }
                else if (redirectUri != null && redirectUri.AbsolutePath == "/copyConsent")
                {
                    currentRequest = new HttpRequestMessage(HttpMethod.Get, redirectUri);

                }
                else
                {
                    // Follow other redirects
                    var method = currentRequest.Method;
                    if (method == HttpMethod.Get || method == HttpMethod.Head)
                    {
                        currentRequest = new HttpRequestMessage(method, redirectUri);
                        currentRequest.Headers.Referrer = currentRequest.RequestUri;
                    }
                    else
                    {
                        // Do not redirect POST unless allowed
                        return response;
                    }
                }
            }
            else
            {
                return response;
            }
        }
    }

    private void AttachCookies(HttpRequestMessage request)
    {
        if (request.RequestUri == null)
            return;

        var cookieHeader = CookieContainer.GetCookieHeader(request.RequestUri);
        if (string.IsNullOrEmpty(cookieHeader))
            return;

        if (request.Headers.Contains("Cookie"))
        {
            request.Headers.Remove("Cookie");
        }
        request.Headers.Add("Cookie", cookieHeader);
    }

    private void ProcessResponseCookies(HttpResponseMessage response, Uri? requestUri)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
            return;

        if (requestUri == null)
            return;

        foreach (var header in cookieHeaders)
        {
            // Skip invalid headers
            if (header.StartsWith("$Version=DELETE;") ||
                header.StartsWith("$Path=DELETE;") ||
                header.StartsWith("$Domain=DELETE;"))
            {
                continue;
            }

            CookieContainer.SetCookies(requestUri, header);
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code >= 300 && code <= 399;
    }
}


internal class DownloadThrottleQueueHandler : RedirectHandler
{
    #region Fields

    private readonly TimeSpan _maxPeriod;
    private readonly SemaphoreSlim _throttleLoad, _throttleRate;

    #endregion

    public DownloadThrottleQueueHandler(CookieContainer cookieContainer, int maxPerPeriod, TimeSpan maxPeriod, int maxParallel = -1)
        : base(cookieContainer)
    {
        if (maxParallel < 0 || maxParallel > maxPerPeriod)
        {
            maxParallel = maxPerPeriod;
        }

        _throttleLoad = new SemaphoreSlim(maxParallel, maxParallel);
        _throttleRate = new SemaphoreSlim(maxPerPeriod, maxPerPeriod);
        _maxPeriod = maxPeriod;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _throttleLoad.WaitAsync(cancellationToken); // Allow bursts up to maxParallel requests at once
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _throttleRate.WaitAsync(cancellationToken);

            // Release after period [Note: Intentionally not awaited]
            // - Do not allow more than maxPerPeriod requests per period
            _ = Task.Delay(_maxPeriod, cancellationToken)
                .ContinueWith(tt => { _throttleRate.Release(1); }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            _throttleLoad.Release();
        }
    }
}
