using UnityEngine;
using TMPro;

namespace LostSpells.Components
{
    /// <summary>
    /// 적 캐릭터 컴포넌트
    /// - 사각형 오브젝트
    /// - 이름 표시
    /// - 체력바 표시
    /// - 오른쪽에서 왼쪽으로 이동
    /// </summary>
    public class EnemyComponent : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] private string enemyName = "Enemy";
        [SerializeField] private int maxHealth = 50;
        [SerializeField] private int currentHealth;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stoppingDistance = 1.5f; // 플레이어와 유지할 거리

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color enemyColor = Color.red;

        [Header("UI Elements")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Transform healthBarBackground;
        [SerializeField] private Transform healthBarFill;

        private Transform player;
        private Rigidbody2D rb;

        private void Awake()
        {
            // SpriteRenderer 설정
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            // Rigidbody2D 설정 (물리 충돌용)
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 1; // 중력 적용
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 방지

            // Collider 설정 (충돌 감지용)
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.4f; // 적 크기에 맞게 조정
            }

            // Layer 설정 (적들끼리는 충돌하지 않도록)
            gameObject.layer = LayerMask.NameToLayer("Enemy");
            if (gameObject.layer == 0) // Layer가 없으면 생성 필요
            {
                Debug.LogWarning("Enemy Layer가 없습니다. Edit > Project Settings > Tags and Layers에서 'Enemy' Layer를 추가하세요.");
            }

            currentHealth = maxHealth;
        }

        private void Start()
        {
            // 적 색상 설정
            if (spriteRenderer != null)
            {
                spriteRenderer.color = enemyColor;
            }

            // 플레이어 찾기
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning($"{enemyName}: 플레이어를 찾을 수 없습니다! Player Tag를 확인하세요.");
            }

            // 이름 설정
            if (nameText != null)
            {
                nameText.text = enemyName;
            }

            // 체력바 초기화
            UpdateHealthBar();
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            Vector2 velocity = rb.linearVelocity;
            velocity.x = 0; // 기본적으로 좌우 이동 없음

            if (player != null)
            {
                // 플레이어와의 거리 계산
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                Vector2 directionToPlayer = (player.position - transform.position).normalized;

                // stoppingDistance보다 멀면 플레이어 쪽으로 이동
                if (distanceToPlayer > stoppingDistance)
                {
                    velocity.x = directionToPlayer.x * moveSpeed;
                }
                // stoppingDistance 이내면 멈춤 (velocity.x = 0으로 유지)
            }
            else
            {
                // 플레이어가 없으면 왼쪽으로 이동
                velocity.x = -moveSpeed;
            }

            // Rigidbody2D로 이동 (Y축 속도는 유지하여 중력 영향 받도록)
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// 적 초기화
        /// </summary>
        public void Initialize(string name, int health, float speed)
        {
            enemyName = name;
            maxHealth = health;
            currentHealth = health;
            moveSpeed = speed;

            if (nameText != null)
            {
                nameText.text = name;
            }

            UpdateHealthBar();
        }

        /// <summary>
        /// 데미지 받기
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            UpdateHealthBar();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 체력바 업데이트
        /// </summary>
        private void UpdateHealthBar()
        {
            if (healthBarFill != null)
            {
                float healthPercent = (float)currentHealth / maxHealth;

                // Scale 조정 (왼쪽에서 오른쪽으로 차도록)
                Vector3 scale = healthBarFill.localScale;
                scale.x = healthPercent;
                healthBarFill.localScale = scale;

                // Position 조정 (왼쪽 기준으로)
                Vector3 pos = healthBarFill.localPosition;
                pos.x = (healthPercent - 1f) / 2f;
                healthBarFill.localPosition = pos;
            }
        }

        /// <summary>
        /// 적 사망
        /// </summary>
        private void Die()
        {
            // TODO: 경험치/골드 드랍
            Destroy(gameObject);
        }

        /// <summary>
        /// Inspector에서 값이 변경될 때 호출
        /// </summary>
        private void OnValidate()
        {
            // 에디터에서만 실행 (플레이 모드나 빌드에서는 실행 안 함)
            #if UNITY_EDITOR
            // 이름 업데이트
            if (nameText != null)
            {
                nameText.text = enemyName;
            }

            // 체력 범위 제한
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // 체력바 업데이트
            UpdateHealthBar();
            #endif
        }

        public string GetEnemyName() => enemyName;
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
    }
}
