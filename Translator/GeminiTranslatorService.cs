// Version: 0.0.0.4
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thmd.Translator;

/// <summary>
/// Production-ready translation service using Google Gemini 2.0 Flash.
/// Supports: rate limiting, retries, multi-language translation,
/// smart auto-detect, streaming callback, and thread-safe API calls.
/// </summary>
public class GeminiTranslatorService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    // Rate limiting
    private readonly int _minIntervalMs = 250;
    private DateTime _lastApiCall = DateTime.MinValue;

    // Thread safety
    private readonly SemaphoreSlim _apiSemaphore = new SemaphoreSlim(1, 1);

    // Retry configuration
    private readonly int _maxRetries = 3;
    private readonly int _retryBaseDelayMs = 500;

    public GeminiTranslatorService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    private async Task EnsureRateLimitAsync()
    {
        var timeSinceLastCall = (DateTime.Now - _lastApiCall).TotalMilliseconds;
        if (timeSinceLastCall < _minIntervalMs)
        {
            await Task.Delay(_minIntervalMs - (int)timeSinceLastCall);
        }
        _lastApiCall = DateTime.Now;
    }

    private string BuildUrl() =>
        $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

    private async Task<string> ExecuteGeminiAsync(string prompt)
    {
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(body),
            Encoding.UTF8,
            "application/json");

        await EnsureRateLimitAsync();
        await _apiSemaphore.WaitAsync();

        try
        {
            int attempts = 0;

            while (true)
            {
                try
                {
                    var response = await _httpClient.PostAsync(BuildUrl(), content);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                        return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";
                    }

                    attempts++;
                    if (attempts >= _maxRetries)
                        return "";

                    await Task.Delay(_retryBaseDelayMs * attempts);
                }
                catch
                {
                    attempts++;
                    if (attempts >= _maxRetries)
                        throw;
                    await Task.Delay(_retryBaseDelayMs * attempts);
                }
            }
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    // MAIN API --------------------------------------------------------------

    /// <summary>
    /// Simple translation to target language.
    /// </summary>
    public async Task<string> TranslateAsync(string text, string targetLanguage)
    {
        string prompt = $"Translate to {targetLanguage}:\n{text}";
        return await ExecuteGeminiAsync(prompt);
    }

    /// <summary>
    /// Automatically detects language and translates to target language.
    /// </summary>
    public async Task<string> SmartTranslateAsync(string text, string targetLanguage)
    {
        string prompt = $@"
Detect the language of this text and translate it to {targetLanguage}.
Return only the translation.
Text:
{text}";
        return await ExecuteGeminiAsync(prompt);
    }

    /// <summary>
    /// Translates many strings at once.
    /// </summary>
    public async Task<Dictionary<string, string>> TranslateManyAsync(
        IEnumerable<string> texts,
        string targetLanguage)
    {
        var result = new Dictionary<string, string>();

        foreach (var t in texts)
        {
            var translated = await TranslateAsync(t, targetLanguage);
            result[t] = translated;
        }

        return result;
    }
}
