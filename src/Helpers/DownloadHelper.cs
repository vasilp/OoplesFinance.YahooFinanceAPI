namespace OoplesFinance.YahooFinanceAPI.Helpers;

public class DownloadHelper(YahooApiHelper apiHelper)
{
    /// <summary>
    /// Downloads the raw csv data using the chosen parameters
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="dataType"></param>
    /// <param name="dataFrequency"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="includeAdjustedClose"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadRawCsvDataAsync(string symbol, DataType dataType, DataFrequency dataFrequency,
        DateTime startDate, DateTime? endDate, bool includeAdjustedClose)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol Parameter Can't Be Empty Or Null");
        }

        return await DownloadRawDataAsync(BuildYahooCsvUrl(symbol, dataType, dataFrequency, startDate, endDate, includeAdjustedClose));
    }

    /// <summary>
    /// Downloads the trending json data using the chosen parameters
    /// </summary>
    /// <param name="country"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadTrendingDataAsync(Country country, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Count Must Be At Least 1 To Return Any Data");
        }

        return await DownloadRawDataAsync(BuildYahooTrendingUrl(country, count));
    }

    /// <summary>
    /// Downloads the screener json data using the chosen parameters
    /// </summary>
    /// <param name="screenerType"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadScreenerDataAsync(ScreenerType screenerType, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Count Must Be At Least 1 To Return Any Data");
        }

        return await DownloadRawDataAsync(BuildYahooScreenerUrl(screenerType, count));
    }

    /// <summary>
    /// Base method to download any raw yahoo data
    /// </summary>
    /// <param name="urlString"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    private async Task<string> DownloadRawDataAsync(string urlString)
    {
        // max length for uri
        if (urlString.Length > 2083)
        {
            throw new ArgumentException("You Have Included Too Many Symbols");
        }

        var response = await apiHelper.Client.GetAsync(urlString);

        if (response.IsSuccessStatusCode)
        {
            // Handle success
            return await response.Content.ReadAsStringAsync();
        }

        throw response.StatusCode switch
        {
            // Handle failure
            HttpStatusCode.NotFound => new InvalidOperationException("Requested Information Not Available On Yahoo Finance"),
            HttpStatusCode.Unauthorized => new InvalidOperationException("Yahoo Finance Authentication Error"),
            HttpStatusCode.Forbidden => new InvalidOperationException("Yahoo Finance Authentication Error"),
            _ => new InvalidOperationException("Yahoo Finance Server Error " + response.StatusCode),
        };
    }

    /// <summary>
    /// Downloads the recommended json data using the chosen symbol
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadRecommendDataAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol Parameter Can't Be Empty Or Null");
        }

        return await DownloadRawDataAsync(BuildYahooRecommendUrl(symbol));
    }

    /// <summary>
    /// Downloads the insights json data using the chosen symbol
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadInsightsDataAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol Parameter Can't Be Empty Or Null");
        }

        return await DownloadRawDataAsync(BuildYahooInsightsUrl(symbol));
    }

    /// <summary>
    /// Downloads the chart json data using the chosen symbol
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="timeRange"></param>
    /// <param name="timeInterval"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadChartDataAsync(string symbol, TimeRange timeRange, TimeInterval timeInterval)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol Parameter Can't Be Empty Or Null");
        }

        return await DownloadRawDataAsync(BuildYahooChartUrl(symbol, timeRange, timeInterval));
    }

    /// <summary>
    /// Downloads the spark chart json data using the chosen symbol
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="timeRange"></param>
    /// <param name="timeInterval"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadSparkChartDataAsync(string symbol, TimeRange timeRange, TimeInterval timeInterval)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol Parameter Can't Be Empty Or Null");
        }

        return await DownloadRawDataAsync(BuildYahooSparkChartUrl([symbol], timeRange, timeInterval));
    }

    /// <summary>
    /// Downloads the spark chart json data using the chosen symbols
    /// </summary>
    /// <param name="symbols"></param>
    /// <param name="timeRange"></param>
    /// <param name="timeInterval"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadSparkChartDataAsync(IEnumerable<string> symbols, TimeRange timeRange, TimeInterval timeInterval)
    {
        if (!symbols.Any())
        {
            throw new ArgumentException("Symbols Parameter Must Contain At Least One Symbol");
        }

        if (symbols.Count() > 250)
        {
            throw new ArgumentException("Symbols Parameter Can't Have More Than 250 Symbols");
        }

        return await DownloadRawDataAsync(BuildYahooSparkChartUrl(symbols, timeRange, timeInterval));
    }

    /// <summary>
    /// Downloads the stats json data using the chosen symbol
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="country"></param>
    /// <param name="language"></param>
    /// <param name="module"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadStatsDataAsync(string symbol, Country country, Language language, YahooModule module)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol Parameter Can't Be Empty Or Null");
        }

        var crumb = await apiHelper.GetCrumb();
        return await DownloadRawDataAsync(BuildYahooStatsUrl(symbol, country, language, module, crumb));
    }

    /// <summary>
    /// Downloads the real-time quote json data using the chosen symbol
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="country"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadRealTimeQuoteDataAsync(string symbol, Country country, Language language)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol Parameter Can't Be Empty Or Null");
        }

        var crumb = await apiHelper.GetCrumb();
        return await DownloadRawDataAsync(BuildYahooRealTimeQuoteUrl([symbol], country, language, crumb));
    }

    /// <summary>
    /// Downloads the real-time quote json data using the chosen symbols
    /// </summary>
    /// <param name="symbols"></param>
    /// <param name="country"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadRealTimeQuoteDataAsync(IEnumerable<string> symbols, Country country, Language language)
    {
        if (!symbols.Any())
        {
            throw new ArgumentException("Symbols Parameter Must Contain At Least One Symbol");
        }

        if (symbols.Count() > 250)
        {
            throw new ArgumentException("Symbols Parameter Can't Have More Than 250 Symbols");
        }

        var crumb = await apiHelper.GetCrumb();
        return await DownloadRawDataAsync(BuildYahooRealTimeQuoteUrl(symbols, country, language, crumb));
    }

    /// <summary>
    /// Downloads the market summary json data
    /// </summary>
    /// <param name="country"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    internal async Task<string> DownloadMarketSummaryDataAsync(Country country, Language language)
    {
        return await DownloadRawDataAsync(BuildYahooMarketSummaryUrl(country, language));
    }

    /// <summary>
    /// Downloads the autocomplete json data for a specified search term
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <param name="country"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<string> DownloadAutoCompleteDataAsync(string searchTerm, Country country, Language language)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("The Search Term Parameter Can't Be Empty Or Null");
        }

        return await DownloadRawDataAsync(BuildYahooAutoCompleteUrl(searchTerm, country, language));
    }

    /// <summary>
    /// Gets the base csv data that is used by all csv helper classes
    /// </summary>
    /// <param name="csvData"></param>
    /// <returns></returns>
    internal static IEnumerable<string[]> GetBaseCsvData(string? csvData)
    {
        IEnumerable<string[]> result = [];
        var rows = csvData?.Split('\n');

        if (rows != null && rows.Length > 1)
        {
            // We are ignoring the first row which are headers
            result = rows.Skip(1).Select(x => x.Split(','));
        }

        return result;
    }
}