using UnityEngine;
using LostSpells.Data;

namespace LostSpells.Components
{
    /// <summary>
    /// 보호막 스킬 (매직실드 등)
    /// - 플레이어 위치에 생성
    /// - 플레이어를 따라다님
    /// - 일정 시간 지속 후 자동 소멸
    /// </summary>
    public class ShieldSkillBehavior : SkillBehavior
    {
        [Header("Shield Settings")]
        [SerializeField] private float shieldDuration = 5f; // 보호막 지속 시간
        [SerializeField] private Vector3 offset = Vector3.zero; // 플레이어로부터 오프셋

        private float activationTime;

        protected override void Start()
        {
            base.Start();
            activationTime = Time.time;

            Debug.Log($"[Shield] {gameObject.name} 활성화! (지속시간: {shieldDuration}초)");
        }

        protected override void MoveSkill()
        {
            // 이동하지 않고, 플레이어를 따라다님
            if (caster != null)
            {
                transform.position = caster.transform.position + offset;
                transform.rotation = caster.transform.rotation;
            }
        }

        protected override void Update()
        {
            base.Update();

            // 지속 시간 체크
            if (Time.time - activationTime > shieldDuration)
            {
                Debug.Log($"[Shield] {gameObject.name} 지속시간 종료");
                Destroy(gameObject);
            }
        }

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            // 보호막은 적의 공격을 막음
            if (((1 << collision.gameObject.layer) & targetLayer) == 0)
                return;

            Debug.Log($"[Shield] 적의 공격 차단!");

            // TODO: 적의 발사체를 파괴하거나 데미지 무효화
            // var enemyProjectile = collision.GetComponent<EnemyProjectile>();
            // if (enemyProjectile != null)
            // {
            //     Destroy(enemyProjectile.gameObject);
            // }
        }
    }
}
