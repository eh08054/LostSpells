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

        [Header("Enemy Stats")]
        [SerializeField] private int baseHealth = 50;
        [SerializeField] private float baseSpeed = 2f;

        private int currentWave = 1;
        private bool isSpawning = false;

        private void Awake()
        {
            // 스폰 포인트가 없으면 화면 오른쪽에 생성 - Awake에서 실행하여 다른 스크립트의 Start보다 먼저 초기화
            if (spawnPoint == null)
            {
                GameObject spawnObj = new GameObject("SpawnPoint");
                spawnPoint = spawnObj.transform;
                spawnPoint.parent = transform;

                UpdateSpawnPointPosition();
            }
        }

        /// <summary>
        /// 스폰 포인트 위치 업데이트
        /// </summary>
        private void UpdateSpawnPointPosition()
        {
            if (spawnPoint == null) return;

            // 화면 오른쪽 끝으로 설정
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 rightEdge = mainCam.ViewportToWorldPoint(new Vector3(1.1f, 0.5f, 10f));
                rightEdge.y = spawnHeight; // 사용자가 설정한 높이 사용
                spawnPoint.position = rightEdge;
            }
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
                // 웨이브가 높아질수록 적 능력치 증가
                int health = baseHealth + (currentWave - 1) * 10;
                float speed = baseSpeed + (currentWave - 1) * 0.2f;
                string name = "Enemy"; // 레벨 없이 단순한 이름만

                enemy.Initialize(name, health, speed);
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
                Camera mainCam = Camera.main;
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
