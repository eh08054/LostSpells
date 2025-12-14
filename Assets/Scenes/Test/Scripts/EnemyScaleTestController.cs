using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LostSpells.Data;

namespace LostSpells.Test
{
    /// <summary>
    /// 적 크기 테스트용 컨트롤러
    /// 인스펙터에서 적 종류를 변경하고 크기를 조절할 수 있음
    /// EnemySpawner와 동일한 방식으로 적 설정
    /// </summary>
    public class EnemyScaleTestController : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [SerializeField] private int currentEnemyIndex = 0;
        [SerializeField] private float currentScale = 1.0f;
        [SerializeField] private float currentHealthBarHeight = 1.5f;

        [Header("Spawn Position")]
        [SerializeField] private Vector3 enemySpawnPosition = new Vector3(3f, -2.5f, 0f);

        [Header("References")]
        [SerializeField] private Transform playerPosition;

        [Header("Display Info (Read Only)")]
        [SerializeField] private string currentEnemyName = "";
        [SerializeField] private int totalEnemyCount = 0;

        private List<string> enemyPrefabNames = new List<string>();
        private GameObject currentEnemy;
        private Transform enemyBody;
        private Transform healthBar;
        private Transform nameText;

        private void Start()
        {
            LoadEnemyList();
            EnemyScaleData.Load();

            if (enemyPrefabNames.Count > 0)
            {
                SpawnCurrentEnemy();
            }
        }

        private void LoadEnemyList()
        {
            // Resources/Enemies 폴더에서 모든 적 프리팹 로드
            GameObject[] enemies = Resources.LoadAll<GameObject>("Enemies");

            // 종류별로 그룹화하여 정렬 (Bear, Wolf, Dragon 등)
            enemyPrefabNames = enemies
                .Select(e => e.name)
                .OrderBy(n => GetEnemyType(n))
                .ThenBy(n => n)
                .ToList();

            totalEnemyCount = enemyPrefabNames.Count;
            Debug.Log($"[EnemyScaleTest] {totalEnemyCount}개 적 프리팹 로드됨");
        }

