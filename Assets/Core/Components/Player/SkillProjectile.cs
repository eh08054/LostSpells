using UnityEngine;
using System.Collections.Generic;

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
        private int pierceCount = 0; // 관통 횟수 (0 = 첫 적에서 사라짐, -1 = 무한 관통)
        private int currentPierceCount = 0; // 현재 관통한 횟수
        private HashSet<int> hitEnemies = new HashSet<int>(); // 이미 맞은 적 ID

        public void Initialize(int damage, float speed, float lifetime, Vector3 direction, GameObject vfxPrefab)
        {
            this.damage = damage;
            this.speed = speed;
            this.lifetime = lifetime;
            this.direction = direction.normalized;
            this.vfxPrefab = vfxPrefab;
            this.pierceCount = 0;

            // 수명 후 자동 파괴
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// 관통 속성 포함 초기화
        /// </summary>
        public void Initialize(int damage, float speed, float lifetime, Vector3 direction, GameObject vfxPrefab, int pierceCount)
        {
            Initialize(damage, speed, lifetime, direction, vfxPrefab);
            this.pierceCount = pierceCount;
        }

        private void Update()
        {
            // 투사체 이동
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 적과 충돌 시 (Layer 사용)
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                int enemyId = collision.gameObject.GetInstanceID();

                // 이미 맞은 적이면 무시
                if (hitEnemies.Contains(enemyId))
                    return;

                hitEnemies.Add(enemyId);

                EnemyComponent enemy = collision.GetComponent<EnemyComponent>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }

                // 충돌 위치에 이펙트 생성
                if (vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
                    Destroy(vfx, 2f);
                }

                // 관통 체크
                if (pierceCount == -1)
                {
                    // 무한 관통 - 파괴하지 않음
                    return;
                }
                else if (currentPierceCount < pierceCount)
                {
                    // 관통 횟수 남음
                    currentPierceCount++;
                    return;
                }

                // 투사체 파괴
                Destroy(gameObject);
            }
            // 지형과 충돌 시 (Ground 또는 Default 레이어)
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                     collision.gameObject.layer == LayerMask.NameToLayer("Default"))
            {
                // 플레이어나 다른 투사체는 무시
                if (collision.GetComponent<PlayerComponent>() != null ||
                    collision.GetComponent<SkillProjectile>() != null)
                {
                    return;
                }

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
