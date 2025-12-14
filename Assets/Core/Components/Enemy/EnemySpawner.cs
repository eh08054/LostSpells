using UnityEngine;
using System.Collections;
using LostSpells.Data;
using LostSpells.Components;

namespace LostSpells.Systems
{
    /// <summary>
    /// 챕터/웨이브 기반 적 스폰 시스템
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Wave Data")]
        [SerializeField] private ChapterWaveData chapterData;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnHeight = 0f;
        [SerializeField] private bool followCamera = true;

        [Header("Wave Progression")]
        [SerializeField] private bool autoProgressWaves = true;
        [SerializeField] private float delayBetweenWaves = 3f;
        [SerializeField] private int maxWaves = 3; // 챕터당 최대 웨이브 수

        [Header("Debug")]
        [SerializeField] private bool autoStartWave = false;
        [SerializeField] private int debugStartWave = 1;

        private int currentWave = 0;
        private bool isSpawning = false;
        private bool isWaitingForEnemiesClear = false;
        private int spawnedEnemiesThisWave = 0;
        private UnityEngine.Camera mainCamera;
        private Vector3 leftSpawnPoint;
        private Vector3 rightSpawnPoint;

        public int CurrentWave => currentWave;
        public bool IsSpawning => isSpawning;

        private void Awake()
        {
            mainCamera = UnityEngine.Camera.main;
            UpdateSpawnPoints();
            EnemyScaleData.Load();
        }

        /// <summary>
        /// 현재 챕터에 맞는 웨이브 데이터 로드
        /// Start()에서 호출하여 GameStateManager가 준비된 후 실행
        /// </summary>
        private void LoadChapterWaveData()
        {
            // 이미 Inspector에서 할당된 경우 사용
            if (chapterData != null)
                return;

            // GameStateManager에서 현재 챕터 ID 가져오기
            int chapterId = 0;
            if (GameStateManager.Instance != null)
            {
                int currentId = GameStateManager.Instance.GetCurrentChapterId();
                Debug.Log($"[EnemySpawner] GameStateManager 챕터 ID: {currentId}");
                if (currentId >= 0)
                {
                    chapterId = currentId;
                }
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] GameStateManager.Instance가 null입니다!");
            }

