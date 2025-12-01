using UnityEngine;

namespace LostSpells.Camera
{
    /// <summary>
    /// 카메라 컨트롤러
    /// - 플레이어를 부드럽게 따라감
    /// - 데드존 설정으로 자연스러운 카메라 움직임
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target; // 따라갈 대상 (플레이어)
        [SerializeField] private float smoothSpeed = 5f; // 카메라 이동 속도 (높을수록 빠름)
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // 카메라 오프셋 (Y=0으로 배경 전체 표시)
        [SerializeField] private bool followY = false; // Y축 추적 (false면 X축만 추적)

        [Header("Dead Zone (화면 중앙 영역)")]
        [SerializeField] private float deadZoneWidth = 3f; // 데드존 너비
        [SerializeField] private float deadZoneHeight = 2f; // 데드존 높이 (followY가 false면 사용 안함)
        [SerializeField] private bool showDeadZone = false; // 데드존 표시 (디버그용)

        [Header("Camera Bounds (카메라 이동 제한)")]
        [SerializeField] private bool useBounds = false; // 카메라 이동 제한 사용
        [SerializeField] private float minX = -50f; // 최소 X 위치
        [SerializeField] private float maxX = 50f; // 최대 X 위치
        [SerializeField] private float minY = -10f; // 최소 Y 위치
        [SerializeField] private float maxY = 10f; // 최대 Y 위치

        // SmoothDamp용 속도 변수
        private Vector3 velocity = Vector3.zero;

        private void Start()
        {
            // 타겟이 없으면 플레이어 찾기
            if (target == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    target = player.transform;
                }
                else
                {
                    Debug.LogWarning("[CameraFollow] 플레이어를 찾을 수 없습니다!");
                }
            }
        }

        private void LateUpdate()
        {
            // 타겟이 없으면 플레이어 찾기 (런타임 생성된 플레이어 대응)
            if (target == null)
            {
                // 먼저 태그로 찾기 시도
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                // 태그로 못 찾으면 이름으로 찾기
                if (player == null)
                {
                    player = GameObject.Find("Player");
                }

                if (player != null)
                {
                    target = player.transform;
                    Debug.Log("[CameraFollow] 플레이어를 찾았습니다: " + player.name);
                }
                else
                {
                    return;
                }
            }

            // 현재 카메라 위치
            Vector3 currentPos = transform.position;

            // 목표 위치 계산 (항상 플레이어를 따라감)
            Vector3 targetPos = new Vector3(
                target.position.x + offset.x,
                followY ? target.position.y + offset.y : offset.y,
                offset.z
            );

            // 카메라 이동 제한 적용
            if (useBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                if (followY)
                {
                    targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
                }
            }

            // 부드러운 카메라 이동 (SmoothDamp는 거리에 상관없이 일정한 시간에 도달)
            float smoothTime = 1f / smoothSpeed; // smoothSpeed가 5면 0.2초에 도달
            transform.position = Vector3.SmoothDamp(currentPos, targetPos, ref velocity, smoothTime);
        }

        /// <summary>
        /// 타겟 설정
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// 카메라 즉시 타겟 위치로 이동
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                Vector3 targetPos = target.position + offset;

                if (useBounds)
                {
                    targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                    targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
                }

                transform.position = targetPos;
                velocity = Vector3.zero; // 속도 초기화
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDeadZone || target == null)
                return;

            // 데드존 표시
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            center.z = 0;
            Gizmos.DrawWireCube(center, new Vector3(deadZoneWidth, deadZoneHeight, 0.1f));
        }
#endif
    }
}
