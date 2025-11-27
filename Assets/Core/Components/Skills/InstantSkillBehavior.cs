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
        [SerializeField] private bool hitOnlyTarget = true; // true: 타겟만 공격, false: 범위 내 모든 적 공격
        [SerializeField] private bool isDamageOverTime = false; // true: 지속 데미지, false: 즉시 데미지
        [SerializeField] private float damageInterval = 0.2f; // 지속 데미지 간격 (초)

        private EnemyComponent targetEnemy = null; // 타겟팅된 적
        private float lastDamageTime = 0f; // 마지막 데미지 시간

        protected override void Start()
        {
            base.Start();

            // 타겟 위치로 이동
            if (findNearestEnemy)
            {
                FindAndMoveToNearestEnemy();
            }

            // 즉시 데미지 모드일 때만 즉시 데미지 적용
            if (!isDamageOverTime)
            {
                ApplyInstantEffect();
            }
            else
            {
                // 지속 데미지 모드: 첫 데미지 즉시 적용
                lastDamageTime = Time.time;
                ApplyInstantEffect();
            }

            // 이펙트 재생 후 자동 파괴
            Destroy(gameObject, effectDuration);
        }

        protected override void Update()
        {
            base.Update();

            // 지속 데미지 모드일 때만 반복 데미지 처리
            if (isDamageOverTime && Time.time - lastDamageTime >= damageInterval)
            {
                lastDamageTime = Time.time;
                ApplyInstantEffect();
            }
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
                targetEnemy = nearestEnemy; // 타겟 저장
                transform.position = nearestEnemy.transform.position;
            }
        }

        /// <summary>
        /// 즉시 효과 적용
        /// </summary>
        private void ApplyInstantEffect()
        {
            if (skillData == null) return;

            // hitOnlyTarget이 true면 타겟만 공격
            if (hitOnlyTarget && targetEnemy != null)
            {
                int damageAmount = Mathf.RoundToInt(skillData.damage);
                targetEnemy.TakeDamage(damageAmount);
                return;
            }

            // hitOnlyTarget이 false면 범위 내 모든 적에게 데미지
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillData.range);

            foreach (var hit in hits)
            {
                if (((1 << hit.gameObject.layer) & targetLayer) == 0)
                    continue;

                var enemy = hit.GetComponent<EnemyComponent>();
                if (enemy != null)
                {
                    // 적에게 데미지 적용
                    int damageAmount = Mathf.RoundToInt(skillData.damage);
                    enemy.TakeDamage(damageAmount);
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