            // Resources에서 챕터 데이터 로드 시도
            try
            {
                chapterData = Resources.Load<ChapterWaveData>($"WaveData/Chapter{chapterId}WaveData");
                if (chapterData != null)
                {
                    Debug.Log($"[EnemySpawner] 챕터 {chapterId} 웨이브 데이터 로드: {chapterData.chapterName}");
                }
                else
                {
                    // 해당 챕터 데이터가 없으면 Chapter1 시도
                    chapterData = Resources.Load<ChapterWaveData>("WaveData/Chapter1WaveData");
                    if (chapterData != null)
                    {
                        Debug.Log($"[EnemySpawner] 챕터 {chapterId} 데이터 없음, Chapter1 사용: {chapterData.chapterName}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnemySpawner] 챕터 데이터 로드 실패: {e.Message}");
            }

            // 로드 실패 시 에러 로그
            if (chapterData == null)
            {
                Debug.LogError($"[EnemySpawner] 챕터 {chapterId} 웨이브 데이터를 찾을 수 없습니다! Resources/WaveData/Chapter{chapterId}WaveData.asset 파일을 확인하세요.");
            }
        }

        private void Start()
        {
            // 챕터 데이터 로드 (GameStateManager가 준비된 후)
            LoadChapterWaveData();

            if (autoStartWave)
            {
                StartWave(debugStartWave);
            }
        }

        private void Update()
        {
            if (followCamera && mainCamera != null)
            {
                UpdateSpawnPoints();
            }

            // 자동 웨이브 진행: 모든 적이 처치되면 다음 웨이브 시작
            if (autoProgressWaves && isWaitingForEnemiesClear && !isSpawning)
            {
                int aliveEnemies = FindObjectsByType<EnemyComponent>(FindObjectsSortMode.None).Length;
                if (aliveEnemies == 0)
                {
                    isWaitingForEnemiesClear = false;
                    StartCoroutine(StartNextWaveAfterDelay());
                }
            }
        }

        /// <summary>
        /// 딜레이 후 다음 웨이브 시작 또는 스테이지 클리어
        /// </summary>
        private IEnumerator StartNextWaveAfterDelay()
        {
            // 마지막 웨이브 클리어 시 스테이지 클리어
            if (currentWave >= maxWaves)
            {
                // Debug.Log($"[EnemySpawner] Stage Clear! All {maxWaves} waves completed!");
                yield return new WaitForSeconds(1f); // 잠시 대기

                // 스테이지 클리어 UI 표시
                LostSpells.UI.InGameUI inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();
                if (inGameUI != null)
                {
                    inGameUI.ShowStageClear();
                }
                yield break;
            }

            // Debug.Log($"[EnemySpawner] All enemies defeated! Next wave in {delayBetweenWaves}s...");
            yield return new WaitForSeconds(delayBetweenWaves);
            StartNextWave();
        }

        /// <summary>
        /// 스폰 포인트 업데이트
        /// </summary>
        private void UpdateSpawnPoints()
        {
            if (mainCamera == null) return;

            // 오른쪽 스폰 포인트 (화면 밖)
            rightSpawnPoint = mainCamera.ViewportToWorldPoint(new Vector3(1.15f, 0.5f, 10f));
            rightSpawnPoint.y = spawnHeight;
            rightSpawnPoint.z = 0;

            // 왼쪽 스폰 포인트 (화면 밖)
            leftSpawnPoint = mainCamera.ViewportToWorldPoint(new Vector3(-0.15f, 0.5f, 10f));
            leftSpawnPoint.y = spawnHeight;
            leftSpawnPoint.z = 0;
        }

        /// <summary>
        /// 웨이브 시작
        /// </summary>
        public void StartWave(int waveNumber)
        {
            if (isSpawning)
            {
                Debug.LogWarning("[EnemySpawner] Already spawning!");
                return;
            }

            // 챕터 데이터가 없으면 먼저 로드
            if (chapterData == null)
            {
                LoadChapterWaveData();
            }

            // 여전히 null이면 에러
            if (chapterData == null)
            {
                Debug.LogError("[EnemySpawner] No chapter data assigned!");
                return;
            }

            currentWave = waveNumber;

            // GameStateManager 및 UI 업데이트
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetCurrentWave(waveNumber);
            }

            // InGameUI 웨이브 정보 업데이트
            LostSpells.UI.InGameUI inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();
            if (inGameUI != null)
            {
                inGameUI.UpdateWaveInfo();
            }

            StartCoroutine(SpawnWaveCoroutine(waveNumber));
        }

        /// <summary>
        /// 다음 웨이브 시작
        /// </summary>
        public void StartNextWave()
        {
            StartWave(currentWave + 1);
        }

        /// <summary>
        /// 웨이브 스폰 코루틴
        /// </summary>
        private IEnumerator SpawnWaveCoroutine(int waveNumber)
        {
            isSpawning = true;

            WaveInfo waveInfo = chapterData.GetWave(waveNumber);
            if (waveInfo == null)
            {
                Debug.LogError($"[EnemySpawner] Wave {waveNumber} not found!");
                isSpawning = false;
                yield break;
            }

            // Debug.Log($"[EnemySpawner] Starting Wave {waveNumber}");

            // 웨이브 시작 대기
            yield return new WaitForSeconds(waveInfo.startDelay);

            // 각 적 그룹 스폰
            foreach (var enemyInfo in waveInfo.enemies)
            {
                GameObject prefab = enemyInfo.LoadPrefab();
                if (prefab == null)
                {
                    Debug.LogWarning($"[EnemySpawner] Enemy prefab '{enemyInfo.enemyPrefabName}' not found, skipping...");
                    continue;
                }

                yield return StartCoroutine(SpawnEnemyGroup(prefab, enemyInfo, waveNumber));
            }

            isSpawning = false;
            isWaitingForEnemiesClear = true; // 적이 모두 처치될 때까지 대기
            // Debug.Log($"[EnemySpawner] Wave {waveNumber} spawning complete! Waiting for enemies to be defeated...");
        }

