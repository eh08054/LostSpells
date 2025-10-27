using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class EnemyBehaviour : MonoBehaviour
{
    [SerializeField] Animator characterAnimatior;
    [SerializeField] HPIndicatorBehaviour HPIndicator;
    [SerializeField] TextMeshProUGUI floatingText;

    public float speed;
    public Transform target;
    [SerializeField] float damageOffset;
    public void Initialise()
    {
        //target = target;
        HPIndicator.SetHPMAX(100);
        HPIndicator.SetHP(100);
        speed = 1.0f;
    }

    public void TakeDamage(int damage, SkillType type, SkillThemeType theme)
    {
        if(damage < 0) damage = 0;
        HPIndicator.currentHP -= damage;
        StartCoroutine(FloatingText(damage));

    }

    IEnumerator FloatingText(int damage)
    {
        floatingText.text = damage.ToString();
        floatingText.transform.localScale = Vector3.one * (1.0f + 0.2f * (damage - 5));
        Color c;
        c = floatingText.color;
        c.a = 1f;
        floatingText.transform.localPosition  = new Vector3(0, damageOffset, 0);
        while(c.a > 0)
        {
            floatingText.color = c;
            c.a = Mathf.Clamp01(c.a-Time.deltaTime * 0.5f);
            floatingText.transform.Translate(Vector3.up * Time.deltaTime * 100f);
            yield return new WaitForEndOfFrame();
        }
        
    }

    void Update()
    {
        //transform.Translate(Vector3.left * speed  * Time.deltaTime);
        //transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }

}
