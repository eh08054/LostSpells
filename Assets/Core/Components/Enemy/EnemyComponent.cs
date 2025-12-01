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
        [SerializeField] private int attackDamage = 10; // 공격 데미지
        [SerializeField] private float attackCooldown = 1f; // 공격 쿨다운 (초)

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color enemyColor;
        [SerializeField] private Sprite enemySprite; // 적 스프라이트 (챕터별로 다름)
        [SerializeField] private Animator animator; // 애니메이터

        [Header("UI Elements")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Transform healthBarBackground;
        [SerializeField] private Transform healthBarFill;

        private Transform player;
        private Rigidbody2D rb;
        private float lastAttackTime = 0f; // 마지막 공격 시간

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

            // Rigidbody2D 설정
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 1; // 중력 적용
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 방지

            // BoxCollider2D 설정 (땅과 충돌하기 위함)
            // 적들끼리는 물리적 충돌 없이 레이캐스트로만 거리 유지
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.8f, 0.8f);
                collider.offset = new Vector2(0, 0);
            }

            // 다른 적들과는 충돌하지 않도록 설정
            // Physics2D.IgnoreLayerCollision을 사용하거나,
            // 각 적의 Collider를 excludeLayers에 추가
            gameObject.layer = LayerMask.NameToLayer("Enemy");

            currentHealth = maxHealth;
        }

        private void Start()
        {
            // 적 스프라이트 설정
            if (spriteRenderer != null && enemySprite != null)
            {
                spriteRenderer.sprite = enemySprite;
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

            // 모든 다른 적들과의 충돌 무시 (땅과만 충돌)
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null)
            {
                EnemyComponent[] allEnemies = FindObjectsByType<EnemyComponent>(FindObjectsSortMode.None);
                foreach (EnemyComponent otherEnemy in allEnemies)
                {
                    if (otherEnemy != this)
                    {
                        Collider2D otherCollider = otherEnemy.GetComponent<Collider2D>();
                        if (otherCollider != null)
                        {
                            Physics2D.IgnoreCollision(myCollider, otherCollider);
                        }
                    }
                }
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
                    float stopDistance = 0.6f; // 이 거리 이내에 적이 있으면 멈춤
                    float resumeDistance = 1.2f; // 이 거리 이상 멀어져야 다시 이동

                    // 나와 플레이어 사이에 있는 적들만 확인
                    bool canMove = true;
                    float closestEnemyDistance = float.MaxValue;

                    EnemyComponent[] allEnemies = FindObjectsByType<EnemyComponent>(FindObjectsSortMode.None);
                    foreach (EnemyComponent otherEnemy in allEnemies)
                    {
                        if (otherEnemy == this) continue;

                        float myX = transform.position.x;
                        float otherX = otherEnemy.transform.position.x;
                        float playerX = player.position.x;

                        // 나와 플레이어 사이에 있는 적인지 확인
                        bool isBetween = false;
                        if (playerX > myX) // 플레이어가 오른쪽에 있음
                        {
                            isBetween = (otherX > myX && otherX < playerX);
                        }
                        else // 플레이어가 왼쪽에 있음
                        {
                            isBetween = (otherX < myX && otherX > playerX);
                        }

                        if (isBetween)
                        {
                            float distToEnemy = Mathf.Abs(otherX - myX);
                            if (distToEnemy < closestEnemyDistance)
                            {
                                closestEnemyDistance = distToEnemy;
                            }
                        }
                    }

                    // 현재 움직이고 있는지 확인
                    bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;

                    // 움직이고 있으면 stopDistance로, 멈춰있으면 resumeDistance로 판단
                    float thresholdDistance = isMoving ? stopDistance : resumeDistance;

                    // 가장 가까운 적까지의 거리가 임계값보다 작으면 멈춤
                    if (closestEnemyDistance < thresholdDistance)
                    {
                        canMove = false;
                    }

                    if (canMove)
                    {
                        velocity.x = directionToPlayer.x * moveSpeed;
                    }
                }
                // stoppingDistance 이내면 멈춤 (velocity.x = 0으로 유지)

                // stoppingDistance 이내면 플레이어 공격
                if (distanceToPlayer <= stoppingDistance)
                {
                    TryAttackPlayer();
                }
            }
            else
            {
                // 플레이어가 없으면 왼쪽으로 이동
                velocity.x = -moveSpeed;
            }

            // Rigidbody2D로 이동 (Y축 속도는 유지하여 중력 영향 받도록)
            rb.linearVelocity = velocity;

            // 스프라이트를 항상 플레이어 방향으로 뒤집기
            if (spriteRenderer != null && player != null)
            {
                float dirX = player.position.x - transform.position.x;
                // 플레이어가 왼쪽에 있으면 true (스프라이트 뒤집기)
                spriteRenderer.flipX = dirX < 0;
            }
            else if (spriteRenderer != null && velocity.x != 0)
            {
                // 플레이어가 없을 때는 이동 방향에 따라
                spriteRenderer.flipX = velocity.x < 0;
            }

            // 애니메이터 Speed 파라미터 업데이트
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                float speed = Mathf.Abs(velocity.x);
                animator.SetFloat("Speed", speed);
            }
        }

        /// <summary>
        /// 적 초기화
        /// </summary>
        public void Initialize(string name, int health, float speed, Sprite sprite = null)
        {
            enemyName = name;
            maxHealth = health;
            currentHealth = health;
            moveSpeed = speed;

            // 스프라이트 설정
            if (sprite != null)
            {
                enemySprite = sprite;
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                }
            }

            if (nameText != null)
            {
                nameText.text = name;
            }

            UpdateHealthBar();
        }

        /// <summary>
        /// 플레이어 공격 시도
        /// </summary>
        private void TryAttackPlayer()
        {
            if (Time.time - lastAttackTime >= attackCooldown && player != null)
            {
                PlayerComponent playerComponent = player.GetComponent<PlayerComponent>();
                if (playerComponent != null)
                {
                    // 넉백 방향 계산 (플레이어 - 적)
                    Vector2 knockbackDirection = (player.position - transform.position).normalized;
                    // Y축 성분을 추가해서 위로 띄움
                    knockbackDirection = new Vector2(knockbackDirection.x, 1f).normalized;

                    playerComponent.TakeDamage(attackDamage, knockbackDirection);
                    lastAttackTime = Time.time;
                }
            }
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
            // Death 애니메이션 재생
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("IsDead", true);
            }

            // 이동 멈추기
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0;
            }

            // 충돌 비활성화
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            // 체력바와 이름 숨기기
            if (healthBarBackground != null)
                healthBarBackground.gameObject.SetActive(false);
            if (nameText != null)
                nameText.gameObject.SetActive(false);

            // Death 애니메이션 길이 후에 제거 (0.5초)
            Destroy(gameObject, 0.5f);

            // TODO: 경험치/골드 드랍
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
