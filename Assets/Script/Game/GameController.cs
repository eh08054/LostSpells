using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    static GameController instance;
    public static GameController Instance => instance;

    [SerializeField] DialogueBehaviour dialogueBehaviour;


    void Start()
    {
        instance = this;
        initGame();
    }
    public void initGame()
    {
        StartGame();
    }

    public void StartGame()
    {
        dialogueBehaviour.DebugText();
    }

   
    void Update()
    {
        
    }
}
