using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Spacebarwhisper : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private WavToWhisper wavToWhisper;
    [SerializeField] private RegisterSkill registerSkill;
    [SerializeField] private TextAnimation textAnimation;
    [SerializeField] private Button skillSendButton;
    [SerializeField] private Image recordingField;
    [SerializeField] private Image myTimebar;
    private AudioClip recordedClip;
    private bool isRecording;
    private float time;
    private readonly int recordDuration = 3;

    private void Start()
    {
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
        recordedClip = Microphone.Start(null, false, recordDuration, 44100);
    }
    private void EndRecording()
    {
        Debug.Log("Ended Recording");
        Microphone.End(null);
        textAnimation.StopAnimation();
        recordingField.gameObject.SetActive(false);
        myTimebar.gameObject.SetActive(false);
        isRecording = false;
        time = 0;
        progressBar.fillAmount = 0;
        wavToWhisper.SendAudioToServer(recordedClip);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            StartRecording();
        }
        if(Input.GetKeyUp(KeyCode.Space))
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
