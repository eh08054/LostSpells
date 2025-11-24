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
        [SerializeField] private Sprite enemySprite; // 적 스프라이트 (챕터별로 다름)
        [SerializeField] private Animator animator; // 애니메이터

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
            gameObject.layer = LayerMask.NameToLayer("Default");

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
                    // 앞에 있는 모든 적들을 확인 (RaycastAll)
                    float checkDistance = 3.0f; // 앞쪽 체크 거리 (충분히 길게)
                    float stopDistance = 0.6f; // 이 거리 이내에 적이 있으면 멈춤
                    float resumeDistance = 1.2f; // 이 거리 이상 멀어져야 다시 이동

                    RaycastHit2D[] hits = Physics2D.RaycastAll(
                        transform.position,
                        new Vector2(directionToPlayer.x, 0),
                        checkDistance
                    );

                    // 앞에 다른 적이 있는지 확인
                    bool canMove = true;
                    float closestEnemyDistance = float.MaxValue;

                    foreach (RaycastHit2D hit in hits)
                    {
                        if (hit.collider != null)
                        {
                            // 맞은 대상이 다른 적인지 확인
                            EnemyComponent otherEnemy = hit.collider.GetComponent<EnemyComponent>();
                            if (otherEnemy != null && otherEnemy != this)
                            {
                                float distToEnemy = hit.distance;
                                if (distToEnemy < closestEnemyDistance)
                                {
                                    closestEnemyDistance = distToEnemy;
                                }
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
            }
            else
            {
                // 플레이어가 없으면 왼쪽으로 이동
                velocity.x = -moveSpeed;
            }

            // Rigidbody2D로 이동 (Y축 속도는 유지하여 중력 영향 받도록)
            rb.linearVelocity = velocity;

            // 이동 방향에 따라 스프라이트 뒤집기
            if (spriteRenderer != null && velocity.x != 0)
            {
                // 왼쪽으로 이동 시 스프라이트 뒤집기
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
