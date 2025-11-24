using UnityEngine;

namespace LostSpells.World
{
    /// <summary>
    /// 월드 랩핑 시스템 (무한 스크롤링용)
    /// 캐릭터가 카메라 시야를 벗어나면 반대편으로 순간이동
    /// </summary>
    public class WorldWrapper : MonoBehaviour
    {
        [Header("Wrapping Settings")]
        [SerializeField] private float wrapDistanceFromCamera = 15f; // 카메라로부터 몇 유닛 떨어지면 랩핑할지
        [SerializeField] private bool enableWrapping = true; // 월드 랩핑 활성화

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showGizmos = true;

        private UnityEngine.Camera mainCamera;

        void Start()
        {
            mainCamera = UnityEngine.Camera.main;
        }

        void LateUpdate()
        {
            if (!enableWrapping)
                return;

            WrapAllObjects();
        }

        private void WrapAllObjects()
        {
            // 플레이어 랩핑
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                WrapObject(player.transform);
            }

            // 적 랩핑
            LostSpells.Components.EnemyComponent[] enemies = FindObjectsByType<LostSpells.Components.EnemyComponent>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    WrapObject(enemy.transform);
                }
            }

            // 스킬 프로젝타일 랩핑 (태그로 찾기 - 태그가 없으면 무시)
            try
            {
                GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");
                foreach (var projectile in projectiles)
                {
                    if (projectile != null)
                    {
                        WrapObject(projectile.transform);
                    }
                }
            }
            catch (UnityException)
            {
                // Projectile 태그가 정의되지 않았으면 무시
            }
        }

        private void WrapObject(Transform obj)
        {
            if (obj == null || mainCamera == null)
                return;

            Vector3 pos = obj.position;
            float cameraX = mainCamera.transform.position.x;

            // 카메라 기준으로 오른쪽으로 너무 멀리 가면 왼쪽으로 이동
            if (pos.x > cameraX + wrapDistanceFromCamera)
            {
                pos.x = cameraX - wrapDistanceFromCamera;
                obj.position = pos;

                if (showDebugInfo)
                {
                    Debug.Log($"{obj.name} wrapped: right -> left (x={pos.x})");
                }
            }
            // 카메라 기준으로 왼쪽으로 너무 멀리 가면 오른쪽으로 이동
            else if (pos.x < cameraX - wrapDistanceFromCamera)
            {
                pos.x = cameraX + wrapDistanceFromCamera;
                obj.position = pos;

                if (showDebugInfo)
                {
                    Debug.Log($"{obj.name} wrapped: left -> right (x={pos.x})");
                }
            }
        }

        /// <summary>
        /// 랩핑 거리 설정
        /// </summary>
        public void SetWrapDistance(float distance)
        {
            wrapDistanceFromCamera = distance;
        }

        /// <summary>
        /// 월드 랩핑 활성화/비활성화
        /// </summary>
        public void SetWrappingEnabled(bool enabled)
        {
            enableWrapping = enabled;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!enableWrapping || !showGizmos)
                return;

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            float cameraX = cam.transform.position.x;

            // 랩핑 경계선 표시
            Gizmos.color = Color.cyan;

            // 왼쪽 경계
            Vector3 leftStart = new Vector3(cameraX - wrapDistanceFromCamera, -10, 0);
            Vector3 leftEnd = new Vector3(cameraX - wrapDistanceFromCamera, 10, 0);
            Gizmos.DrawLine(leftStart, leftEnd);

            // 오른쪽 경계
            Vector3 rightStart = new Vector3(cameraX + wrapDistanceFromCamera, -10, 0);
            Vector3 rightEnd = new Vector3(cameraX + wrapDistanceFromCamera, 10, 0);
            Gizmos.DrawLine(rightStart, rightEnd);

            // 텍스트 표시 (경계 위치)
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(leftStart + Vector3.up * 12, $"Wrap Left ({cameraX - wrapDistanceFromCamera:F1})");
            UnityEditor.Handles.Label(rightStart + Vector3.up * 12, $"Wrap Right ({cameraX + wrapDistanceFromCamera:F1})");
        }
#endif
    }
}
