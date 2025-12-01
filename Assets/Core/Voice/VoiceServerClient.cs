using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using LostSpells.Data;

namespace LostSpells.Systems
{
    public class VoiceServerClient : MonoBehaviour
    {
        [Header("Server Settings")]
        [Tooltip("서버 URL (예: http://localhost:8000)")]
        public string serverUrl = "http://localhost:8000";

        private List<string> currentSkillKeywords = new List<string>();
        private string currentLanguage = "ko"; // 기본값: 한국어

        private void Start()
        {
            // 서버 연결 테스트
            StartCoroutine(CheckServerStatus());
        }

        /// <summary>
        /// 서버 상태 확인
        /// </summary>
        public IEnumerator CheckServerStatus()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/"))
            {
                yield return request.SendWebRequest();

                // 음성인식 서버 연결 실패는 정상 상황이므로 경고 로그 제거
                // 서버가 필요한 경우에만 사용자가 수동으로 시작하면 됨
            }
        }

        /// <summary>
        /// 스킬 목록을 서버에 설정
        /// 새 서버는 /recognize 요청 시 스킬을 함께 전달하므로, 여기서는 키워드만 저장
        /// </summary>
        public IEnumerator SetSkills(List<SkillData> skillList)
        {
            // 스킬에서 음성 키워드만 추출 (현재 언어에 맞게)
            currentSkillKeywords.Clear();
            foreach (var skill in skillList)
            {
                // 현재 언어에 맞는 스킬 이름을 키워드로 사용
                string keyword = GetVoiceKeywordForCurrentLanguage(skill);
                if (!string.IsNullOrEmpty(keyword))
                {
                    currentSkillKeywords.Add(keyword);
                }
            }

            if (currentSkillKeywords.Count == 0)
            {
                Debug.LogWarning("음성 키워드가 설정된 스킬이 없습니다.");
            }

            yield break;
        }

        /// <summary>
        /// 현재 언어에 맞는 음성 키워드 반환
        /// </summary>
        private string GetVoiceKeywordForCurrentLanguage(SkillData skill)
        {
            // 한국어면 한국어 이름, 영어면 영어 이름 사용
            if (currentLanguage == "ko")
            {
                return !string.IsNullOrEmpty(skill.skillName) ? skill.skillName : skill.skillNameEn;
            }
            else
            {
                return !string.IsNullOrEmpty(skill.skillNameEn) ? skill.skillNameEn : skill.skillName;
            }
        }

        /// <summary>
        /// 음성 인식 언어 설정
        /// </summary>
        public void SetLanguage(string languageCode)
        {
            currentLanguage = languageCode;
        }

        /// <summary>
        /// 음성 파일을 서버로 전송하고 스킬 인식 결과 받기
        /// </summary>
        public IEnumerator RecognizeSkill(byte[] audioData, Action<RecognitionResult> callback)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("audio", audioData, "recording.wav", "audio/wav");
            form.AddField("language", currentLanguage);
            form.AddField("skills", string.Join(",", currentSkillKeywords)); // 스킬 키워드 전달

            using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/recognize", form))
            {
                request.timeout = 60;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    RecognitionResult result = ParseRecognitionResult(jsonResponse);
                    callback?.Invoke(result);
                }
                else
                {
                    Debug.LogWarning($"음성 인식 실패: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 현재 설정된 스킬 키워드 조회
        /// </summary>
        public List<string> GetCurrentSkillKeywords()
        {
            return currentSkillKeywords;
        }

        /// <summary>
        /// 사용 가능한 모델 목록 조회
        /// </summary>
        public IEnumerator GetAvailableModels(Action<ModelsInfo> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/models"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    ModelsInfo modelsInfo = JsonUtility.FromJson<ModelsInfo>(jsonResponse);
                    callback?.Invoke(modelsInfo);
                }
                else
                {
                    Debug.LogWarning($"모델 목록 조회 실패: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 모델 선택 및 로드
        /// </summary>
        public IEnumerator SelectModel(string modelSize, Action<bool> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField("model_size", modelSize);

            using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/models/select", form))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"모델 변경 실패: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// 모델 다운로드
        /// </summary>
        public IEnumerator DownloadModel(string modelSize, Action<bool> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField("model_size", modelSize);

            using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/models/download", form))
            {
                // 모델 다운로드는 시간이 오래 걸릴 수 있으므로 타임아웃 연장
                request.timeout = 600; // 10분

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"모델 다운로드 실패: {request.error}");
                    callback?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// JSON 응답을 RecognitionResult로 파싱
        /// 새 서버 형식: {success, text, matched_skill, confidence, candidates, processing_time}
        /// </summary>
        private RecognitionResult ParseRecognitionResult(string json)
        {
            var result = new RecognitionResult();
            result.skill_scores = new Dictionary<string, float>();

            try
            {
                // JsonUtility로 기본 파싱
                var serverResponse = JsonUtility.FromJson<ServerRecognitionResponse>(json);

                if (!serverResponse.success)
                {
                    result.status = "error";
                    return result;
                }

                // 새 형식을 기존 형식으로 변환
                result.status = "success";
                result.recognized_text = serverResponse.text;
                result.processing_time = serverResponse.processing_time;

                result.action = serverResponse.action;
                result.order = serverResponse.order;

                Debug.Log("스킬명: " + serverResponse.matched_skill);
                // best_match 설정
                result.best_match = new BestMatch
                {
                    skill = serverResponse.matched_skill ?? "",
                    score = serverResponse.confidence
                };

                // candidates를 skill_scores로 변환
                if (serverResponse.candidates != null)
                {
                    foreach (var candidate in serverResponse.candidates)
                    {
                        result.skill_scores[candidate.name] = candidate.confidence;
                    }
                }
                result.direction = serverResponse.direction;
                result.location = serverResponse.location;

            }
            catch (Exception e)
            {
                Debug.LogWarning($"JSON 파싱 실패: {e.Message}");
                result.status = "error";
            }

            return result;
        }

        [Serializable]
        private class ServerRecognitionResponse
        {
            public string action;
            public string order;
            public bool success;
            public string text;
            public string matched_skill;
            public float confidence;
            public SkillCandidate[] candidates;
            public float processing_time;
            public string error;
            public string direction;
            public int location;
        }

        [Serializable]
        private class SkillCandidate
        {
            public string name;
            public float confidence;
        }
    }

    /// <summary>
    /// 인식 결과 데이터 구조
    /// </summary>
    [Serializable]
    public class RecognitionResult
    {
        public string action;
        public string order;
        public string status;
        public string recognized_text;
        public float processing_time;
        public BestMatch best_match;
        public System.Collections.Generic.Dictionary<string, float> skill_scores;
        public string direction;
        public int location;
    }

    [Serializable]
    public class BestMatch
    {
        public string skill;
        public float score;
    }

    /// <summary>
    /// 모델 정보
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
    /// 모델 목록 정보
    /// </summary>
    [Serializable]
    public class ModelsInfo
    {
        public string status;
        public string current_model;
        // Note: Unity의 JsonUtility는 Dictionary를 직접 지원하지 않으므로,
        // 실제 사용 시에는 수동 파싱이 필요할 수 있습니다.
    }
}
