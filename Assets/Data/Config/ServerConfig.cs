using UnityEngine;

namespace LostSpells.Data.Config
{
    /// <summary>
    /// 중앙화된 서버 설정
    /// 모든 서버 관련 설정을 한 곳에서 관리
    /// </summary>
    [CreateAssetMenu(fileName = "ServerConfig", menuName = "LostSpells/Config/Server Config")]
    public class ServerConfig : ScriptableObject
    {
        [Header("Server Connection")]
        [Tooltip("Voice recognition server base URL")]
        public string serverUrl = "http://localhost:8000";

        [Tooltip("Connection timeout in seconds")]
        public int connectionTimeout = 30;

        [Tooltip("Request timeout in seconds")]
        public int requestTimeout = 60;

        [Header("Voice Recognition")]
        [Tooltip("Default language code (ko, en, ja, zh)")]
        public string defaultLanguage = "ko";

        [Tooltip("Default Whisper model size")]
        public string defaultModelSize = "base";

        [Tooltip("Minimum confidence score for skill recognition")]
        [Range(0f, 1f)]
        public float minimumConfidence = 0.7f;

        [Header("API Endpoints")]
        public string healthCheckEndpoint = "/";
        public string recognizeEndpoint = "/recognize";
        public string setSkillsEndpoint = "/set-skills";
        public string getSkillsEndpoint = "/skills";
        public string modelsEndpoint = "/models";
        public string selectModelEndpoint = "/models/select";
        public string downloadModelEndpoint = "/models/download";
        public string modelStatusEndpoint = "/models/{0}/status";
        public string deleteModelEndpoint = "/models/{0}";

        // Singleton pattern for easy access
        private static ServerConfig _instance;
        public static ServerConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ServerConfig>("Config/ServerConfig");
                    if (_instance == null)
                    {
                        Debug.LogError("ServerConfig asset not found in Resources/Config/! Creating default instance.");
                        _instance = CreateInstance<ServerConfig>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Get full URL for an endpoint
        /// </summary>
        public string GetUrl(string endpoint)
        {
            return serverUrl.TrimEnd('/') + endpoint;
        }

        /// <summary>
        /// Get formatted URL with parameters
        /// </summary>
        public string GetUrl(string endpointFormat, params object[] args)
        {
            string endpoint = string.Format(endpointFormat, args);
            return GetUrl(endpoint);
        }
    }
}
