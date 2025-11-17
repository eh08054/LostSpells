using UnityEngine;
using LostSpells.Data;

namespace LostSpells.Components
{
    /// <summary>
    /// 즉시 발동 스킬 (썬더볼트, 힐 등)
    /// - 타겟 위치에 즉시 생성
    /// - 이동하지 않음
    /// - 이펙트 재생 후 자동 소멸
    /// </summary>
    public class InstantSkillBehavior : SkillBehavior
    {
        [Header("Instant Skill Settings")]
        [SerializeField] private float effectDuration = 1f; // 이펙트 지속 시간
        [SerializeField] private bool findNearestEnemy = true; // 가장 가까운 적 찾기

        protected override void Start()
        {
            base.Start();

            // 타겟 위치로 이동
            if (findNearestEnemy)
            {
                FindAndMoveToNearestEnemy();
            }

            // 즉시 데미지 처리
            ApplyInstantEffect();

            // 이펙트 재생 후 자동 파괴
            Destroy(gameObject, effectDuration);
        }

        protected override void MoveSkill()
        {
            // 이동하지 않음
        }

        /// <summary>
        /// 가장 가까운 적 찾아서 이동
        /// </summary>
        private void FindAndMoveToNearestEnemy()
        {
            EnemyComponent[] enemies = FindObjectsByType<EnemyComponent>(FindObjectsSortMode.None);

            if (enemies.Length == 0)
            {
                Debug.LogWarning("[Instant] 타겟할 적이 없습니다!");
                return;
            }

            // 가장 가까운 적 찾기
            EnemyComponent nearestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            if (nearestEnemy != null)
            {
                transform.position = nearestEnemy.transform.position;
            }
        }

        /// <summary>
        /// 즉시 효과 적용
        /// </summary>
        private void ApplyInstantEffect()
        {
            if (skillData == null) return;

            // 범위 내 모든 적에게 데미지
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillData.range);

            foreach (var hit in hits)
            {
                if (((1 << hit.gameObject.layer) & targetLayer) == 0)
                    continue;

                var enemy = hit.GetComponent<EnemyComponent>();
                if (enemy != null)
                {
                    // TODO: Enemy에 데미지 적용 메서드 추가 필요
                    // enemy.TakeDamage(skillData.damage);
                }
            }
        }

        // 범위 시각화 (에디터에서만)
        private void OnDrawGizmosSelected()
        {
            if (skillData != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, skillData.range);
            }
        }
    }
}