        /// <summary>
        /// 적 이름에서 기본 종류 추출 (예: BlackBearEnemy -> Bear)
        /// </summary>
        private string GetEnemyType(string enemyName)
        {
            // "Enemy" 접미사 제거
            string name = enemyName.Replace("Enemy", "");

            // 색상/설명 접두사 목록
            string[] prefixes = {
                // 색상
                "Black", "Blue", "Brown", "Green", "Grey", "Purple", "Red", "Yellow", "Golden", "Pink",
                // 설명
                "Abyss", "Hell", "Fire", "Ice", "Flame", "Earth", "Royal", "Flying", "Death", "Dark", "Bloody", "Mega"
            };

            // 접두사 제거
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix))
                {
                    name = name.Substring(prefix.Length);
                    break;
                }
            }

            // 숫자 접미사 제거 (Gorgon1 -> Gorgon)
            name = Regex.Replace(name, @"\d+$", "");

            return name;
        }

        /// <summary>
        /// 현재 인덱스의 적 스폰 (EnemySpawner와 동일한 방식)
        /// </summary>
        public void SpawnCurrentEnemy()
        {
            if (currentEnemy != null)
            {
                Destroy(currentEnemy);
            }

            if (currentEnemyIndex < 0 || currentEnemyIndex >= enemyPrefabNames.Count)
            {
                Debug.LogWarning("[EnemyScaleTest] 유효하지 않은 인덱스");
                return;
            }

            string enemyName = enemyPrefabNames[currentEnemyIndex];
            currentEnemyName = enemyName;

            GameObject prefab = Resources.Load<GameObject>($"Enemies/{enemyName}");
            if (prefab == null)
            {
                Debug.LogError($"[EnemyScaleTest] 프리팹 로드 실패: {enemyName}");
                return;
            }

            currentEnemy = Instantiate(prefab, enemySpawnPosition, Quaternion.identity);
            currentEnemy.name = $"[TEST] {enemyName}";

            // Enemy 레이어 설정
            currentEnemy.layer = LayerMask.NameToLayer("Enemy");

            // ========== EnemySpawner와 동일한 설정 ==========

            // Body(스프라이트)만 크기 조절 (UI는 유지)
            enemyBody = currentEnemy.transform.Find("Body");
            if (enemyBody != null)
            {
                // 저장된 스케일 적용
                currentScale = EnemyScaleData.GetScale(enemyName);
                enemyBody.localScale = Vector3.one * currentScale;
            }

            // UI 위치 및 크기 조절 (EnemySpawner와 동일)
            healthBar = currentEnemy.transform.Find("HealthBarBackground");
            if (healthBar != null)
            {
                // 저장된 체력바 높이 적용
                currentHealthBarHeight = EnemyScaleData.GetHealthBarHeight(enemyName);
                healthBar.localPosition = new Vector3(0, currentHealthBarHeight, 0);
                healthBar.localScale = new Vector3(1f, 0.2f, 1f);

                // SortingOrder를 Body(100)보다 높게 설정
                SpriteRenderer healthBarSprite = healthBar.GetComponent<SpriteRenderer>();
                if (healthBarSprite != null)
                {
                    healthBarSprite.sortingOrder = 110;
                }

                // HealthBarFill도 설정
                Transform healthBarFill = healthBar.Find("HealthBarFill");
                if (healthBarFill != null)
                {
                    SpriteRenderer fillSprite = healthBarFill.GetComponent<SpriteRenderer>();
                    if (fillSprite != null)
                    {
                        fillSprite.sortingOrder = 115;
                    }
                }
            }

            nameText = currentEnemy.transform.Find("NameText");
            if (nameText != null)
            {
                // 체력바 위에 보이도록 높이 조절 (체력바 + 0.3)
                RectTransform rectTransform = nameText.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, currentHealthBarHeight + 0.3f);
                }

                // SortingOrder를 Body(100)보다 높게 설정
                MeshRenderer meshRenderer = nameText.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingOrder = 110;
                }
            }

            // EnemyComponent 비활성화 (이동/공격 방지)
            var enemyComponent = currentEnemy.GetComponent<LostSpells.Components.EnemyComponent>();
            if (enemyComponent != null)
            {
                enemyComponent.enabled = false;
            }

            // Rigidbody2D 설정 (중력 방지, 위치 고정)
            var rb = currentEnemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0;
            }

            Debug.Log($"[EnemyScaleTest] 스폰: {enemyName} (Scale: {currentScale})");
        }

        /// <summary>
        /// 현재 스케일 적용
        /// </summary>
        public void ApplyScale()
        {
            if (enemyBody != null)
            {
                enemyBody.localScale = Vector3.one * currentScale;
            }
        }

        /// <summary>
        /// 현재 체력바 높이 적용
        /// </summary>
        public void ApplyHealthBarHeight()
        {
            if (healthBar != null)
            {
                healthBar.localPosition = new Vector3(0, currentHealthBarHeight, 0);
            }
            if (nameText != null)
            {
                RectTransform rectTransform = nameText.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, currentHealthBarHeight + 0.3f);
                }
            }
        }

        /// <summary>
        /// 다음 적으로 변경
        /// </summary>
        public void NextEnemy()
        {
            currentEnemyIndex = (currentEnemyIndex + 1) % enemyPrefabNames.Count;
            SpawnCurrentEnemy();
        }

        /// <summary>
        /// 이전 적으로 변경
        /// </summary>
        public void PreviousEnemy()
        {
            currentEnemyIndex--;
            if (currentEnemyIndex < 0) currentEnemyIndex = enemyPrefabNames.Count - 1;
            SpawnCurrentEnemy();
        }

        /// <summary>
        /// 현재 스케일 저장
        /// </summary>
        public void SaveCurrentScale()
        {
            if (string.IsNullOrEmpty(currentEnemyName)) return;

            EnemyScaleData.SetScale(currentEnemyName, currentScale);
            Debug.Log($"[EnemyScaleTest] 스케일 설정: {currentEnemyName} = {currentScale}");
        }

        /// <summary>
        /// 모든 스케일 데이터 파일로 저장
        /// </summary>
        public void SaveAllToFile()
        {
#if UNITY_EDITOR
            SaveCurrentScale();
            EnemyScaleData.Save();
#else
            Debug.LogWarning("[EnemyScaleTest] 에디터에서만 저장 가능");
#endif
        }

        /// <summary>
        /// 스케일 값 변경 시 호출 (즉시 저장)
        /// </summary>
        public void OnScaleChanged(float newScale)
        {
            currentScale = newScale;
            ApplyScale();

            // 즉시 저장
            SaveAndUpdateFile();
        }

        /// <summary>
        /// 현재 스케일을 즉시 파일에 저장
        /// </summary>
        private void SaveAndUpdateFile()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(currentEnemyName)) return;

            EnemyScaleData.SetScale(currentEnemyName, currentScale);
            EnemyScaleData.SetHealthBarHeight(currentEnemyName, currentHealthBarHeight);
            EnemyScaleData.Save();
            Debug.Log($"[EnemyScaleTest] 저장됨: {currentEnemyName} (Scale: {currentScale}, HealthBar: {currentHealthBarHeight})");
#endif
        }

        /// <summary>
        /// 체력바 높이 변경 시 호출 (즉시 저장)
        /// </summary>
        public void OnHealthBarHeightChanged(float newHeight)
        {
            currentHealthBarHeight = newHeight;
            ApplyHealthBarHeight();

            // 즉시 저장
            SaveAndUpdateFile();
        }

        /// <summary>
        /// 체력바 높이 증가
        /// </summary>
        public void IncreaseHealthBarHeight(float amount = 0.1f)
        {
            OnHealthBarHeightChanged(currentHealthBarHeight + amount);
        }

        /// <summary>
        /// 체력바 높이 감소
        /// </summary>
        public void DecreaseHealthBarHeight(float amount = 0.1f)
        {
            OnHealthBarHeightChanged(Mathf.Max(0.1f, currentHealthBarHeight - amount));
        }

        /// <summary>
        /// 스케일 증가
        /// </summary>
        public void IncreaseScale(float amount = 0.1f)
        {
            OnScaleChanged(currentScale + amount);
        }

        /// <summary>
        /// 스케일 감소
        /// </summary>
        public void DecreaseScale(float amount = 0.1f)
        {
            OnScaleChanged(Mathf.Max(0.1f, currentScale - amount));
        }

        // Inspector에서 값 변경 시 자동 적용
        private void OnValidate()
        {
            if (Application.isPlaying && enemyBody != null)
            {
                ApplyScale();
            }
        }
    }
}
