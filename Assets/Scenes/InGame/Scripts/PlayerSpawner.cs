using UnityEngine;
using LostSpells.Camera;

namespace LostSpells.Systems
{
    /// <summary>
    /// 게임 시작 시 플레이어 프리팹을 생성하는 스크립트
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab; // 플레이어 프리팹
        [SerializeField] private Vector3 spawnPosition = new Vector3(0, -2.595f, 0); // 스폰 위치

        private void Awake()
        {
            // 플레이어 프리팹 생성 (Start보다 먼저 실행되도록 Awake 사용)
            if (playerPrefab != null)
            {
                GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                player.name = "Player"; // 이름 설정
                player.tag = "Player"; // 태그 설정 (프리팹에도 설정되어 있지만 확실하게)

                // 카메라에게 플레이어 알려주기
                CameraFollow cameraFollow = FindFirstObjectByType<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(player.transform);
                }
                else
                {
                    Debug.LogWarning("PlayerSpawner: CameraFollow를 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.LogError("PlayerSpawner: Player Prefab이 설정되지 않았습니다!");
            }
        }
    }
}
