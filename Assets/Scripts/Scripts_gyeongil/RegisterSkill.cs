using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using Newtonsoft.Json;


public class RegisterSkill : MonoBehaviour
{
    [System.Serializable]
    public class SkillJson
    {
        public string[] skillData = new string[]{
        "Small heal",
        "Calm",
        "Cheerful",
        "Excited",
        "Friendly",
        "Metal shield",
        "Serious",
        "Urgent",
        "Fire ball"
        };
    }
    public void sendSkills()
    {
        StartCoroutine(sendSkillsToServer("http://127.0.0.1:8000/register_skill"));
    }
    IEnumerator sendSkillsToServer(string url)
    {
        SkillJson skills = new SkillJson();
        Debug.Log("Preparing to send skills: " + string.Join(", ", skills));

        string jsonData = JsonUtility.ToJson(skills);
        Debug.Log("Sending skills: " + jsonData);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Skills registered successfully.");
        }
        else
        {
            Debug.LogError("Error registering skills: " + request.error);
        }
    }
}
