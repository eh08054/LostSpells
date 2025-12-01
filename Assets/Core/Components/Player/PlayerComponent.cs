using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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
        [SerializeField] private float manaRegenRate = 5f; // 초당 마나 회복량
        [SerializeField] private float healthRegenRate = 5f; // 초당 체력 회복량
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f; // 점프 힘
        [SerializeField] private float knockbackForce = 3f; // 넉백 힘

        [Header("Skill System")]
        [SerializeField] private Transform skillCastPoint; // 스킬 발사 위치 (없으면 플레이어 중심 사용)
        [SerializeField] private LostSpells.Data.SkillData[] availableSkills = new LostSpells.Data.SkillData[6]; // 사용 가능한 스킬 (최대 6개: 1~6 키)
        [SerializeField] private GameObject skillProjectilePrefab; // 투사체 프리팹

        private float[] skillCooldowns = new float[6]; // 각 스킬의 쿨다운

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Animator animator; // 애니메이터

        [Header("UI Elements")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Transform healthBarBackground;
        [SerializeField] private Transform healthBarFill;

        private Rigidbody2D rb;
        private Collider2D playerCollider;
        private bool isKnockedBack = false; // 넉백 중인지 여부
        private bool isGrounded = false; // 땅에 닿아있는지 여부
        private float manaRegenAccumulator = 0f; // 마나 회복 누적값
        private float healthRegenAccumulator = 0f; // 체력 회복 누적값

        [Header("Sound Move Settings")]
        public float gridSize = 1.0f; // 한 칸의 크기
        public float autoMoveSpeed = 5.0f; // 음성 명령 시 이동 속도

        private bool isAutoMoving = false; // 현재 음성 이동 중인지 체크
        private Coroutine currentMoveCoroutine;
        private const float PIXEL_TO_UNIT_FACTOR = 1.0f;
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
            rb.gravityScale = 3; // 중력 강하게 적용 (빠른 낙하)
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
            // 이름 설정
            if (nameText != null)
            {
                nameText.text = playerName;
            }

            // 체력바 초기화
            UpdateHealthBar();

            // 스킬 데이터 초기화
            InitializeDefaultSkills();

            // 투사체 프리팹 자동 생성
            if (skillProjectilePrefab == null)
            {
                CreateProjectilePrefab();
            }
        }

        private void Update()
        {
            if (rb == null) return;

            // 마나 자동 회복
            RegenerateMana();

            // 체력 자동 회복
            RegenerateHealth();

            // 스킬 쿨다운 감소
            UpdateSkillCooldowns();

            // 스킬 입력 처리
            HandleSkillInput();

            // 넉백 중에는 이동 불가
            if (isKnockedBack) return;

            if (isAutoMoving)
            {
                UpdateAnimationState(); 
                return;
            }

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

            // 이동 방향에 따라 스프라이트 뒤집기
            HandleSpriteFlip(horizontal);
            UpdateAnimationState();
        }

        // 음성으로 움직임 명령 시 호출되는 함수
        public void ExecuteMoveCommand(string direction, int amount)
        {
            if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);

            float dirMultiplier = (direction == "left") ? -1f : 1f;

            float targetDistance = amount * gridSize;

            currentMoveCoroutine = StartCoroutine(MoveToTargetRoutine(dirMultiplier, targetDistance));
        }

        private IEnumerator MoveToTargetRoutine(float directionSign, float distance)
        {
            isAutoMoving = true; // 키보드 제어 차단

            float startX = transform.position.x;
            float targetX = startX + (directionSign * distance);

            float timeOut = 3.0f;
            float timer = 0f;

            while (timer < timeOut)
            {
                timer += Time.deltaTime;

                // 현재 위치와 목표 위치의 차이 계산
                float distanceRemaining = Mathf.Abs(transform.position.x - targetX);

                // 목표 근처(0.1f)에 도달했으면 종료
                if (distanceRemaining < 0.1f) break;

                // 물리 속도 적용
                Vector2 velocity = rb.linearVelocity;
                velocity.x = directionSign * autoMoveSpeed;
                rb.linearVelocity = velocity;

                // 방향 전환
                HandleSpriteFlip(directionSign);
                UpdateAnimationState(); // 걷는 애니메이션 갱신

                yield return null; // 다음 프레임까지 대기
            }

            // 이동 종료 처리
            Vector2 stopVelocity = rb.linearVelocity;
            stopVelocity.x = 0f;
            rb.linearVelocity = stopVelocity;

            isAutoMoving = false; // 키보드 제어 복구
            UpdateAnimationState(); 
        }
        private void HandleSpriteFlip(float horizontal)
        {
            if (horizontal < 0f) spriteRenderer.flipX = true;
            else if (horizontal > 0f) spriteRenderer.flipX = false;
        }
        private void UpdateAnimationState()
        {
            if (animator != null)
            {
                float speed = Mathf.Abs(rb.linearVelocity.x);
                animator.SetFloat("Speed", speed);
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
        /// 데미지 받기 (넉백 포함)
        /// </summary>
        public void TakeDamage(int damage, Vector2 knockbackDirection)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            UpdateHealthBar();

            // 넉백 적용
            if (rb != null && !isKnockedBack)
            {
                rb.linearVelocity = knockbackDirection.normalized * knockbackForce;
                isKnockedBack = true;
                IgnoreAllEnemyCollisions(true);
            }

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

            // 충돌 비활성화 (모든 Collider)
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            // 입력 비활성화 (플레이어가 더 이상 조작할 수 없도록)
            enabled = false;

            // 게임 오버 UI 표시
            LostSpells.UI.InGameUI inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();
            if (inGameUI != null)
            {
                inGameUI.ShowGameOver();
            }
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
        /// 부활 - 체력 전체 회복 및 상태 초기화
        /// </summary>
        public void Revive()
        {
            // 체력 100% 회복
            currentHealth = maxHealth;
            UpdateHealthBar();

            // Death 애니메이션 해제 및 Idle 상태로 강제 전환
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("IsDead", false);
                animator.SetFloat("Speed", 0f);

                // 애니메이터를 강제로 Idle 상태로 전환
                animator.Play("Idle", 0, 0f);
            }

            // 물리 재활성화
            if (rb != null)
            {
                rb.gravityScale = 3;
                rb.linearVelocity = Vector2.zero; // 속도 초기화
            }

            // 충돌 재활성화 (모든 Collider)
            Collider2D[] colliders = GetComponents<Collider2D>();
            Debug.Log($"[Revive] 총 {colliders.Length}개의 Collider 재활성화 중");
            foreach (var collider in colliders)
            {
                collider.enabled = true;
                Debug.Log($"[Revive] Collider 재활성화: {collider.GetType().Name}, isTrigger: {collider.isTrigger}");
            }

            // 넉백 상태 해제
            isKnockedBack = false;

            // 입력 재활성화
            enabled = true;
        }

        /// <summary>
        /// 마나 자동 회복
        /// </summary>
        private void RegenerateMana()
        {
            if (currentMana < maxMana)
            {
                // 누적 방식으로 마나 회복 (소수점 단위로 누적)
                manaRegenAccumulator += manaRegenRate * Time.deltaTime;

                // 누적값이 1 이상이면 정수로 변환하여 마나 회복
                if (manaRegenAccumulator >= 1f)
                {
                    int manaToAdd = Mathf.FloorToInt(manaRegenAccumulator);
                    currentMana += manaToAdd;
                    currentMana = Mathf.Min(currentMana, maxMana);
                    manaRegenAccumulator -= manaToAdd;
                }
            }
        }

        /// <summary>
        /// 체력 자동 회복
        /// </summary>
        private void RegenerateHealth()
        {
            if (currentHealth < maxHealth)
            {
                // 누적 방식으로 체력 회복 (소수점 단위로 누적)
                healthRegenAccumulator += healthRegenRate * Time.deltaTime;

                // 누적값이 1 이상이면 정수로 변환하여 체력 회복
                if (healthRegenAccumulator >= 1f)
                {
                    int healthToAdd = Mathf.FloorToInt(healthRegenAccumulator);
                    currentHealth += healthToAdd;
                    currentHealth = Mathf.Min(currentHealth, maxHealth);
                    healthRegenAccumulator -= healthToAdd;

                    // 체력바 업데이트
                    UpdateHealthBar();
                }
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
        public bool CastSkill(LostSpells.Data.SkillData skillData, string direction, int location)
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

            if ((direction == "right" || direction == "left") && location != 0)
            {
                Vector3 castOrigin = GetSkillCastPosition();

                // 3. 최종 발사 위치 (Position) 계산
                float xOffset = location * PIXEL_TO_UNIT_FACTOR;
                float directionSign = 0f;

                if (direction.ToLower() == "left")
                {
                    directionSign = -1f;
                }
                else if (direction.ToLower() == "right")
                {
                    directionSign = 1f;
                }

                // 최종 스킬 생성 위치 (Position)
                castPosition = castOrigin + new Vector3(xOffset * directionSign, 0, 0);
            }

            // 스킬 생성
            GameObject skillInstance = Instantiate(skillPrefab, castPosition, castRotation);

            // 스킬에 데이터 전달 (스킬 스크립트가 있다면)
            var skillScript = skillInstance.GetComponent<SkillBehavior>();
            if (skillScript != null)
            {
                skillScript.Initialize(skillData, this);
            }
            else
            {
                Debug.LogWarning($"[Player] SkillBehavior 컴포넌트를 찾을 수 없음!");
            }

            // 마나 소모
            currentMana -= (int)skillData.manaCost;
            UpdateHealthBar();

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

        // ========== 스킬 시스템 ==========

        /// <summary>
        /// 기본 스킬 데이터 초기화 (스킬 데이터가 설정되지 않은 경우)
        /// </summary>
        private void InitializeDefaultSkills()
        {
            // 이미 스킬이 설정되어 있으면 초기화하지 않음
            bool hasSkills = false;
            for (int i = 0; i < availableSkills.Length; i++)
            {
                if (availableSkills[i] != null)
                {
                    hasSkills = true;
                    break;
                }
            }

            if (hasSkills) return;

            // 기본 스킬 6개 생성
            availableSkills[0] = CreateSkill("fireball", "Fireball", "화염구", LostSpells.Data.SkillType.Fireball,
                "Fire/FireBall", 15, 2f, 20, 12f);

            availableSkills[1] = CreateSkill("icespike", "Ice Spike", "얼음 가시", LostSpells.Data.SkillType.IceSpike,
                "Ice/IceSpike", 12, 1.8f, 15, 10f);

            availableSkills[2] = CreateSkill("lightning", "Thunder Strike", "번개", LostSpells.Data.SkillType.Lightning,
                "Electricity/ElectricLightning01", 18, 2.5f, 25, 15f);

            availableSkills[3] = CreateSkill("earthrock", "Stone Bullet", "돌 탄환", LostSpells.Data.SkillType.EarthRock,
                "Earth/EarthRock", 10, 1.5f, 12, 8f);

            availableSkills[4] = CreateSkill("holylight", "Divine Ray", "신성한 광선", LostSpells.Data.SkillType.HolyLight,
                "Holy/HolyProjectile", 20, 3f, 30, 14f);

            availableSkills[5] = CreateSkill("voidblast", "Void Orb", "암흑 구체", LostSpells.Data.SkillType.VoidBlast,
                "Void/VoidBall", 25, 3.5f, 35, 10f);
        }

        /// <summary>
        /// 스킬 데이터 생성
        /// </summary>
        private LostSpells.Data.SkillData CreateSkill(string id, string nameEn, string nameKo,
            LostSpells.Data.SkillType type, string vfxPath, int manaCost, float cooldown, int damage, float speed)
        {
            return new LostSpells.Data.SkillData
            {
                skillId = id,
                skillName = nameKo,
                skillNameEn = nameEn,
                skillType = type,
                manaCost = manaCost,
                cooldown = cooldown,
                damage = damage,
                projectileSpeed = speed,
                projectileLifetime = 3f,
                effectPrefabPath = $"Templates/Pixel Art/PixelArtRPGVFX/Prefabs/{vfxPath}"
            };
        }

        /// <summary>
        /// 투사체 프리팹 자동 생성
        /// </summary>
        private void CreateProjectilePrefab()
        {
            GameObject projectile = new GameObject("SkillProjectile");

            // CircleCollider2D 추가 (트리거)
            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.isTrigger = true;

            // Rigidbody2D 추가 (Kinematic)
            Rigidbody2D rb2d = projectile.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.gravityScale = 0f;

            // SkillProjectile 컴포넌트 추가
            projectile.AddComponent<SkillProjectile>();

            skillProjectilePrefab = projectile;
        }

        /// <summary>
        /// 스킬 쿨다운 업데이트
        /// </summary>
        private void UpdateSkillCooldowns()
        {
            for (int i = 0; i < skillCooldowns.Length; i++)
            {
                if (skillCooldowns[i] > 0)
                {
                    skillCooldowns[i] -= Time.deltaTime;
                    if (skillCooldowns[i] < 0)
                        skillCooldowns[i] = 0;
                }
            }
        }

        /// <summary>
        /// 스킬 입력 처리 (1~6 키)
        /// </summary>
        private void HandleSkillInput()
        {
            if (Keyboard.current == null) return;

            // 1~6 키 체크
            for (int i = 0; i < 6; i++)
            {
                Key skillKey = Key.Digit1 + i; // Digit1, Digit2, ..., Digit6
                if (Keyboard.current[skillKey].wasPressedThisFrame)
                {
                    CastSkill(i, "none", 0);
                }
            }
        }

        /// <summary>
        /// 스킬 시전
        /// </summary>
        private void CastSkill(int skillIndex, string direction, int location)
        {
            // 유효성 검사
            if (skillIndex < 0 || skillIndex >= availableSkills.Length)
                return;

            LostSpells.Data.SkillData skill = availableSkills[skillIndex];
            if (skill == null)
                return;

            // 쿨다운 체크
            if (skillCooldowns[skillIndex] > 0)
                return;

            // 마나 체크
            if (currentMana < skill.manaCost)
                return;

            // 마나 소모
            currentMana -= (int)skill.manaCost;

            // 쿨다운 시작
            skillCooldowns[skillIndex] = skill.cooldown;

            // 투사체 발사
            FireProjectile(skill);
        }

        /// <summary>
        /// 투사체 발사
        /// </summary>
        private void FireProjectile(LostSpells.Data.SkillData skill)
        {
            // 발사 위치 결정
            Vector3 spawnPosition = skillCastPoint != null ? skillCastPoint.position : transform.position;

            // 발사 방향 결정 (플레이어가 보는 방향)
            Vector3 direction = spriteRenderer.flipX ? Vector3.left : Vector3.right;

            // VFX 프리팹 로드
            GameObject vfxPrefab = null;
            if (!string.IsNullOrEmpty(skill.effectPrefabPath))
            {
                vfxPrefab = UnityEngine.Resources.Load<GameObject>(skill.effectPrefabPath);
            }

            // 투사체 생성
            if (skillProjectilePrefab != null)
            {
                GameObject projectile = Instantiate(skillProjectilePrefab, spawnPosition, Quaternion.identity);

                // SkillProjectile 컴포넌트 초기화
                SkillProjectile projectileScript = projectile.GetComponent<SkillProjectile>();
                if (projectileScript == null)
                {
                    projectileScript = projectile.AddComponent<SkillProjectile>();
                }

                projectileScript.Initialize(
                    (int)skill.damage,
                    skill.projectileSpeed,
                    skill.projectileLifetime,
                    direction,
                    vfxPrefab
                );

                // 투사체에 VFX 이펙트를 자식으로 추가 (발사 중 이펙트)
                if (vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(vfxPrefab, projectile.transform);
                    vfx.transform.localPosition = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 특정 스킬의 쿨다운 확인
        /// </summary>
        public float GetSkillCooldown(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= skillCooldowns.Length)
                return 0f;
            return skillCooldowns[skillIndex];
        }

        /// <summary>
        /// 특정 스킬 정보 가져오기
        /// </summary>
        public LostSpells.Data.SkillData GetSkill(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= availableSkills.Length)
                return null;
            return availableSkills[skillIndex];
        }
    }
}
