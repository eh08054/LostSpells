using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace LostSpells.Components
{

    public class PlayerComponent : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private string playerName = "Wizard";
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;
        [SerializeField] private int maxMana = 80;
        [SerializeField] private int currentMana;
        [SerializeField] private float manaRegenRate = 5f;
        [SerializeField] private float healthRegenRate = 5f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float knockbackForce = 3f;

        [Header("Skill System")]
        [SerializeField] private Transform skillCastPoint;
        [SerializeField] private LostSpells.Data.SkillData[] availableSkills = new LostSpells.Data.SkillData[6];
        [SerializeField] private GameObject skillProjectilePrefab;

        private float[] skillCooldowns = new float[6];

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Animator animator;

        [Header("UI Elements")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Transform healthBarBackground;
        [SerializeField] private Transform healthBarFill;

        private Rigidbody2D rb;
        private Collider2D playerCollider;
        private bool isKnockedBack = false;
        private bool isGrounded = false;
        private float manaRegenAccumulator = 0f;
        private float healthRegenAccumulator = 0f;

        private float currentHorizontalInput = 0f;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

            rb.gravityScale = 3;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            playerCollider = GetComponent<CircleCollider2D>();
            if (playerCollider == null)
            {
                CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.radius = 0.4f;
                playerCollider = circleCollider;
            }

            currentHealth = maxHealth;
            currentMana = maxMana;
        }

        private void Start()
        {
            if (nameText != null) nameText.text = playerName;
            UpdateHealthBar();
            InitializeDefaultSkills();
            if (skillProjectilePrefab == null) CreateProjectilePrefab();
        }

        private void Update()
        {
            if (rb == null) return;

            RegenerateMana();
            RegenerateHealth();
            UpdateSkillCooldowns();
            HandleSkillInput();

            if (isKnockedBack) return;

            Key moveLeftKey = GetMoveLeftKey();
            Key moveRightKey = GetMoveRightKey();
            Key jumpKey = GetJumpKey();

            currentHorizontalInput = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current[moveLeftKey].isPressed)
                {
                    currentHorizontalInput = -1f;
                }
                else if (Keyboard.current[moveRightKey].isPressed)
                {
                    currentHorizontalInput = 1f;
                }

                if (Keyboard.current[jumpKey].wasPressedThisFrame && isGrounded)
                {
                    Vector2 velocity = rb.linearVelocity;
                    velocity.y = jumpForce;
                    rb.linearVelocity = velocity;
                }
            }

            if (currentHorizontalInput < 0f) spriteRenderer.flipX = true;
            else if (currentHorizontalInput > 0f) spriteRenderer.flipX = false;

            if (animator != null)
            {
                animator.SetFloat("Speed", Mathf.Abs(currentHorizontalInput));
            }
        }

        private void FixedUpdate()
        {
            if (isKnockedBack || rb == null) return;

            Vector2 velocity = rb.linearVelocity;
            velocity.x = currentHorizontalInput * moveSpeed;
            rb.linearVelocity = velocity;
        }


        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            UpdateHealthBar();
            if (currentHealth <= 0) Die();
        }

        public void TakeDamage(int damage, Vector2 knockbackDirection)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            UpdateHealthBar();

            if (rb != null && !isKnockedBack)
            {
                rb.linearVelocity = knockbackDirection.normalized * knockbackForce;
                isKnockedBack = true;
                IgnoreAllEnemyCollisions(true);
            }

            if (currentHealth <= 0) Die();
        }

        private void Die()
        {
            if (animator != null) animator.SetBool("IsDead", true);
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0;
            }
            foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
            enabled = false;

            var inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();
            if (inGameUI != null) inGameUI.ShowGameOver();
        }

        public void Heal(int amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHealthBar();
        }

        public void Revive()
        {
            currentHealth = maxHealth;
            UpdateHealthBar();
            if (animator != null)
            {
                animator.SetBool("IsDead", false);
                animator.SetFloat("Speed", 0f);
                animator.Play("Idle", 0, 0f);
            }
            if (rb != null)
            {
                rb.gravityScale = 3;
                rb.linearVelocity = Vector2.zero;
            }
            foreach (var col in GetComponents<Collider2D>()) col.enabled = true;
            isKnockedBack = false;
            enabled = true;
        }

        private void RegenerateMana()
        {
            if (currentMana < maxMana)
            {
                manaRegenAccumulator += manaRegenRate * Time.deltaTime;
                if (manaRegenAccumulator >= 1f)
                {
                    int manaToAdd = Mathf.FloorToInt(manaRegenAccumulator);
                    currentMana += manaToAdd;
                    currentMana = Mathf.Min(currentMana, maxMana);
                    manaRegenAccumulator -= manaToAdd;
                }
            }
        }

        private void RegenerateHealth()
        {
            if (currentHealth < maxHealth)
            {
                healthRegenAccumulator += healthRegenRate * Time.deltaTime;
                if (healthRegenAccumulator >= 1f)
                {
                    int healthToAdd = Mathf.FloorToInt(healthRegenAccumulator);
                    currentHealth += healthToAdd;
                    currentHealth = Mathf.Min(currentHealth, maxHealth);
                    healthRegenAccumulator -= healthToAdd;
                    UpdateHealthBar();
                }
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBarFill != null)
            {
                float healthPercent = (float)currentHealth / maxHealth;
                Vector3 scale = healthBarFill.localScale;
                scale.x = healthPercent;
                healthBarFill.localScale = scale;
                Vector3 pos = healthBarFill.localPosition;
                pos.x = (healthPercent - 1f) / 2f;
                healthBarFill.localPosition = pos;
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (nameText != null) nameText.text = playerName;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthBar();
#endif
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            EnemyComponent enemy = collision.gameObject.GetComponent<EnemyComponent>();
            if (enemy != null && rb != null)
            {
                if (isKnockedBack) return;
                float dirX = Mathf.Sign(transform.position.x - collision.transform.position.x);
                rb.linearVelocity = new Vector2(dirX * knockbackForce, knockbackForce);
                isKnockedBack = true;
                IgnoreAllEnemyCollisions(true);
                TakeDamage(10);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;
                    if (isKnockedBack && Mathf.Abs(rb.linearVelocity.y) < 0.5f)
                    {
                        isKnockedBack = false;
                        IgnoreAllEnemyCollisions(false);
                    }
                    break;
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            isGrounded = false;
        }

        private void IgnoreAllEnemyCollisions(bool ignore)
        {
            if (playerCollider == null) return;
            EnemyComponent[] enemies = FindObjectsByType<EnemyComponent>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                if (enemyCollider != null) Physics2D.IgnoreCollision(playerCollider, enemyCollider, ignore);
            }
        }

        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public int GetCurrentMana() => currentMana;
        public int GetMaxMana() => maxMana;

        public void SetPlayerName(string name)
        {
            playerName = name;
            if (nameText != null) nameText.text = name;
        }

        public string GetPlayerName() => playerName;

        public bool CastSkill(LostSpells.Data.SkillData skillData)
        {
            if (currentMana < skillData.manaCost) return false;
            GameObject skillPrefab = LoadSkillPrefab(skillData.effectPrefabPath);
            if (skillPrefab == null) return false;

            Vector3 castPosition = GetSkillCastPosition();
            Quaternion castRotation = GetSkillCastRotation();
            GameObject skillInstance = Instantiate(skillPrefab, castPosition, castRotation);

            var skillScript = skillInstance.GetComponent<SkillBehavior>();
            if (skillScript != null) skillScript.Initialize(skillData, this);

            currentMana -= (int)skillData.manaCost;
            UpdateHealthBar();
            return true;
        }

        private Vector3 GetSkillCastPosition()
        {
            if (skillCastPoint != null) return skillCastPoint.position;
            Vector3 offset = spriteRenderer.flipX ? Vector3.left * 0.5f : Vector3.right * 0.5f;
            return transform.position + offset + Vector3.up * 0.3f;
        }

        private Quaternion GetSkillCastRotation()
        {
            return spriteRenderer.flipX ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        }

        private GameObject LoadSkillPrefab(string prefabPath)
        {
            return string.IsNullOrEmpty(prefabPath) ? null : Resources.Load<GameObject>(prefabPath);
        }

        public Transform GetSkillCastPoint() => skillCastPoint;

        private Key GetMoveLeftKey()
        {
            var saveData = LostSpells.Data.SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("MoveLeft"))
                return ParseKey(saveData.keyBindings["MoveLeft"], Key.A);
            return Key.A;
        }

        private Key GetMoveRightKey()
        {
            var saveData = LostSpells.Data.SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("MoveRight"))
                return ParseKey(saveData.keyBindings["MoveRight"], Key.D);
            return Key.D;
        }

        private Key GetJumpKey()
        {
            var saveData = LostSpells.Data.SaveManager.Instance?.GetCurrentSaveData();
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("Jump"))
                return ParseKey(saveData.keyBindings["Jump"], Key.W);
            return Key.W;
        }

        private Key ParseKey(string keyString, Key defaultKey)
        {
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
                    if (System.Enum.TryParse<Key>(keyString, true, out Key key)) return key;
                    return defaultKey;
            }
        }

        private void InitializeDefaultSkills()
        {
            bool hasSkills = false;
            for (int i = 0; i < availableSkills.Length; i++) { if (availableSkills[i] != null) { hasSkills = true; break; } }
            if (hasSkills) return;

            availableSkills[0] = CreateSkill("fireball", "Fireball", "화염구", LostSpells.Data.SkillType.Fireball, "Fire/FireBall", 15, 2f, 20, 12f);
            availableSkills[1] = CreateSkill("icespike", "Ice Spike", "얼음 가시", LostSpells.Data.SkillType.IceSpike, "Ice/IceSpike", 12, 1.8f, 15, 10f);
            availableSkills[2] = CreateSkill("lightning", "Thunder Strike", "번개", LostSpells.Data.SkillType.Lightning, "Electricity/ElectricLightning01", 18, 2.5f, 25, 15f);
            availableSkills[3] = CreateSkill("earthrock", "Stone Bullet", "돌 탄환", LostSpells.Data.SkillType.EarthRock, "Earth/EarthRock", 10, 1.5f, 12, 8f);
            availableSkills[4] = CreateSkill("holylight", "Divine Ray", "신성한 광선", LostSpells.Data.SkillType.HolyLight, "Holy/HolyProjectile", 20, 3f, 30, 14f);
            availableSkills[5] = CreateSkill("voidblast", "Void Orb", "암흑 구체", LostSpells.Data.SkillType.VoidBlast, "Void/VoidBall", 25, 3.5f, 35, 10f);
        }

        private LostSpells.Data.SkillData CreateSkill(string id, string nameEn, string nameKo, LostSpells.Data.SkillType type, string vfxPath, int manaCost, float cooldown, int damage, float speed)
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

        private void CreateProjectilePrefab()
        {
            GameObject projectile = new GameObject("SkillProjectile");
            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f; collider.isTrigger = true;
            Rigidbody2D rb2d = projectile.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Kinematic; rb2d.gravityScale = 0f;
            projectile.AddComponent<SkillProjectile>();
            skillProjectilePrefab = projectile;
        }

        private void UpdateSkillCooldowns()
        {
            for (int i = 0; i < skillCooldowns.Length; i++)
            {
                if (skillCooldowns[i] > 0)
                {
                    skillCooldowns[i] -= Time.deltaTime;
                    if (skillCooldowns[i] < 0) skillCooldowns[i] = 0;
                }
            }
        }

        private void HandleSkillInput()
        {
            if (Keyboard.current == null) return;
            for (int i = 0; i < 6; i++)
            {
                Key skillKey = Key.Digit1 + i;
                if (Keyboard.current[skillKey].wasPressedThisFrame) CastSkill(i);
            }
        }

        private void CastSkill(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= availableSkills.Length) return;
            LostSpells.Data.SkillData skill = availableSkills[skillIndex];
            if (skill == null || skillCooldowns[skillIndex] > 0 || currentMana < skill.manaCost) return;

            currentMana -= (int)skill.manaCost;
            skillCooldowns[skillIndex] = skill.cooldown;
            FireProjectile(skill);
        }

        private void FireProjectile(LostSpells.Data.SkillData skill)
        {
            Vector3 spawnPosition = skillCastPoint != null ? skillCastPoint.position : transform.position;
            Vector3 direction = spriteRenderer.flipX ? Vector3.left : Vector3.right;
            GameObject vfxPrefab = null;
            if (!string.IsNullOrEmpty(skill.effectPrefabPath)) vfxPrefab = Resources.Load<GameObject>(skill.effectPrefabPath);

            if (skillProjectilePrefab != null)
            {
                GameObject projectile = Instantiate(skillProjectilePrefab, spawnPosition, Quaternion.identity);
                SkillProjectile projectileScript = projectile.GetComponent<SkillProjectile>();
                if (projectileScript == null) projectileScript = projectile.AddComponent<SkillProjectile>();

                projectileScript.Initialize((int)skill.damage, skill.projectileSpeed, skill.projectileLifetime, direction, vfxPrefab);
                if (vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(vfxPrefab, projectile.transform);
                    vfx.transform.localPosition = Vector3.zero;
                }
            }
        }

        public float GetSkillCooldown(int skillIndex) => (skillIndex < 0 || skillIndex >= skillCooldowns.Length) ? 0f : skillCooldowns[skillIndex];
        public LostSpells.Data.SkillData GetSkill(int skillIndex) => (skillIndex < 0 || skillIndex >= availableSkills.Length) ? null : availableSkills[skillIndex];
    }
}