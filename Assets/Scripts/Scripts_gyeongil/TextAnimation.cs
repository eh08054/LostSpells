using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class TextAnimation : MonoBehaviour
{
    private string[] text = { "Recording.", "Recording..", "Recording..." };
    private TMP_Text recordingText = null;
    private Coroutine animationCoroutine;
    private void Awake()
    {
        recordingText = GetComponent<TMP_Text>();
    }
    private void OnEnable()
    {
        animationCoroutine = StartCoroutine(AnimateText());
    }
    private IEnumerator AnimateText()
    {
        recordingText.text = text[0];
        int index = 0;
        while (true)
        {
            recordingText.text = text[index];
            index = (index + 1) % 3;
            yield return new WaitForSeconds(0.5f);
        }
    }
    public void StopAnimation()
    {
        if(animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }   
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
        animationCoroutine = null;
    }
}
