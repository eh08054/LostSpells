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

    [Header("Audio Settings")]
    public int sampleRate = 44100; // 마이크 샘플레이트 - 프로젝트 설정과 동일하게
    public int frameSize = 2048;   // 분석 윈도우 (권장 1024-4096)
    public int hopSize = 1024;     // 프레임 슬라이드 (보통 frameSize/2)

    [Header("Onset / Energy")]
    public float rmsThreshold = 0.01f; // 음절 시작 기준 (조정 필요)
    public float rmsSilenceThreshold = 0.008f; // 음절 종료 기준
    public int minFramesPerSegment = 3; // 최소 프레임 길이(노이즈 필터)

    [Header("Pitch Matching")]
    public string noteNames = "C4,G4,E4,G4"; // 또는 빈값으로 두고 targetNotesHz 사용
    public float[] targetNotesHz; // 직접 Hz 배열로 넣어도 됨
    public float toleranceCents = 100f; // 허용 오차(센트) — 초기값 관대(100cent = 1 semitone)

    [Header("Debug / UI hooks")]
    public bool debugLog = true;

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

