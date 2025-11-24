using System;
using UnityEngine;
using UnityEngine.UI;

public class ConstantRecording : MonoBehaviour
{
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int preRollSeconds = 1;
    [SerializeField] private float threshold = 0.02f;
    [SerializeField] private float silenceTimeout = 1f;
    [SerializeField] private WavToWhisper wavToWhisper;
    [SerializeField] private RegisterSkill registerSkill;
    [SerializeField] private TextAnimation textAnimation;
    [SerializeField] private AnalyzingPitch analyzingPitch;
    [SerializeField] private Button skillSendButton;
    [SerializeField] private Image recordingField;

    private AudioSource audioSource;
    private AudioClip audioClip;
    private string micDevice;

    private bool isRecording = false;
    private float silenceTimer = 0f;
    private float recordingTimer = 0f;

    private int detectionStartIndex = 0; 
    private int sampleSize = 1024;

    private bool checkTextAnimation = false;   

    private void Start()
    {
        recordingField.gameObject.SetActive(false);
        skillSendButton.onClick.AddListener(registerSkill.sendSkills);
        audioSource = GetComponent<AudioSource>();
        audioClip = Microphone.Start(micDevice, true, 60, sampleRate);

        while (Microphone.GetPosition(micDevice) <= 0) { }
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void Update()
    {
        if (audioClip == null) return;

        int currentPos = Microphone.GetPosition(micDevice);

        float rms = CalculateRMS(currentPos);
 
        if (!isRecording)
        {
            if (rms > threshold)
            {
                Debug.Log("Voice detected, starting recording.");
                isRecording = true;
                detectionStartIndex = currentPos;
                silenceTimer = 0f;
            }
        }
        else
        {
            recordingTimer += Time.deltaTime;
            if(!checkTextAnimation)
            {
                recordingField.gameObject.SetActive(true);
                checkTextAnimation = true;
            }
            if (rms < threshold / 2) 
            {
                silenceTimer += Time.deltaTime;
                if (silenceTimer > silenceTimeout)
                {
                    Debug.Log("Silence detected, stopping recording.");
                    textAnimation.StopAnimation();
                    recordingField.gameObject.SetActive(false);
                    checkTextAnimation = false;
                    Debug.Log("Total Recording Time: " + recordingTimer + " seconds");
                    if (recordingTimer < 2f)
                    {
                        Debug.Log("Recording too short, discarding.");
                        isRecording = false;
                        silenceTimer = 0f;
                        recordingTimer = 0f;
                        return;
                    }
                    else
                    {
                        StopAndSend(currentPos);
                        silenceTimer = 0f;
                        recordingTimer = 0f;
                        isRecording = false;
                    }
                }
            }
            else
            {
                silenceTimer = 0f;
            }
        }

    }

    private float CalculateRMS(int currentPos)
    {
        int startReadPos = currentPos - sampleSize;
        if (startReadPos < 0) startReadPos += audioClip.samples;

        float[] tempSamples = new float[sampleSize];
        if (startReadPos + sampleSize < audioClip.samples)
        {
            audioClip.GetData(tempSamples, startReadPos);
        }
        else
        {
            int endCount = audioClip.samples - startReadPos;
            float[] part1 = new float[endCount];
            float[] part2 = new float[sampleSize - endCount];

            audioClip.GetData(part1, startReadPos);
            audioClip.GetData(part2, 0);

            Array.Copy(part1, 0, tempSamples, 0, endCount);
            Array.Copy(part2, 0, tempSamples, endCount, part2.Length);
        }

        float sum = 0f;
        for (int i = 0; i < tempSamples.Length; i++)
        {
            sum += tempSamples[i] * tempSamples[i];
        }
        return Mathf.Sqrt(sum / tempSamples.Length);
    }

    private void StopAndSend(int currentPos)
    {
        int preRollSamples = preRollSeconds * sampleRate;
        int finalStartIndex = detectionStartIndex - preRollSamples;
        if (finalStartIndex < 0) finalStartIndex += audioClip.samples;

        int totalLength = 0;
        if (currentPos >= finalStartIndex)
        {
            totalLength = currentPos - finalStartIndex;
        }
        else
        {
            totalLength = (audioClip.samples - finalStartIndex) + currentPos;
        }

        float[] fullData = new float[totalLength];

        if (currentPos >= finalStartIndex)
        {
            audioClip.GetData(fullData, finalStartIndex);
        }
        else
        {
            float[] part1 = new float[audioClip.samples - finalStartIndex];
            float[] part2 = new float[currentPos];

            audioClip.GetData(part1, finalStartIndex);
            audioClip.GetData(part2, 0);

            Array.Copy(part1, 0, fullData, 0, part1.Length);
            Array.Copy(part2, 0, fullData, part1.Length, part2.Length);
        }

        AudioClip clipToSend = AudioClip.Create("UserVoice", totalLength, 1, sampleRate, false);
        clipToSend.SetData(fullData, 0);

        if (wavToWhisper != null)
        {
            wavToWhisper.SendAudioToServer(clipToSend);
        }
        analyzingPitch.AnalyzeRecordedClip(clipToSend);
    }
}