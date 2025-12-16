// Version: 0.1.1.99
namespace Thmd.Configuration
{
    /// <summary>
    /// Represents the configuration settings for AI services such as OpenAI and Gemini.
    /// </summary>
    public class AiConfig : IConfig
    {
        private static readonly object _lock = new();
        /// <summary>
        /// API key for OpenAI services.
        /// </summary>
        public string OpenApiKey { get; set; }
        /// <summary>
        /// API key for Gemini services.
        /// </summary>
        public string GeminiApiKey { get; set; }

        /// <summary>
        /// Loads the AI configuration from the JSON file.
        /// </summary>
        public void Load()
        {
            lock (_lock)
            {
                var loadedConfig = Config.LoadFromJsonFile<AiConfig>(Config.AiConfigPath);
                OpenApiKey = loadedConfig.OpenApiKey;
                GeminiApiKey = loadedConfig.GeminiApiKey;
            }
        }

        /// <summary>
        /// Saves the AI configuration to the JSON file.
        /// </summary>
        public void Save()
        {
            lock (_lock)
            {
                Config.SaveToFile(Config.AiConfigPath, this);
            }
        }
    }
}
