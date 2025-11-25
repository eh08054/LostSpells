using UnityEngine;

namespace LostSpells.Components
{
    /// <summary>
    /// 스킬 투사체 - 적과 충돌 시 데미지 적용 및 이펙트 재생
    /// </summary>
    public class SkillProjectile : MonoBehaviour
    {
        private int damage;
        private float lifetime;
        private float speed;
        private Vector3 direction;
        private GameObject vfxPrefab;

        public void Initialize(int damage, float speed, float lifetime, Vector3 direction, GameObject vfxPrefab)
        {
            this.damage = damage;
            this.speed = speed;
            this.lifetime = lifetime;
            this.direction = direction.normalized;
            this.vfxPrefab = vfxPrefab;

            // 수명 후 자동 파괴
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            // 투사체 이동
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 적과 충돌 시
            if (collision.CompareTag("Enemy"))
            {
                EnemyComponent enemy = collision.GetComponent<EnemyComponent>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }

                // 충돌 위치에 이펙트 생성
                if (vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
                    Destroy(vfx, 2f); // 2초 후 이펙트 제거
                }

                // 투사체 파괴
                Destroy(gameObject);
            }
            // 지형과 충돌 시
            else if (collision.CompareTag("Ground"))
            {
                // 충돌 위치에 이펙트 생성
                if (vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
                    Destroy(vfx, 2f);
                }

                // 투사체 파괴
                Destroy(gameObject);
            }
        }
    }
}
