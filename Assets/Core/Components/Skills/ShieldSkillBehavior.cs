using UnityEngine;
using LostSpells.Data;

namespace LostSpells.Components
{
    /// <summary>
    /// 보호막 스킬 (매직실드 등)
    /// - 플레이어 위치에 생성
    /// - 플레이어를 따라다님
    /// - 일정 시간 지속 후 자동 소멸
    /// - 활성화 중에는 플레이어가 데미지를 받지 않음
    /// </summary>
    public class ShieldSkillBehavior : SkillBehavior
    {
        [Header("Shield Settings")]
        [SerializeField] private float shieldDuration = 5f; // 보호막 지속 시간
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.25f, 0f); // 플레이어로부터 오프셋 (위로 0.25)

        private float activationTime;

        protected override void Start()
        {
            base.Start();
            activationTime = Time.unscaledTime;

            // 플레이어에게 실드 활성화 알림
            if (caster != null)
            {
                caster.ActivateShield();
            }
        }

        /// <summary>
        /// 스킬 초기화 (PlayerComponent에서 호출)
        /// </summary>
        public override void Initialize(LostSpells.Data.SkillData data, PlayerComponent player)
        {
            base.Initialize(data, player);

            // 스킬 데이터에서 지속 시간 가져오기
            if (data != null && data.projectileLifetime > 0)
            {
                shieldDuration = data.projectileLifetime;
            }
        }

        protected override void MoveSkill()
        {
            // 이동하지 않고, 플레이어를 따라다님
            if (caster != null)
            {
                transform.position = caster.transform.position + offset;
                // 회전은 적용하지 않음 (실드는 항상 같은 방향 유지)
            }
        }

        protected override void Update()
        {
            // 플레이어 따라다니기
            MoveSkill();

            // 지속 시간 체크 (unscaled time 사용하여 TimeScale=0에서도 작동)
            if (Time.unscaledTime - activationTime > shieldDuration)
            {
                DestroyShield();
            }
        }

        /// <summary>
        /// 실드 제거 (지속 시간 종료 또는 외부 요인)
        /// </summary>
        private void DestroyShield()
        {
            // 플레이어 실드 비활성화
            if (caster != null)
            {
                caster.DeactivateShield();
            }
            Destroy(gameObject);
        }

        /// <summary>
        /// 오브젝트 파괴 시 호출
        /// </summary>
        private void OnDestroy()
        {
            // 혹시 DestroyShield가 아닌 다른 방법으로 파괴되는 경우 대비
            if (caster != null && caster.HasActiveShield())
            {
                caster.DeactivateShield();
            }
        }

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            // 실드는 적에게 데미지를 주지 않음 (방어 전용)
            // 적의 투사체가 있다면 파괴할 수 있음 (TODO: 구현)
        }

        /// <summary>
        /// 화면 밖으로 나가도 제거하지 않음 (플레이어를 따라다니므로)
        /// </summary>
        protected override void OnBecameInvisible()
        {
            // 아무것도 하지 않음 (부모 클래스의 Destroy 방지)
        }
    }
}
