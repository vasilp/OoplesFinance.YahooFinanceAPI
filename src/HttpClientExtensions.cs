using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace OoplesFinance.YahooFinanceAPI;

public static class HttpClientExtensions
{
    public const string HttpClientDIName = "YahooHttpClient";

    public static IServiceCollection ConfigureYahooApiHelper(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientDIName, client =>
            {
#if DEBUG
                client.Timeout = TimeSpan.FromMinutes(5);
#endif
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Pragma", "no-cache");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var cookieContainer = new CookieContainer();

                // 40 calls in a minute, no more than 4 simultaneously
                var handler = new DownloadThrottleQueueHandler(cookieContainer, 40, TimeSpan.FromMinutes(1), 4);
                return handler;
            });

        services.AddScoped<YahooApiHelper>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient(HttpClientDIName);
            return new YahooApiHelper(client);
        });

        services.AddScoped<DownloadHelper>();

        return services;
    }
}