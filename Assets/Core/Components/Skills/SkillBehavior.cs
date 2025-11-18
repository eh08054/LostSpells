using UnityEngine;
using LostSpells.Data;

namespace LostSpells.Components
{
    /// <summary>
    /// 스킬 프리팹의 기본 동작 제어
    /// - 각 스킬 프리팹에 이 스크립트를 붙이거나 상속받은 스크립트 사용
    /// </summary>
    public class SkillBehavior : MonoBehaviour
    {
        [Header("Skill Settings")]
        [SerializeField] protected float moveSpeed = 10f; // 발사체 이동 속도
        [SerializeField] protected float lifetime = 5f; // 스킬 지속 시간 (초)
        [SerializeField] protected LayerMask targetLayer; // 타격 대상 레이어 (예: Enemy)

        protected SkillData skillData;
        protected PlayerComponent caster; // 스킬 사용자
        protected SpriteRenderer spriteRenderer; // 스프라이트 렌더러 (방향 표시용)

        private float spawnTime;

        protected virtual void Start()
        {
            spawnTime = Time.time;

            // SpriteRenderer 가져오기
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 스킬 초기화 (PlayerComponent에서 호출)
        /// </summary>
        public virtual void Initialize(SkillData data, PlayerComponent player)
        {
            skillData = data;
            caster = player;

            // 플레이어의 방향에 따라 스킬 스프라이트 뒤집기
            if (spriteRenderer != null && player != null)
            {
                var playerSprite = player.GetComponent<SpriteRenderer>();
                if (playerSprite != null)
                {
                    spriteRenderer.flipX = playerSprite.flipX;
                }
            }
        }

        protected virtual void Update()
        {
            // 발사체 이동
            MoveSkill();

            // 수명 체크
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 스킬 이동 (발사체 타입인 경우)
        /// </summary>
        protected virtual void MoveSkill()
        {
            // 앞으로 이동
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 충돌 처리 (Trigger 사용 권장)
        /// </summary>
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            // 타겟 레이어 체크
            if (((1 << collision.gameObject.layer) & targetLayer) == 0)
                return;

            // 적에게 데미지 처리
            var enemy = collision.GetComponent<EnemyComponent>();
            if (enemy != null && skillData != null)
            {
                int damageAmount = Mathf.RoundToInt(skillData.damage);
                enemy.TakeDamage(damageAmount);

                // 스킬 제거 (관통형이 아니라면)
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 화면 밖으로 나가면 제거 (선택사항)
        /// </summary>
        protected virtual void OnBecameInvisible()
        {
            // 카메라 밖으로 나가면 제거
            Destroy(gameObject);
        }
    }
}
