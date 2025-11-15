using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostSpells.Data.Models.Server
{
    /// <summary>
    /// Server health check response
    /// </summary>
    [Serializable]
    public class ServerHealthResponse
    {
        public string status;
        public string message;
        public string[] current_skills;
    }

    /// <summary>
    /// Voice recognition request
    /// </summary>
    [Serializable]
    public class RecognitionRequest
    {
        public byte[] audioData;
        public string language;
        public string skills; // comma-separated
    }

    /// <summary>
    /// Voice recognition response
    /// </summary>
    [Serializable]
    public class RecognitionResponse
    {
        public string status;
        public string recognized_text;
        public float processing_time;
        public Dictionary<string, float> skill_scores;
        public BestMatch best_match;
        public string note; // For test mode messages
    }

    [Serializable]
    public class BestMatch
    {
        public string skill;
        public float score;
    }

    /// <summary>
    /// Set skills request
    /// </summary>
    [Serializable]
    public class SetSkillsRequest
    {
        public string skills; // comma-separated
    }

    /// <summary>
    /// Set skills response
    /// </summary>
    [Serializable]
    public class SetSkillsResponse
    {
        public string status;
        public string message;
        public string[] skills;
    }

    /// <summary>
    /// Model information
    /// </summary>
    [Serializable]
    public class ModelInfo
    {
        public string name;
        public string description;
        public string size;
        public bool downloaded;
    }

    /// <summary>
    /// Available models response
    /// </summary>
    [Serializable]
    public class ModelsResponse
    {
        public string status;
        public string current_model;
        public Dictionary<string, ModelInfo> available_models;
    }

    /// <summary>
    /// Model selection request
    /// </summary>
    [Serializable]
    public class SelectModelRequest
    {
        public string model_size;
    }

    /// <summary>
    /// Model selection response
    /// </summary>
    [Serializable]
    public class SelectModelResponse
    {
        public string status;
        public string message;
        public string current_model;
    }

    /// <summary>
    /// Model download request
    /// </summary>
    [Serializable]
    public class DownloadModelRequest
    {
        public string model_size;
    }

    /// <summary>
    /// Model download response
    /// </summary>
    [Serializable]
    public class DownloadModelResponse
    {
        public string status;
        public string message;
        public string model_size;
    }

    /// <summary>
    /// Model status response
    /// </summary>
    [Serializable]
    public class ModelStatusResponse
    {
        public bool downloaded;
        public int download_progress; // 0-100
        public string status; // "downloaded", "not_downloaded", "downloading"
    }

    /// <summary>
    /// Model delete response
    /// </summary>
    [Serializable]
    public class DeleteModelResponse
    {
        public string status;
        public string message;
        public string model_size;
    }

    /// <summary>
    /// Generic error response
    /// </summary>
    [Serializable]
    public class ErrorResponse
    {
        public string detail;
    }
}
