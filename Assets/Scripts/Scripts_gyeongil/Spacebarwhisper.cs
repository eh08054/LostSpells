using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Spacebarwhisper : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private WavToWhisper wavToWhisper;
    [SerializeField] private RegisterSkill registerSkill;
    [SerializeField] private TextAnimation textAnimation;
    [SerializeField] private PitchTest pitchTest;
    [SerializeField] private AnalyzingPitch analyzingPitch;
    [SerializeField] private Button skillSendButton;
    [SerializeField] private Image recordingField;
    [SerializeField] private Image myTimebar;
    private AudioSource audioSource;
    private bool isRecording;
    private float time;
    private readonly int recordDuration = 3;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        recordingField.gameObject.SetActive(false);
        myTimebar.gameObject.SetActive(false);
        skillSendButton.onClick.AddListener(registerSkill.sendSkills);
    }
    private void StartRecording()
    {
        Debug.Log("Started Recording");
        recordingField.gameObject.SetActive(true);
        myTimebar.gameObject.SetActive(true);
        isRecording = true;
        audioSource.clip = Microphone.Start(null, false, recordDuration, 44100);
        audioSource.enabled = true;
    }
    private void EndRecording()
    {
        Debug.Log("Ended Recording");
        int currentPosition = Microphone.GetPosition(null);
        Microphone.End(null);

        AudioClip originalClip = audioSource.clip;
        AudioClip newClip = null;

        if(originalClip != null && currentPosition > 0)
        {
            float[] recordedData = new float[currentPosition];
            originalClip.GetData(recordedData, 0);

            newClip = AudioClip.Create("NewClip", currentPosition, originalClip.channels, originalClip.frequency, false);
            newClip.SetData(recordedData, 0);

            audioSource.clip = null;
        }
        audioSource.enabled = false;
        textAnimation.StopAnimation();
        recordingField.gameObject.SetActive(false);
        myTimebar.gameObject.SetActive(false);
        isRecording = false;
        time = 0;
        progressBar.fillAmount = 0;
        wavToWhisper.SendAudioToServer(originalClip);
        analyzingPitch.AnalyzeRecordedClip(newClip);
        Destroy(originalClip);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartRecording();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            EndRecording();
        }
        if (Input.GetKey(KeyCode.Space) && isRecording)
        {
            time += Time.deltaTime;
            progressBar.fillAmount = time / recordDuration;
            if (time >= recordDuration)
            {
                time = 0;
                isRecording = false;
                EndRecording();
            }
        }
    }
}

