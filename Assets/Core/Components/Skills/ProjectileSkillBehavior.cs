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
            Debug.Log($"[Projectile] {gameObject.name} 발사!");
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

            // 적에게 데미지 처리
            var enemy = collision.GetComponent<EnemyComponent>();
            if (enemy != null && skillData != null)
            {
                Debug.Log($"[Projectile] {skillData.skillName}이(가) {enemy.name}에게 {skillData.damage} 데미지!");

                // TODO: Enemy에 데미지 적용 메서드 추가 필요
                // enemy.TakeDamage(skillData.damage);

                // 관통형이 아니면 파괴
                if (!penetrating)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
