using UnityEngine;
using System.Collections;
using LostSpells.Components;

namespace LostSpells.Systems
{
    /// <summary>
    /// 웨이브별 적 생성 시스템
    /// 화면 오른쪽에서 적을 생성합니다
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Prefab")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnHeight = 0f; // 스폰 높이 (Y 좌표)
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int enemiesPerWave = 5;

        [Header("Chapter Monster Data")]
        [SerializeField] private LostSpells.Data.ChapterMonsterData[] chapterMonsters; // 챕터별 몬스터 데이터 (1~8)
        [SerializeField] private bool useChapterMonster = true; // 챕터별 몬스터 사용 여부

        [Header("Manual Monster Settings (useChapterMonster가 false일 때)")]
        [SerializeField] private Sprite manualMonsterSprite;
        [SerializeField] private int baseHealth = 50;
        [SerializeField] private float baseSpeed = 2f;

        [Header("Dynamic Spawn")]
        [SerializeField] private bool followCamera = true; // 카메라를 따라 스폰 위치 이동

        private int currentWave = 1;
        private bool isSpawning = false;
        private UnityEngine.Camera mainCamera;

        private void Awake()
        {
            // 메인 카메라 찾기
            mainCamera = UnityEngine.Camera.main;

            // 스폰 포인트가 없으면 화면 오른쪽에 생성 - Awake에서 실행하여 다른 스크립트의 Start보다 먼저 초기화
            if (spawnPoint == null)
            {
                GameObject spawnObj = new GameObject("SpawnPoint");
                spawnPoint = spawnObj.transform;
                spawnPoint.parent = transform;

                UpdateSpawnPointPosition();
            }
        }

        private void Update()
        {
            // 카메라를 따라 스폰 위치 업데이트
            if (followCamera && mainCamera != null)
            {
                UpdateSpawnPointPosition();
            }
        }

        /// <summary>
        /// 스폰 포인트 위치 업데이트
        /// </summary>
        private void UpdateSpawnPointPosition()
        {
            if (spawnPoint == null || mainCamera == null)
                return;

            // 화면 오른쪽 끝으로 설정
            Vector3 rightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1.1f, 0.5f, 10f));
            rightEdge.y = spawnHeight; // 사용자가 설정한 높이 사용
            rightEdge.z = 0; // Z 위치를 0으로 고정
            spawnPoint.position = rightEdge;
        }

        /// <summary>
        /// 웨이브 시작
        /// </summary>
        public void StartWave(int waveNumber)
        {
            currentWave = waveNumber;

            if (!isSpawning)
            {
                StartCoroutine(SpawnWave());
            }
        }

        /// <summary>
        /// 웨이브의 적들을 생성
        /// </summary>
        private IEnumerator SpawnWave()
        {
            isSpawning = true;

            int enemiesToSpawn = enemiesPerWave + (currentWave - 1) * 2; // 웨이브마다 2마리씩 증가

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }

            isSpawning = false;
        }

        /// <summary>
        /// 적 하나 생성
        /// </summary>
        private void SpawnEnemy()
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("Enemy Prefab이 설정되지 않았습니다!");
                return;
            }

            // 적 생성
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            enemyObj.name = $"Enemy (Wave {currentWave})";

            // 적 컴포넌트 설정
            EnemyComponent enemy = enemyObj.GetComponent<EnemyComponent>();
            if (enemy != null)
            {
                // 몬스터 데이터 가져오기
                Sprite monsterSprite = null;
                string monsterName = "Enemy";
                int health = baseHealth;
                float speed = baseSpeed;

                // 챕터별 몬스터 사용
                if (useChapterMonster && chapterMonsters != null && chapterMonsters.Length > 0)
                {
                    // GameStateManager에서 현재 챕터 ID 가져오기 (없으면 1로 기본값)
                    int currentChapterId = 1;

                    // GameStateManager가 있으면 현재 챕터 가져오기
                    if (LostSpells.Systems.GameStateManager.Instance != null)
                    {
                        currentChapterId = LostSpells.Systems.GameStateManager.Instance.GetCurrentChapterId();
                    }

                    // 현재 챕터에 맞는 몬스터 데이터 찾기
                    LostSpells.Data.ChapterMonsterData monsterData = null;
                    foreach (var data in chapterMonsters)
                    {
                        if (data != null && data.chapterId == currentChapterId)
                        {
                            monsterData = data;
                            break;
                        }
                    }

                    // 몬스터 데이터 적용
                    if (monsterData != null)
                    {
                        monsterSprite = monsterData.monsterSprite;
                        monsterName = monsterData.monsterName;
                        health = monsterData.baseHealth;
                        speed = monsterData.baseSpeed;
                    }
                    else
                    {
                        Debug.LogWarning($"챕터 {currentChapterId}에 대한 몬스터 데이터를 찾을 수 없습니다. 수동 설정을 사용합니다.");
                        monsterSprite = manualMonsterSprite;
                    }
                }
                else
                {
                    // 수동 몬스터 설정 사용
                    monsterSprite = manualMonsterSprite;
                }

                // 웨이브가 높아질수록 적 능력치 증가
                health += (currentWave - 1) * 10;
                speed += (currentWave - 1) * 0.2f;

                // 적 초기화 (스프라이트 포함)
                enemy.Initialize(monsterName, health, speed, monsterSprite);
            }
        }

        /// <summary>
        /// 적 Prefab 설정
        /// </summary>
        public void SetEnemyPrefab(GameObject prefab)
        {
            enemyPrefab = prefab;
        }

        public bool IsSpawning() => isSpawning;

        /// <summary>
        /// Scene 뷰에서 스폰 위치 시각화 (에디터 전용)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 스폰 위치 계산
            Vector3 spawnPosition = Vector3.zero;

            if (spawnPoint != null)
            {
                // SpawnPoint가 있으면 그 위치 사용
                spawnPosition = spawnPoint.position;
            }
            else
            {
                // SpawnPoint가 없으면 예상 위치 계산
                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    spawnPosition = mainCam.ViewportToWorldPoint(new Vector3(1.1f, 0.5f, 10f));
                    spawnPosition.y = spawnHeight;
                }
            }

            // Gizmo 그리기
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnPosition, 0.5f); // 스폰 위치에 빨간 원

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnPosition + Vector3.up * 1f, spawnPosition + Vector3.down * 1f); // 세로선
            Gizmos.DrawLine(spawnPosition + Vector3.left * 0.5f, spawnPosition + Vector3.right * 0.5f); // 가로선
        }

        /// <summary>
        /// Inspector에서 값이 변경될 때 호출
        /// </summary>
        private void OnValidate()
        {
            #if UNITY_EDITOR
            // 에디터에서 spawnHeight 변경 시 SpawnPoint 위치 업데이트
            if (Application.isPlaying && spawnPoint != null)
            {
                UpdateSpawnPointPosition();
            }
            #endif
        }
    }
}