        /// <summary>
        /// 적 그룹 스폰
        /// </summary>
        private IEnumerator SpawnEnemyGroup(GameObject prefab, EnemySpawnInfo info, int waveNumber)
        {
            int leftCount = 0;
            int rightCount = 0;

            for (int i = 0; i < info.count; i++)
            {
                Vector3 spawnPos;
                int moveDirection;

                // 스폰 위치 결정
                switch (info.spawnSide)
                {
                    case SpawnSide.Left:
                        spawnPos = leftSpawnPoint;
                        moveDirection = 1; // 오른쪽으로 이동
                        break;

                    case SpawnSide.Right:
                        spawnPos = rightSpawnPoint;
                        moveDirection = -1; // 왼쪽으로 이동
                        break;

                    case SpawnSide.Both:
                    default:
                        // 번갈아가며 스폰
                        if (i % 2 == 0)
                        {
                            spawnPos = rightSpawnPoint;
                            moveDirection = -1;
                            rightCount++;
                        }
                        else
                        {
                            spawnPos = leftSpawnPoint;
                            moveDirection = 1;
                            leftCount++;
                        }
                        break;
                }

                // 적 생성
                SpawnEnemy(prefab, spawnPos, moveDirection, waveNumber, info);

                yield return new WaitForSeconds(info.spawnInterval);
            }
        }

        /// <summary>
        /// 개별 적 스폰
        /// </summary>
        private void SpawnEnemy(GameObject prefab, Vector3 position, int moveDirection, int waveNumber, EnemySpawnInfo info)
        {
            GameObject enemyObj = Instantiate(prefab, position, Quaternion.identity);
            enemyObj.name = $"{prefab.name} (Wave {waveNumber})";

            // Body(스프라이트)만 크기 조절 (UI는 유지)
            Transform body = enemyObj.transform.Find("Body");
            if (body != null)
            {
                // EnemyScaleData에서 크기 가져오기
                float scale = EnemyScaleData.GetScale(prefab.name);
                body.localScale = Vector3.one * scale;
            }

            // EnemyScaleData에서 체력바 높이 가져오기
            float healthBarHeight = EnemyScaleData.GetHealthBarHeight(prefab.name);

            // UI 위치 및 크기 조절 (플레이어와 동일하게)
            // Body 스프라이트가 sortingOrder=100이므로 UI는 그 위에 표시되어야 함
            Transform healthBar = enemyObj.transform.Find("HealthBarBackground");
            if (healthBar != null)
            {
                // 적 스프라이트 위에 보이도록 높이 조절
                healthBar.localPosition = new Vector3(0, healthBarHeight, 0);
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

            Transform nameText = enemyObj.transform.Find("NameText");
            if (nameText != null)
            {
                // 체력바 위에 보이도록 높이 조절 (체력바 + 0.3)
                RectTransform rectTransform = nameText.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, healthBarHeight + 0.3f);
                }

                // SortingOrder를 Body(100)보다 높게 설정
                MeshRenderer meshRenderer = nameText.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingOrder = 110;
                }
            }

            // EnemyComponent 설정
            EnemyComponent enemy = enemyObj.GetComponent<EnemyComponent>();
            if (enemy != null)
            {
                // 웨이브별 스탯 보너스 적용
                int bonusHealth = info.healthBonus * (waveNumber - 1);
                float bonusSpeed = info.speedBonus * (waveNumber - 1);

                enemy.ApplyWaveBonus(bonusHealth, bonusSpeed);
                enemy.SetMoveDirection(moveDirection);

                // 웨이브별 점수 보너스: 웨이브 * 10 (웨이브 1 = 10점, 웨이브 5 = 50점)
                enemy.SetScoreValue(waveNumber * 10);
            }

            // Enemy 레이어 설정
            enemyObj.layer = LayerMask.NameToLayer("Enemy");
        }

        /// <summary>
        /// 챕터 데이터 설정
        /// </summary>
        public void SetChapterData(ChapterWaveData data)
        {
            chapterData = data;
        }

        /// <summary>
        /// 스폰 중지
        /// </summary>
        public void StopSpawning()
        {
            StopAllCoroutines();
            isSpawning = false;
        }

        /// <summary>
        /// Scene 뷰에서 스폰 위치 시각화
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) return;

            // 오른쪽 스폰 위치
            Vector3 rightPos = cam.ViewportToWorldPoint(new Vector3(1.15f, 0.5f, 10f));
            rightPos.y = spawnHeight;
            rightPos.z = 0;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightPos, 0.5f);
            Gizmos.DrawLine(rightPos + Vector3.up, rightPos + Vector3.down);

            // 왼쪽 스폰 위치
            Vector3 leftPos = cam.ViewportToWorldPoint(new Vector3(-0.15f, 0.5f, 10f));
            leftPos.y = spawnHeight;
            leftPos.z = 0;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leftPos, 0.5f);
            Gizmos.DrawLine(leftPos + Vector3.up, leftPos + Vector3.down);
        }
    }
}
