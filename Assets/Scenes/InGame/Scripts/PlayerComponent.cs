using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace LostSpells.Components
{
    /// <summary>
    /// 플레이어 캐릭터 컴포넌트
    /// - A, D 키로 좌우 이동
    /// - 이름 표시
    /// - 체력바 표시
    /// </summary>
    public class PlayerComponent : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private string playerName = "Wizard"; // 한글 폰트 경고 방지를 위해 영어로 설정
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;
        [SerializeField] private int maxMana = 80;
        [SerializeField] private int currentMana;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 5f; // 점프 힘
        [SerializeField] private float knockbackForce = 3f; // 넉백 힘

        [Header("Skill System")]
        [SerializeField] private Transform skillCastPoint; // 스킬 발사 위치 (없으면 플레이어 중심 사용)

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color playerColor = Color.blue;

        [Header("UI Elements")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Transform healthBarBackground;
        [SerializeField] private Transform healthBarFill;

        private Rigidbody2D rb;
        private Collider2D playerCollider;
        private bool isKnockedBack = false; // 넉백 중인지 여부
        private bool isGrounded = false; // 땅에 닿아있는지 여부

        private void Awake()
        {
            // SpriteRenderer가 없으면 추가
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
            playerCollider = GetComponent<CircleCollider2D>();
            if (playerCollider == null)
            {
                CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.radius = 0.4f; // 플레이어 크기에 맞게 조정
                playerCollider = circleCollider;
            }

            // 체력 초기화
            currentHealth = maxHealth;

            // 마나 초기화
            currentMana = maxMana;
        }

        private void Start()
        {
            // 플레이어 색상 설정
            if (spriteRenderer != null)
            {
                spriteRenderer.color = playerColor;
            }

            // 이름 설정
            if (nameText != null)
            {
                nameText.text = playerName;
            }

            // 체력바 초기화
            UpdateHealthBar();
        }

        private void Update()
        {
            if (rb == null) return;

            // 넉백 중에는 이동 불가
            if (isKnockedBack) return;

            // 키바인딩에서 이동 키 가져오기
            Key moveLeftKey = GetMoveLeftKey();
            Key moveRightKey = GetMoveRightKey();
            Key jumpKey = GetJumpKey();

            // 좌우 이동 (새 Input System 사용)
            float horizontal = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current[moveLeftKey].isPressed)
                {
                    horizontal = -1f;
                }
                else if (Keyboard.current[moveRightKey].isPressed)
                {
                    horizontal = 1f;
                }

                // 점프 (땅에 있을 때만)
                if (Keyboard.current[jumpKey].wasPressedThisFrame && isGrounded)
                {
                    Vector2 velocity = rb.linearVelocity;
                    velocity.y = jumpForce;
                    rb.linearVelocity = velocity;
                }
            }

            // Rigidbody2D로 좌우 이동 (Y축 속도는 유지하여 중력 영향 받도록)
            Vector2 velocity2 = rb.linearVelocity;
            velocity2.x = horizontal * moveSpeed;
            rb.linearVelocity = velocity2;
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
        /// 플레이어 사망
        /// </summary>
        private void Die()
        {
            Debug.Log("플레이어 사망!");
            // TODO: 게임 오버 처리
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(int amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            UpdateHealthBar();
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
        /// Inspector에서 값이 변경될 때 호출
        /// </summary>
        private void OnValidate()
        {
            #if UNITY_EDITOR
            // 이름 업데이트
            if (nameText != null)
            {
                nameText.text = playerName;
            }

            // 체력 범위 제한
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // 체력바 업데이트
            UpdateHealthBar();
            #endif
        }

        /// <summary>
        /// 적과 충돌 시 넉백 및 데미지
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 적과 충돌했는지 확인
            EnemyComponent enemy = collision.gameObject.GetComponent<EnemyComponent>();
            if (enemy != null && rb != null)
            {
                    // 넉백 중에는 무적 판정 (추가 데미지/넉백 없음)
                if (isKnockedBack)
                {
                    return;
                }

                // 적으로부터 떨어지는 좌우 방향의 부호만 계산
                float dirX = Mathf.Sign(transform.position.x - collision.transform.position.x);

                // 넉백 적용 (X축은 방향에 따라, Y축은 항상 위쪽으로)
                rb.linearVelocity = new Vector2(dirX * knockbackForce, knockbackForce);

                // 넉백 상태로 변경 & 플레이어 콜라이더가 모든 적과 충돌 무시 (땅에 닿을 때까지 무적)
                isKnockedBack = true;
                IgnoreAllEnemyCollisions(true);

                // 데미지 받기
                TakeDamage(10); // 적과 충돌 시 10 데미지
            }
        }

        /// <summary>
        /// 바닥에 닿으면 넉백 상태 해제 및 착지 체크
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            // 바닥 충돌 체크 (플레이어 아래쪽에 충돌이 있는지)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 충돌 지점이 플레이어보다 아래에 있거나, 법선이 위를 향하면 땅에 있는 것
                // contact.normal.y > 0.5f는 충돌면이 위쪽을 향한다는 의미 (땅 위에 서있음)
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;

                    // 넉백 중이고 Y 속도가 거의 0이면 착지한 것
                    if (isKnockedBack && Mathf.Abs(rb.linearVelocity.y) < 0.5f)
                    {
                        isKnockedBack = false;
                        // 플레이어 콜라이더가 모든 적과 충돌 다시 활성화
                        IgnoreAllEnemyCollisions(false);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 바닥에서 떨어지면 착지 해제
        /// </summary>
        private void OnCollisionExit2D(Collision2D collision)
        {
            isGrounded = false;
        }

        /// <summary>
        /// 플레이어 콜라이더가 모든 적 콜라이더와의 충돌을 무시/복구
        /// </summary>
        private void IgnoreAllEnemyCollisions(bool ignore)
        {
            if (playerCollider == null) return;

            // 씬에 있는 모든 적 찾기
            EnemyComponent[] enemies = FindObjectsByType<EnemyComponent>(FindObjectsSortMode.None);

            foreach (var enemy in enemies)
            {
                Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                if (enemyCollider != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, enemyCollider, ignore);
                }
            }
        }

        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public int GetCurrentMana() => currentMana;
        public int GetMaxMana() => maxMana;

        /// <summary>
        /// 플레이어 이름 설정 (음성인식 상태 표시용)
        /// </summary>
        public void SetPlayerName(string name)
        {
            playerName = name;
            if (nameText != null)
            {
                nameText.text = name;
            }
        }

        /// <summary>
        /// 플레이어 이름 가져오기
        /// </summary>
        public string GetPlayerName()
        {
            return playerName;
        }

        // ========== 스킬 시스템 ==========

        /// <summary>
        /// 스킬 사용
        /// </summary>
        public bool CastSkill(LostSpells.Data.SkillData skillData)
        {
            // 마나 체크
            if (currentMana < skillData.manaCost)
            {
                Debug.LogWarning($"[Player] 마나 부족! 현재: {currentMana}, 필요: {skillData.manaCost}");
                return false;
            }

            // 스킬 프리팹 로드
            GameObject skillPrefab = LoadSkillPrefab(skillData.effectPrefabPath);
            if (skillPrefab == null)
            {
                Debug.LogError($"[Player] 스킬 프리팹을 찾을 수 없음: {skillData.effectPrefabPath}");
                return false;
            }

            // 발사 위치 결정
            Vector3 castPosition = GetSkillCastPosition();
            Quaternion castRotation = GetSkillCastRotation();

            // 스킬 생성
            GameObject skillInstance = Instantiate(skillPrefab, castPosition, castRotation);

            // 스킬에 데이터 전달 (스킬 스크립트가 있다면)
            var skillScript = skillInstance.GetComponent<SkillBehavior>();
            if (skillScript != null)
            {
                skillScript.Initialize(skillData, this);
            }

            // 마나 소모
            currentMana -= (int)skillData.manaCost;
            UpdateHealthBar();

            Debug.Log($"[Player] 스킬 사용: {skillData.skillName} (마나: {currentMana}/{maxMana})");
            return true;
        }

        /// <summary>
        /// 스킬 발사 위치 가져오기
        /// </summary>
        private Vector3 GetSkillCastPosition()
        {
            if (skillCastPoint != null)
            {
                return skillCastPoint.position;
            }
            else
            {
                // 발사 지점이 없으면 플레이어 위치 + 오프셋 사용
                Vector3 offset = spriteRenderer.flipX ? Vector3.left * 0.5f : Vector3.right * 0.5f;
                return transform.position + offset + Vector3.up * 0.3f;
            }
        }

        /// <summary>
        /// 스킬 발사 방향 가져오기
        /// </summary>
        private Quaternion GetSkillCastRotation()
        {
            // 플레이어가 보는 방향으로 스킬 발사
            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                return Quaternion.Euler(0, 180, 0); // 왼쪽
            }
            else
            {
                return Quaternion.identity; // 오른쪽
            }
        }

        /// <summary>
        /// 스킬 프리팹 로드 (Resources 폴더에서)
        /// </summary>
        private GameObject LoadSkillPrefab(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return null;

            // Resources 폴더에서 로드 (예: "Prefabs/Skills/Fireball")
            return Resources.Load<GameObject>(prefabPath);
        }

        /// <summary>
        /// 스킬 발사 위치 Transform 가져오기 (외부에서 참조용)
        /// </summary>
        public Transform GetSkillCastPoint()
        {
            return skillCastPoint;
        }

        // ========== 키 바인딩 ==========

        /// <summary>
        /// SaveData에서 왼쪽 이동 키 가져오기
        /// </summary>
        private Key GetMoveLeftKey()
        {
            var saveData = LostSpells.Data.SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("MoveLeft"))
            {
                string keyString = saveData.keyBindings["MoveLeft"];
                return ParseKey(keyString, Key.A);
            }

            // 기본값: A
            return Key.A;
        }

        /// <summary>
        /// SaveData에서 오른쪽 이동 키 가져오기
        /// </summary>
        private Key GetMoveRightKey()
        {
            var saveData = LostSpells.Data.SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("MoveRight"))
            {
                string keyString = saveData.keyBindings["MoveRight"];
                return ParseKey(keyString, Key.D);
            }

            // 기본값: D
            return Key.D;
        }

        /// <summary>
        /// SaveData에서 점프 키 가져오기
        /// </summary>
        private Key GetJumpKey()
        {
            var saveData = LostSpells.Data.SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("Jump"))
            {
                string keyString = saveData.keyBindings["Jump"];
                return ParseKey(keyString, Key.W);
            }

            // 기본값: W
            return Key.W;
        }

        /// <summary>
        /// 키 문자열을 Key enum으로 변환 (Options의 GetKeyDisplayName 역함수)
        /// </summary>
        private Key ParseKey(string keyString, Key defaultKey)
        {
            // 특수 키 매핑 (Options의 GetKeyDisplayName과 반대)
            switch (keyString)
            {
                case "Space": return Key.Space;
                case "LShift": return Key.LeftShift;
                case "RShift": return Key.RightShift;
                case "LCtrl": return Key.LeftCtrl;
                case "RCtrl": return Key.RightCtrl;
                case "LAlt": return Key.LeftAlt;
                case "RAlt": return Key.RightAlt;
                case "Tab": return Key.Tab;
                case "Enter": return Key.Enter;
                case "Backspace": return Key.Backspace;
                default:
                    // 일반 키는 Enum.TryParse 시도
                    if (System.Enum.TryParse<Key>(keyString, true, out Key key))
                    {
                        return key;
                    }
                    return defaultKey;
            }
        }
    }
}
