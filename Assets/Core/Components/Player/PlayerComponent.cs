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
        [SerializeField] private float manaRegenRate = 5f; // 초당 마나 회복량
        [SerializeField] private float healthRegenRate = 5f; // 초당 체력 회복량
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f; // 점프 힘
        [SerializeField] private float knockbackForce = 3f; // 넉백 힘

        [Header("Skill System")]
        [SerializeField] private Transform skillCastPoint; // 스킬 발사 위치 (없으면 플레이어 중심 사용)
        [SerializeField] private LostSpells.Data.SkillData[] availableSkills = new LostSpells.Data.SkillData[34]; // 모든 VFX 스킬 (34개)
        [SerializeField] private GameObject skillProjectilePrefab; // 투사체 프리팹

        private float[] skillCooldowns = new float[34]; // 각 스킬의 쿨다운
        private const int TOTAL_SKILLS = 34;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Animator animator; // 애니메이터

        [Header("UI Elements")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Transform healthBarBackground;
        [SerializeField] private Transform healthBarFill;

        [Header("Voice Recognition")]
        [SerializeField] private ParticleSystem voiceRecognitionParticle; // 음성인식 중 표시할 파티클

        private Rigidbody2D rb;
        private Collider2D playerCollider;
        private bool isGrounded = false; // 땅에 닿아있는지 여부
        private float manaRegenAccumulator = 0f; // 마나 회복 누적값
        private float healthRegenAccumulator = 0f; // 체력 회복 누적값

        // 음성 명령 이동
        private int voiceMovementDirection = 0; // -1: 왼쪽, 0: 정지, 1: 오른쪽
        private bool voiceJumpRequested = false;
        private bool stopAfterLanding = false; // 착지 후 이동 멈춤 플래그
        private bool wasGrounded = true; // 이전 프레임 착지 상태

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

            // 스킬 배열 크기 확인 및 재할당 (Prefab에 저장된 크기가 다를 수 있음)
            if (availableSkills == null || availableSkills.Length != TOTAL_SKILLS)
            {
                availableSkills = new LostSpells.Data.SkillData[TOTAL_SKILLS];
            }
            if (skillCooldowns == null || skillCooldowns.Length != TOTAL_SKILLS)
            {
                skillCooldowns = new float[TOTAL_SKILLS];
            }

            // 스킬 데이터 초기화 (Awake에서 초기화하여 다른 컴포넌트가 Start에서 접근 가능하도록)
            InitializeDefaultSkills();

            // 음성인식 파티클 자동 찾기 (Inspector에서 설정하지 않은 경우)
            if (voiceRecognitionParticle == null)
            {
                voiceRecognitionParticle = GetComponentInChildren<ParticleSystem>(true);
            }

            // 음성인식 파티클 즉시 비활성화 (Awake에서 처리하여 시작 시 보이지 않도록)
            if (voiceRecognitionParticle != null)
            {
                voiceRecognitionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                voiceRecognitionParticle.gameObject.SetActive(false);
            }
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

            // Player와 Enemy 레이어 간 물리 충돌 무시 (서로 통과)
            SetupEnemyCollisionIgnore();
        }

        /// <summary>
        /// Player와 Enemy 간 물리 충돌 무시 설정
        /// </summary>
        private void SetupEnemyCollisionIgnore()
        {
            // 플레이어 레이어 자동 설정
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                gameObject.layer = playerLayer;
            }

            // 레이어 기반 충돌 무시
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (playerLayer >= 0 && enemyLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
            }

            // 씬에 있는 모든 적과의 충돌 직접 무시 (레이어 설정이 안 되어 있을 경우 대비)
            IgnoreAllEnemyCollisions(true);

            // 적 감지용 트리거 콜라이더 추가
            if (playerCollider is CircleCollider2D circleCol)
            {
                CircleCollider2D trigger = gameObject.AddComponent<CircleCollider2D>();
                trigger.radius = circleCol.radius;
                trigger.offset = circleCol.offset;
                trigger.isTrigger = true;
            }
            else if (playerCollider is BoxCollider2D boxCol)
            {
                BoxCollider2D trigger = gameObject.AddComponent<BoxCollider2D>();
                trigger.size = boxCol.size;
                trigger.offset = boxCol.offset;
                trigger.isTrigger = true;
            }
        }

        /// <summary>
        /// 모든 적과의 물리 충돌 무시/활성화
        /// </summary>
        public void IgnoreAllEnemyCollisions(bool ignore)
        {
            if (playerCollider == null) return;

            // 씬에 있는 모든 EnemyComponent 찾기
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

        /// <summary>
        /// 특정 적과의 물리 충돌 무시 (새로 스폰된 적용)
        /// </summary>
        public void IgnoreEnemyCollision(Collider2D enemyCollider, bool ignore = true)
        {
            if (playerCollider != null && enemyCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, enemyCollider, ignore);
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

            // 키바인딩에서 이동 키 가져오기
            Key moveLeftKey = GetMoveLeftKey();
            Key moveRightKey = GetMoveRightKey();
            Key jumpKey = GetJumpKey();

            // 좌우 이동 (새 Input System 사용 + 음성 명령)
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

            // 착지 감지 (공중에서 땅으로)
            if (!wasGrounded && isGrounded && stopAfterLanding)
            {
                // 점프 후 착지 - 이동 멈춤
                voiceMovementDirection = 0;
                stopAfterLanding = false;
            }

            // 음성 명령 이동 (키보드 입력이 없을 때만 적용)
            if (horizontal == 0f && voiceMovementDirection != 0)
            {
                horizontal = voiceMovementDirection;
            }

            // 음성 명령 점프
            if (voiceJumpRequested && isGrounded)
            {
                // 이동 중 점프면 착지 후 멈춤 플래그 설정
                if (voiceMovementDirection != 0)
                {
                    stopAfterLanding = true;
                }

                Vector2 velocity = rb.linearVelocity;
                velocity.y = jumpForce;
                rb.linearVelocity = velocity;
                voiceJumpRequested = false;
            }

            // 착지 상태 저장 (다음 프레임 비교용)
            wasGrounded = isGrounded;

            // Rigidbody2D로 좌우 이동 (Y축 속도는 유지하여 중력 영향 받도록)
            Vector2 velocity2 = rb.linearVelocity;
            velocity2.x = horizontal * moveSpeed;
            rb.linearVelocity = velocity2;

            // 이동 방향에 따라 스프라이트 뒤집기
            if (horizontal < 0f) // 왼쪽으로 이동
            {
                spriteRenderer.flipX = true;
            }
            else if (horizontal > 0f) // 오른쪽으로 이동
            {
                spriteRenderer.flipX = false;
            }

            // 애니메이터 Speed 파라미터 업데이트
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                float speed = Mathf.Abs(velocity2.x);
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
            // 넉백 없이 데미지만 적용 (하위 호환성 유지)
            TakeDamage(damage);
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
            // Debug.Log($"[Revive] 총 {colliders.Length}개의 Collider 재활성화 중");
            foreach (var collider in colliders)
            {
                collider.enabled = true;
                // Debug.Log($"[Revive] Collider 재활성화: {collider.GetType().Name}, isTrigger: {collider.isTrigger}");
            }

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
        /// 적과 접촉 시 데미지 (트리거 충돌 - 물리적 충돌 없이 통과)
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 적과 접촉했는지 확인
            EnemyComponent enemy = other.GetComponent<EnemyComponent>();
            if (enemy != null)
            {
                // 데미지 받기 (물리적 충돌 없음)
                TakeDamage(10); // 적과 접촉 시 10 데미지
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

        // ========== 음성인식 파티클 ==========

        /// <summary>
        /// 음성인식 파티클 표시 (음성 녹음 시작 시 호출)
        /// </summary>
        public void ShowVoiceRecognitionParticle()
        {
            // Debug.Log($"[PlayerComponent] ShowVoiceRecognitionParticle 호출됨, particle: {(voiceRecognitionParticle != null ? voiceRecognitionParticle.name : "null")}");
            if (voiceRecognitionParticle != null)
            {
                // 랜덤 색상 선택 (빨간색, 파란색, 회색)
                Color[] colors = new Color[] { Color.red, Color.blue, Color.gray };
                Color randomColor = colors[Random.Range(0, colors.Length)];

                // 파티클 시스템의 시작 색상 설정
                var main = voiceRecognitionParticle.main;
                main.startColor = randomColor;

                voiceRecognitionParticle.gameObject.SetActive(true);
                voiceRecognitionParticle.Clear(true); // 이전 파티클 클리어
                voiceRecognitionParticle.Play(true);  // 자식 파티클도 함께 재생
                // Debug.Log($"[PlayerComponent] 파티클 재생 시작, isPlaying: {voiceRecognitionParticle.isPlaying}, color: {randomColor}");
            }
        }

        /// <summary>
        /// 음성인식 파티클 숨김 (음성인식 완료 시 호출)
        /// </summary>
        public void HideVoiceRecognitionParticle()
        {
            // Debug.Log($"[PlayerComponent] HideVoiceRecognitionParticle 호출됨");
            if (voiceRecognitionParticle != null)
            {
                voiceRecognitionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                voiceRecognitionParticle.gameObject.SetActive(false);
            }
        }

        // ========== 음성 명령 이동 ==========

        /// <summary>
        /// 음성 명령으로 이동 방향 설정
        /// </summary>
        /// <param name="direction">-1: 왼쪽, 0: 정지, 1: 오른쪽</param>
        public void SetVoiceMovement(int direction)
        {
            voiceMovementDirection = Mathf.Clamp(direction, -1, 1);
        }

        /// <summary>
        /// 음성 명령으로 점프 요청
        /// </summary>
        public void VoiceJump()
        {
            voiceJumpRequested = true;
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
        /// 기본 스킬 데이터 초기화 (Pixel Art VFX 에셋 전체 사용 - 34개)
        /// </summary>
        private void InitializeDefaultSkills()
        {
            int i = 0;
            // 파라미터: id, nameEn, nameKo, type, vfxPath, mana, cooldown, damage, speed, pierce, lifetime

            // ===== Fire (불) 스킬 - 5개 =====
            availableSkills[i++] = CreateSkill("fireball", "Fire Ball", "화염구", LostSpells.Data.SkillType.Attack,
                "VFX/Fire/FireBall", 5, 0.8f, 25, 12f, 0, 2.5f);  // 기본 투사체
            availableSkills[i++] = CreateSkill("fireshield", "Fire Shield", "화염 방패", LostSpells.Data.SkillType.Defense,
                "VFX/Fire/FireShield", 8, 1.5f, 0, 0f, 0, 3f);
            availableSkills[i++] = CreateSkill("firetornado", "Fire Tornado", "화염 회오리", LostSpells.Data.SkillType.Attack,
                "VFX/Fire/FireTornado", 10, 2.0f, 35, 6f, 3, 4f);  // 3회 관통, 긴 사거리
            availableSkills[i++] = CreateSkill("fireslash", "Fire Slash", "불꽃 베기", LostSpells.Data.SkillType.Attack,
                "VFX/Fire/FireSlash", 3, 0.5f, 15, 18f, 0, 1.5f);  // 빠르고 짧은 사거리
            availableSkills[i++] = CreateSkill("fireexplosion", "Fire Explosion", "화염 폭발", LostSpells.Data.SkillType.Attack,
                "VFX/Fire/FireExplosion1", 12, 2.5f, 50, 5f, -1, 3f);  // 무한 관통 (폭발)

            // ===== Ice (얼음) 스킬 - 6개 =====
            availableSkills[i++] = CreateSkill("iceball", "Ice Ball", "얼음 구슬", LostSpells.Data.SkillType.Attack,
                "VFX/Ice/IceBall", 5, 0.8f, 22, 11f, 0, 2.5f);
            availableSkills[i++] = CreateSkill("iceshield", "Ice Shield", "얼음 방패", LostSpells.Data.SkillType.Defense,
                "VFX/Ice/IceShield", 8, 1.5f, 0, 0f, 0, 3f);
            availableSkills[i++] = CreateSkill("icespike", "Ice Spike", "얼음 가시", LostSpells.Data.SkillType.Attack,
                "VFX/Ice/IceSpike", 6, 1.0f, 28, 14f, 2, 3f);  // 2회 관통
            availableSkills[i++] = CreateSkill("iceslash", "Ice Slash", "얼음 베기", LostSpells.Data.SkillType.Attack,
                "VFX/Ice/IceSlash", 3, 0.5f, 14, 16f, 0, 1.5f);  // 빠르고 짧음
            availableSkills[i++] = CreateSkill("iceprojectile", "Ice Projectile", "얼음 투사체", LostSpells.Data.SkillType.Attack,
                "VFX/Ice/IceProjectile", 7, 1.2f, 30, 15f, 1, 4f);  // 1회 관통, 긴 사거리
            availableSkills[i++] = CreateSkill("iceclaw", "Ice Claw", "얼음 발톱", LostSpells.Data.SkillType.Attack,
                "VFX/Ice/IceClaw", 4, 0.6f, 18, 12f, 0, 2f);

            // ===== Electricity (번개) 스킬 - 6개 =====
            availableSkills[i++] = CreateSkill("electricball", "Electric Ball", "전기 구슬", LostSpells.Data.SkillType.Attack,
                "VFX/Electricity/ElectricBall", 6, 0.8f, 24, 16f, 0, 3f);  // 빠른 투사체
            availableSkills[i++] = CreateSkill("electricshield", "Electric Shield", "전기 방패", LostSpells.Data.SkillType.Defense,
                "VFX/Electricity/ElectricShield", 8, 1.5f, 0, 0f, 0, 3f);
            availableSkills[i++] = CreateSkill("electrictornado", "Electric Tornado", "번개 회오리", LostSpells.Data.SkillType.Attack,
                "VFX/Electricity/ElectricTornado", 10, 2.0f, 40, 7f, 5, 5f);  // 5회 관통, 매우 긴 사거리
            availableSkills[i++] = CreateSkill("electricslash", "Electric Slash", "번개 베기", LostSpells.Data.SkillType.Attack,
                "VFX/Electricity/ElectricSlash", 4, 0.4f, 18, 20f, 0, 1.2f);  // 매우 빠름
            availableSkills[i++] = CreateSkill("electricexplosion", "Electric Explosion", "번개 폭발", LostSpells.Data.SkillType.Attack,
                "VFX/Electricity/ElectricExplosion", 14, 2.5f, 55, 5f, -1, 3.5f);  // 무한 관통
            availableSkills[i++] = CreateSkill("lightning", "Lightning", "번개", LostSpells.Data.SkillType.Attack,
                "VFX/Electricity/ElectricLighting1", 8, 1.0f, 32, 25f, -1, 6f);  // 무한 관통, 매우 빠름, 긴 사거리

            // ===== Earth (대지) 스킬 - 5개 =====
            availableSkills[i++] = CreateSkill("earthball", "Earth Ball", "대지 구슬", LostSpells.Data.SkillType.Attack,
                "VFX/Earth/EarthBall", 6, 1.0f, 30, 8f, 0, 2.5f);  // 느리지만 강함
            availableSkills[i++] = CreateSkill("earthshield", "Earth Shield", "대지 방패", LostSpells.Data.SkillType.Defense,
                "VFX/Earth/EarthShield", 10, 2.0f, 0, 0f, 0, 4f);  // 긴 지속
            availableSkills[i++] = CreateSkill("earthrock", "Earth Rock", "암석 투척", LostSpells.Data.SkillType.Attack,
                "VFX/Earth/EarthRock", 8, 1.2f, 40, 7f, 1, 3f);  // 1회 관통, 강함
            availableSkills[i++] = CreateSkill("earthlava", "Lava", "용암", LostSpells.Data.SkillType.Attack,
                "VFX/Earth/EarthLava", 12, 2.5f, 55, 4f, -1, 4f);  // 무한 관통, 느림
            availableSkills[i++] = CreateSkill("earthspin", "Earth Spin", "대지 회전", LostSpells.Data.SkillType.Attack,
                "VFX/Earth/EarthSpin", 9, 1.8f, 35, 6f, 2, 3.5f);  // 2회 관통

            // ===== Holy (신성) 스킬 - 6개 =====
            availableSkills[i++] = CreateSkill("holyball", "Holy Ball", "신성 구슬", LostSpells.Data.SkillType.Attack,
                "VFX/Holy/HolyBall", 6, 1.0f, 28, 11f, 0, 3f);
            availableSkills[i++] = CreateSkill("holyshield", "Holy Shield", "신성 방패", LostSpells.Data.SkillType.Defense,
                "VFX/Holy/HolyShield", 10, 2.0f, 0, 0f, 0, 4f);
            availableSkills[i++] = CreateSkill("holycross", "Holy Cross", "신성 십자가", LostSpells.Data.SkillType.Attack,
                "VFX/Holy/HolyCross", 10, 1.5f, 45, 9f, 3, 4f);  // 3회 관통
            availableSkills[i++] = CreateSkill("holyslash", "Holy Slash", "신성 베기", LostSpells.Data.SkillType.Attack,
                "VFX/Holy/HolySlash", 5, 0.6f, 22, 15f, 0, 1.8f);
            availableSkills[i++] = CreateSkill("holyprojectile", "Holy Projectile", "신성 투사체", LostSpells.Data.SkillType.Attack,
                "VFX/Holy/HolyProjectile", 7, 1.2f, 32, 13f, 2, 5f);  // 2회 관통, 긴 사거리
            availableSkills[i++] = CreateSkill("holyblessing", "Holy Blessing", "신성 축복", LostSpells.Data.SkillType.Defense,
                "VFX/Holy/HolyBlessing", 15, 3.0f, 0, 0f, 0, 5f);

            // ===== Void (암흑) 스킬 - 6개 =====
            availableSkills[i++] = CreateSkill("voidball", "Void Ball", "암흑 구슬", LostSpells.Data.SkillType.Attack,
                "VFX/Void/VoidBall", 7, 1.0f, 32, 10f, 1, 3f);  // 1회 관통
            availableSkills[i++] = CreateSkill("voidshield", "Void Shield", "암흑 방패", LostSpells.Data.SkillType.Defense,
                "VFX/Void/VoidShield", 10, 2.0f, 0, 0f, 0, 4f);
            availableSkills[i++] = CreateSkill("voidblackhole", "Black Hole", "블랙홀", LostSpells.Data.SkillType.Attack,
                "VFX/Void/VoidBlackHole", 18, 3.5f, 70, 3f, -1, 5f);  // 무한 관통, 느리지만 매우 강함
            availableSkills[i++] = CreateSkill("voidslash", "Void Slash", "암흑 베기", LostSpells.Data.SkillType.Attack,
                "VFX/Void/VoidSlash", 5, 0.7f, 25, 14f, 0, 2f);
            availableSkills[i++] = CreateSkill("voidportal", "Void Portal", "암흑 포탈", LostSpells.Data.SkillType.Attack,
                "VFX/Void/VoidPortal", 14, 2.5f, 50, 5f, 4, 4.5f);  // 4회 관통
            availableSkills[i++] = CreateSkill("voidexplosion", "Void Explosion", "암흑 폭발", LostSpells.Data.SkillType.Attack,
                "VFX/Void/VoidExplosion1", 16, 3.0f, 60, 4f, -1, 4f);  // 무한 관통
        }

        /// <summary>
        /// 스킬 데이터 생성 (관통 및 사거리 포함)
        /// </summary>
        /// <param name="pierce">관통 횟수 (0=관통없음, -1=무한관통)</param>
        /// <param name="lifetime">투사체 수명/사거리 (초)</param>
        private LostSpells.Data.SkillData CreateSkill(string id, string nameEn, string nameKo,
            LostSpells.Data.SkillType type, string vfxPath, int manaCost, float cooldown, int damage, float speed,
            int pierce = 0, float lifetime = 3f)
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
                projectileLifetime = lifetime,
                pierceCount = pierce,
                effectPrefabPath = vfxPath,
                voiceKeyword = nameKo
            };
        }

        /// <summary>
        /// 투사체 프리팹 자동 생성
        /// </summary>
        private void CreateProjectilePrefab()
        {
            GameObject projectile = new GameObject("SkillProjectileTemplate");

            // 기본 스프라이트 추가 (VFX가 없을 때 보이도록)
            SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(32, Color.white);
            sr.sortingOrder = 50;
            projectile.transform.localScale = Vector3.one * 0.5f;

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

            // 템플릿 비활성화 (씬에서 보이지 않도록)
            projectile.SetActive(false);

            // 부모를 플레이어로 설정하여 관리
            projectile.transform.SetParent(transform);

            skillProjectilePrefab = projectile;
        }

        /// <summary>
        /// 원형 스프라이트 동적 생성
        /// </summary>
        private Sprite CreateCircleSprite(int size, Color color)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float radius = size / 2f;
            float radiusSq = radius * radius;
            Vector2 center = new Vector2(radius - 0.5f, radius - 0.5f);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq < radiusSq)
                    {
                        // 중심은 밝게, 가장자리로 갈수록 약간 어둡게
                        float normalizedDist = Mathf.Sqrt(distSq) / radius;
                        float brightness = 1f - normalizedDist * 0.3f;
                        pixels[y * size + x] = new Color(
                            color.r * brightness,
                            color.g * brightness,
                            color.b * brightness,
                            1f // 완전 불투명
                        );
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // pixelsPerUnit을 낮춰서 스프라이트가 더 크게 보이도록
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
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
                    CastSkill(i);
                }
            }
        }

        /// <summary>
        /// 스킬 시전
        /// </summary>
        private void CastSkill(int skillIndex)
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
            // 발사 위치 결정 (플레이어 위치 + 약간 앞으로)
            Vector3 offset = spriteRenderer.flipX ? Vector3.left * 0.5f : Vector3.right * 0.5f;
            Vector3 spawnPosition = transform.position + offset + Vector3.up * 0.3f;

            // 발사 방향 결정 (플레이어가 보는 방향)
            Vector3 direction = spriteRenderer.flipX ? Vector3.left : Vector3.right;

            // VFX 스프라이트 로드 (스프라이트 시트에서 모든 프레임 로드)
            Sprite[] vfxSprites = null;
            if (!string.IsNullOrEmpty(skill.effectPrefabPath))
            {
                vfxSprites = UnityEngine.Resources.LoadAll<Sprite>(skill.effectPrefabPath);
                if (vfxSprites != null && vfxSprites.Length > 0)
                {
                    // 로드된 스프라이트 이름 표시
                    string spriteNames = string.Join(", ", System.Array.ConvertAll(vfxSprites, s => s.name));
                    // Debug.Log($"[PlayerComponent] VFX 스프라이트 로드 성공: {skill.effectPrefabPath}\n  - {vfxSprites.Length}개 프레임: {spriteNames}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerComponent] VFX 스프라이트 로드 실패: {skill.effectPrefabPath}");
                }
            }

            // 투사체 생성
            GameObject projectile = new GameObject($"Skill_{skill.skillId}");
            projectile.transform.position = spawnPosition;

            // SpriteRenderer 추가
            SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 100;

            // VFX 스프라이트가 있으면 사용, 없으면 폴백 스프라이트 사용
            if (vfxSprites != null && vfxSprites.Length > 0)
            {
                sr.sprite = vfxSprites[0]; // 첫 번째 프레임으로 시작
                projectile.transform.localScale = Vector3.one * 1.5f; // 스프라이트 크기 조정

                // 애니메이션 컴포넌트 추가 (여러 프레임이 있을 경우)
                if (vfxSprites.Length > 1)
                {
                    SpriteAnimator animator = projectile.AddComponent<SpriteAnimator>();
                    animator.Initialize(vfxSprites, 12f); // 12 FPS 애니메이션
                }

                // Debug.Log($"[PlayerComponent] VFX 스프라이트 투사체 생성: {skill.skillId}");
            }
            else
            {
                // 폴백: 프로시저럴 스프라이트
                Color skillColor = GetSkillColor(skill.skillId);
                sr.sprite = CreateSkillSprite(skill.skillId, skillColor);
                projectile.transform.localScale = Vector3.one * 0.7f;

                // TrailRenderer 추가 (폴백 시에만)
                TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
                trail.time = 0.3f;
                trail.startWidth = 0.5f;
                trail.endWidth = 0.1f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = skillColor;
                trail.endColor = new Color(skillColor.r, skillColor.g, skillColor.b, 0f);
                trail.sortingOrder = 100;

                // Debug.Log($"[PlayerComponent] 폴백 투사체 생성 (스프라이트 없음): {skill.skillId}");
            }

            // 왼쪽을 향하면 뒤집기
            if (spriteRenderer.flipX)
            {
                sr.flipX = true;
            }

            // 충돌체 추가
            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.isTrigger = true;

            // Rigidbody2D 추가
            Rigidbody2D rb2d = projectile.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.gravityScale = 0f;

            // SkillProjectile 컴포넌트 추가 및 초기화
            SkillProjectile projectileScript = projectile.AddComponent<SkillProjectile>();
            projectileScript.Initialize(
                (int)skill.damage,
                skill.projectileSpeed,
                skill.projectileLifetime,
                direction,
                null, // 충돌 VFX는 나중에 추가
                skill.pierceCount // 관통 횟수
            );

            // Debug.Log($"[PlayerComponent] 투사체 발사: {skill.skillId} at {spawnPosition}, direction: {direction}");
        }

        /// <summary>
        /// 스킬별 다른 모양의 스프라이트 생성
        /// </summary>
        private Sprite CreateSkillSprite(string skillId, Color color)
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            float center = size / 2f;

            switch (skillId)
            {
                case "fireball":
                    // 불꽃 모양 (물방울을 옆으로 뒤집은 형태)
                    DrawFlameShape(pixels, size, color);
                    break;
                case "iceball":
                    // 다이아몬드/크리스탈 모양
                    DrawDiamondShape(pixels, size, color);
                    break;
                case "electricball":
                    // 별/번개 모양
                    DrawStarShape(pixels, size, color);
                    break;
                case "voidball":
                    // 링/도넛 모양
                    DrawRingShape(pixels, size, color);
                    break;
                case "fireshield":
                case "iceshield":
                    // 방패 모양
                    DrawShieldShape(pixels, size, color);
                    break;
                default:
                    // 기본 원형
                    DrawCircleShape(pixels, size, color);
                    break;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private void DrawCircleShape(Color[] pixels, int size, Color color)
        {
            float center = size / 2f - 0.5f;
            float radiusSq = (size / 2f) * (size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    if (dx * dx + dy * dy < radiusSq)
                        pixels[y * size + x] = color;
                }
            }
        }

        private void DrawFlameShape(Color[] pixels, int size, Color color)
        {
            // 불꽃: 오른쪽으로 뾰족한 물방울 형태
            float centerY = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float ny = (y - centerY) / (size / 2f);
                    float nx = x / (float)size;

                    // 왼쪽은 둥글고, 오른쪽으로 갈수록 좁아짐
                    float width = (1f - nx) * 0.8f;
                    if (Mathf.Abs(ny) < width && nx > 0.1f)
                    {
                        // 밝기 그라데이션
                        float brightness = 1f - nx * 0.5f;
                        pixels[y * size + x] = new Color(color.r * brightness, color.g * brightness, color.b * brightness, 1f);
                    }
                }
            }
        }

        private void DrawDiamondShape(Color[] pixels, int size, Color color)
        {
            // 다이아몬드: 마름모 형태
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);
                    if (dx + dy < center * 0.9f)
                    {
                        float dist = (dx + dy) / center;
                        float brightness = 1f - dist * 0.3f;
                        pixels[y * size + x] = new Color(color.r * brightness, color.g * brightness, color.b * brightness, 1f);
                    }
                }
            }
        }

        private void DrawStarShape(Color[] pixels, int size, Color color)
        {
            // 별: 4개의 뾰족한 끝
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // 별 모양: 4개의 뾰족한 끝
                    float starRadius = center * (0.4f + 0.5f * Mathf.Abs(Mathf.Cos(angle * 2f)));

                    if (dist < starRadius)
                    {
                        float brightness = 1f - dist / starRadius * 0.3f;
                        pixels[y * size + x] = new Color(color.r * brightness, color.g * brightness, color.b * brightness, 1f);
                    }
                }
            }
        }

        private void DrawRingShape(Color[] pixels, int size, Color color)
        {
            // 링: 도넛 형태
            float center = size / 2f;
            float outerRadius = center * 0.9f;
            float innerRadius = center * 0.4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < outerRadius && dist > innerRadius)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        private void DrawShieldShape(Color[] pixels, int size, Color color)
        {
            // 방패: 위가 둥글고 아래가 뾰족한 형태
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / center;
                    float ny = (y - center) / center;

                    // 위쪽은 넓고, 아래쪽으로 갈수록 좁아짐
                    float width = ny < 0 ? 0.8f : 0.8f * (1f - ny);
                    if (Mathf.Abs(nx) < width && ny > -0.8f && ny < 0.9f)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        /// <summary>
        /// 스킬 ID에 따른 색상 반환
        /// </summary>
        private Color GetSkillColor(string skillId)
        {
            switch (skillId)
            {
                case "fireball":
                case "fireshield":
                    return new Color(1f, 0.4f, 0.1f); // 주황/빨강 (불)
                case "iceball":
                case "iceshield":
                    return new Color(0.4f, 0.8f, 1f); // 하늘색 (얼음)
                case "electricball":
                    return new Color(1f, 1f, 0.3f); // 노란색 (전기)
                case "voidball":
                    return new Color(0.6f, 0.2f, 0.8f); // 보라색 (공허)
                default:
                    return Color.white;
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

        /// <summary>
        /// 모든 스킬 가져오기 (UI에서 사용)
        /// </summary>
        public LostSpells.Data.SkillData[] GetAllSkills()
        {
            return availableSkills;
        }

        /// <summary>
        /// 스킬 데이터로 스킬 시전 (UI 클릭에서 호출)
        /// </summary>
        public bool CastSkillByData(LostSpells.Data.SkillData skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("[PlayerComponent] 스킬이 null입니다.");
                return false;
            }

            // 해당 스킬의 인덱스 찾기
            int skillIndex = -1;
            for (int i = 0; i < availableSkills.Length; i++)
            {
                if (availableSkills[i] != null && availableSkills[i].skillId == skill.skillId)
                {
                    skillIndex = i;
                    break;
                }
            }

            // 스킬이 플레이어 스킬 목록에 없으면 직접 발사
            if (skillIndex < 0)
            {
                // 마나 체크
                if (currentMana < skill.manaCost)
                {
                    Debug.LogWarning($"[PlayerComponent] 마나 부족! 현재: {currentMana}, 필요: {skill.manaCost}");
                    return false;
                }

                // 마나 소모
                currentMana -= (int)skill.manaCost;

                // 투사체 발사
                FireProjectile(skill);
                return true;
            }

            // 스킬이 목록에 있으면 기존 CastSkill 호출 (쿨다운 체크 포함)
            CastSkill(skillIndex);
            return true;
        }

        /// <summary>
        /// 스킬 ID로 스킬 시전
        /// </summary>
        public bool CastSkillById(string skillId)
        {
            for (int i = 0; i < availableSkills.Length; i++)
            {
                if (availableSkills[i] != null && availableSkills[i].skillId == skillId)
                {
                    CastSkill(i);
                    return true;
                }
            }

            Debug.LogWarning($"[PlayerComponent] 스킬을 찾을 수 없음: {skillId}");
            return false;
        }
    }
}
