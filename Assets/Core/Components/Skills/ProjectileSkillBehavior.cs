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
        [SerializeField] private bool penetrating = false; // 관통형 여부

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
                // TODO: Enemy에 데미지 적용 메서드 추가 필요
                // enemy.TakeDamage(skillData.damage);

                if (!penetrating)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
