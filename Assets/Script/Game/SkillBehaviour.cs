using UnityEngine;

public class SkillBehaviour : MonoBehaviour
{
    public float speed;
    public int damage;
    public SkillType type;
    public SkillThemeType theme;

    public void Initialise(float speed, int damage, SkillType type, SkillThemeType theme)
    {
        this.speed = speed;
        this.damage = damage;
        this.type = type;
        this.theme = theme;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
        if(transform.position.x > 2000 ) Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == 6) // enemy layer
        {
            collision.transform.GetComponent<EnemyBehaviour>().TakeDamage(damage, type, theme);
            Destroy(gameObject);
        }
    }
}
