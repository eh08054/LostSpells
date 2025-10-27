using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HPIndicatorBehaviour : MonoBehaviour
{
    [SerializeField] Slider indicatorSlider;

    public int currentHP;
    public int HPMax;

    private Coroutine coroutine;

    public void SetHPMAX(int HPMax)
    {
        this.HPMax = HPMax;
    }
    public void SetHP(int currentHP)
    {
        this.currentHP = currentHP;
    }
    void Update()
    {
        indicatorSlider.value = Mathf.Lerp(indicatorSlider.value,(float)currentHP / HPMax , Time.deltaTime * 0.1f);
    }


}
