using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using TMPro;

public class WavToWhisper : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text analyzeResultText;
    private float startTime = 0f;
    private float endTime = 0f;
    private float timeNow = 0f;
    [System.Serializable]
    public class AudioJson
    {
        public string audioData;
    }
    private void Update()
    {
        timeNow += Time.deltaTime;
    }
    public void SendAudioToServer(AudioClip audio)
    {
        startTime = timeNow;
        StartCoroutine(PostRequest("http://127.0.0.1:8000/whisper_stt2", audio));
    }
    IEnumerator PostRequest(string url, AudioClip audio)
    {
        byte[] audioBytes = WavUtility.FromAudioClip(audio);
        string base64Audio = Convert.ToBase64String(audioBytes);

        AudioJson payload = new AudioJson();
        payload.audioData = base64Audio;

        string jsonData = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            endTime = timeNow;
            Debug.Log("Time taken for request: " + (endTime - startTime) + " seconds");
            var responseText = request.downloadHandler.text;
            var jsonObj = JObject.Parse(responseText);

            string audioBase64 = jsonObj["audio"].ToString();
            string resultText = jsonObj["text"].ToString();
            string expectation = jsonObj["expectation"].ToString();
            string skillEvidence = jsonObj["skill_evidence"].ToString();
            string location = jsonObj["location"].ToString();
            string index = jsonObj["index"].ToString();

            byte[] myAudio = Convert.FromBase64String(audioBase64);
            AudioClip clip = WavUtility.ToAudioClip(myAudio, 0, "myVoice");
            // 재생
            audioSource.clip = clip;
            audioSource.Play();

            Debug.Log(jsonObj["type"].ToString());

            if (jsonObj["type"].ToString() == "attack")
            {
                scoreText.text = "Score: " + expectation;
                Debug.Log("Transcribed Text: " + resultText);
                Debug.Log("Expectation: " + expectation);
                Debug.Log("Skill Evidence: " + skillEvidence);
                AttackOrdered(jsonObj["type"].ToString(), skillEvidence, location, index);
            }
            if (jsonObj["type"].ToString() == "move")
            {
                MoveOrdered(jsonObj["type"].ToString(), location, index);
            }
        }
        else
        {
            Debug.Log("Error: " + request.error);
        }
    }

    private void MoveOrdered(string actionType, string location, string index)
    {
        //location 방향으로 index만큼 이동
        Debug.Log($"방향: {location} " + $"몇번째: {index}");
        analyzeResultText.text = $"행동: {actionType.ToUpper()}" + "\n" + $"방향: {location}" + "\n" + $"번호: {index}";
    }
    private void AttackOrdered(string actionType, string skillEvidence, string location, string index)
    {
        //location 방향의 index번째 적에게 skillEvidence 스킬로 공격
        Debug.Log($"스킬명: {skillEvidence} " + $"방향: {location} " + $"몇번째: {index}");
        analyzeResultText.text = $"행동: {actionType.ToUpper()}" + "\n" + $"스킬: {skillEvidence}" + "\n" + $"방향: {location}" + "\n" + $"번호: {index}";
    }
}



