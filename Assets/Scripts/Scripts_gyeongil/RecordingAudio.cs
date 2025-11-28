using UnityEngine;
using UnityEngine.UI;

public class RecordingAudio : MonoBehaviour
{
    [SerializeField] private Button recordButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button skillSendButton;
    [SerializeField] private Image progressBar;
    [SerializeField] private WavToWhisper wavToWhisper;
    [SerializeField] private RegisterSkill registerSkill;
    private AudioClip recordedClip;
    private bool isRecording;
    private float time;
    private readonly int recordDuration = 3;
    void Start()
    {
        recordButton.onClick.AddListener(StartRecording);
        stopButton.onClick.AddListener(EndRecording);
        skillSendButton.onClick.AddListener(registerSkill.sendSkills);
    }
    private void StartRecording()
    {
        isRecording = true;
        stopButton.gameObject.SetActive(true);
        recordButton.gameObject.SetActive(false);
        recordedClip = Microphone.Start(null, false, recordDuration, 44100);
    }
    private void EndRecording()
    {
        Microphone.End(null);
        isRecording = false;
        progressBar.fillAmount = 0;
        wavToWhisper.SendAudioToServer(recordedClip);
        recordButton.gameObject.SetActive(true);
        stopButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isRecording)
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

