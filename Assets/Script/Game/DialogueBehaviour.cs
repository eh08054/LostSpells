
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBehaviour : MonoBehaviour
{
    const int DialogueOffset = 800;
    [SerializeField] SpriteDatabase spriteDatabase;
    [SerializeField] Image dialogue;
    [SerializeField] RectTransform dialogueRect;
    [SerializeField] Image playerImage;
    [SerializeField] Image enemyImage;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI dialogueText;
    

    List<string> dialogueQueue;
    private int currentIndex;


    public bool isOpen;

    public delegate void SimpleCallback();
    public void OpenDialogue(SimpleCallback simpleCallback = null)
    {
        if (isOpen) return;


        playerImage.transform.parent.gameObject.SetActive(false);
        enemyImage.transform.parent.gameObject.SetActive(false);
        titleText.text = "";
        dialogueText.text = "";

        isOpen = true;
        StartCoroutine(OpenDialogueCoroutine(simpleCallback));

    }

    IEnumerator OpenDialogueCoroutine(SimpleCallback simpleCallback)
    {
        dialogue.transform.position = new Vector3(dialogue.transform.position.x, -DialogueOffset, 0);
        dialogue.gameObject.SetActive(true);
        while (dialogue.transform.position.y < 0)
        {
            dialogue.transform.Translate(Vector3.up * Time.deltaTime * 1000);
            yield return null;
        }
        dialogue.transform.position = new Vector3(dialogue.transform.position.x, 0, 0);

        simpleCallback?.Invoke();
        yield return null;
    }
    public void CloseDialogue(SimpleCallback simpleCallback = null)
    {
        if (!isOpen) return;
        isOpen = false;
        StartCoroutine(CloseDialogueCoroutine(simpleCallback));
    }
    IEnumerator CloseDialogueCoroutine(SimpleCallback simpleCallback)
    {
        dialogue.transform.position = new Vector3(dialogue.transform.position.x, 0, 0);
        dialogue.gameObject.SetActive(true);
        while (dialogue.transform.position.y > -DialogueOffset)
        {
            dialogue.transform.Translate(Vector3.up * Time.deltaTime * -1000);
            yield return null;
        }
        dialogue.transform.position = new Vector3(dialogue.transform.position.x, - DialogueOffset, 0);
        simpleCallback?.Invoke();
        yield return null;
    }

    public void SetDialogueList(List<string> dialoguelist)
    {
        dialogueQueue = dialoguelist;
        currentIndex = -1;

        OnclickDialogue();
    }

    public void SetDialogue(string p, string e, string title, string text)
    {
        SetDialoguePlayerImage(spriteDatabase.GetSprite(p));
        SetDialogueEnemyImage(spriteDatabase.GetSprite(e));
        SetDialogueTitle(title);
        SetDialogueText(text);
    }


    public void SetDialogue(Sprite playerSprite, Sprite enemySprite, string title, string text)
    {
        SetDialoguePlayerImage(playerSprite);
        SetDialogueEnemyImage(enemySprite);
        SetDialogueTitle(title);
        SetDialogueText(text);
    }
    public void SetDialoguePlayerImage(Sprite sprite)
    {
        if (sprite == null)
        {
            playerImage.transform.parent.gameObject.SetActive(false);
            return;
        }
        else
        {
            playerImage.transform.parent.gameObject.SetActive(true);
            playerImage.sprite = sprite;
        }
    }
    public void SetDialogueEnemyImage(Sprite sprite)
    {
        if (sprite == null)
        {
            enemyImage.transform.parent.gameObject.SetActive(false);
            return;
        }
        else
        {
            enemyImage.transform.parent.gameObject.SetActive(true);
            enemyImage.sprite = sprite;
        }
    }
    public void SetDialogueTitle(string title)
    {
        titleText.text = title;
    }
    public void SetDialogueText(string text)
    {
        dialogueText.text = text;
    }

    public void OnclickDialogue()
    {
        
        currentIndex++;

        if(currentIndex < dialogueQueue.Count)
        {
            OpenDialogue();
            string[] args = dialogueQueue[currentIndex].Split(',');
            SetDialogue(
                args[0].Trim(),
                args[1].Trim(),
                args[2].Trim(),
                args[3].Trim()
                );
        }
        else
        {
            CloseDialogue(() =>
            {
                //resume game;
            });
        }
    }

    public void DebugText()
    {
        
        OpenDialogue(() => {
                SetDialogueList(new List<string> {
                "player1, ,Test1, text description",
                "player1, ,Test1, text description2",
                "player1, ,Test1, text description3",
                ",enemy1 ,enemy, text test",
                ",enemy2 ,enemy, text test",
                ", ,narrator, text test",
            });
        });
        
    }
}
