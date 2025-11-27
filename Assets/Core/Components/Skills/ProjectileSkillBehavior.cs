using UnityEngine;
using LostSpells.Data;

namespace LostSpells.Components
{
    /// <summary>
    /// 발사체 스킬 (파이어볼, 아이스 애로우 등)
    /// - 플레이어 앞에서 발사
    /// - 직선으로 날아감
    /// - 적과 충돌 시 데미지 + 파괴
    /// </summary>
    public class ProjectileSkillBehavior : SkillBehavior
    {
        [Header("Projectile Settings")]
        [SerializeField] private int maxPenetrationCount = 0; // 최대 관통 횟수 (-1: 무제한, 0: 관통 안함, 1+: 지정 횟수)

        private int currentPenetrationCount = 0; // 현재 관통한 적의 수

        protected override void Start()
        {
            base.Start();
        }

        protected override void MoveSkill()
        {
            // 앞으로 직선 이동
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            // 타겟 레이어 체크
            if (((1 << collision.gameObject.layer) & targetLayer) == 0)
                return;

            var enemy = collision.GetComponent<EnemyComponent>();
            if (enemy != null && skillData != null)
            {
                // 적에게 데미지 적용
                int damageAmount = Mathf.RoundToInt(skillData.damage);
                enemy.TakeDamage(damageAmount);

                // 관통 카운트 증가
                currentPenetrationCount++;

                // 관통 여부 체크
                // -1: 무제한 관통 (파괴 안함)
                // 0: 관통 안함 (첫 충돌 시 파괴)
                // 1+: 지정 횟수만큼 관통 후 파괴
                if (maxPenetrationCount >= 0 && currentPenetrationCount > maxPenetrationCount)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
