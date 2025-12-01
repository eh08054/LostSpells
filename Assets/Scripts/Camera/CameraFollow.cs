using TMPro;
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
        [SerializeField] private float smoothTime = 0.25f;
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

        private Vector3 currentVelocity;
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

            // 타겟의 월드 위치를 뷰포트 좌표로 변환
            Vector3 viewportPos = GetComponent<UnityEngine.Camera>().WorldToViewportPoint(target.position);

            // 데드존 체크 (뷰포트 좌표 0~1 범위)
            float deadZoneLeft = 0.5f - (deadZoneWidth / 20f); // 20은 대략적인 뷰포트 너비
            float deadZoneRight = 0.5f + (deadZoneWidth / 20f);

            // 목표 위치 계산 (데드존 밖으로 나갔을 때만 이동)
            Vector3 targetPos = currentPos;

            // X축 체크
            if (viewportPos.x < deadZoneLeft)
            {
                // 왼쪽으로 벗어남
                targetPos.x = target.position.x + offset.x;
            }
            else if (viewportPos.x > deadZoneRight)
            {
                // 오른쪽으로 벗어남
                targetPos.x = target.position.x + offset.x;
            }

            // Y축 처리
            if (followY)
            {
                // Y축도 추적하는 경우 (데드존 사용)
                float deadZoneBottom = 0.5f - (deadZoneHeight / 20f);
                float deadZoneTop = 0.5f + (deadZoneHeight / 20f);

                if (viewportPos.y < deadZoneBottom)
                {
                    // 아래로 벗어남
                    targetPos.y = target.position.y + offset.y;
                }
                else if (viewportPos.y > deadZoneTop)
                {
                    // 위로 벗어남
                    targetPos.y = target.position.y + offset.y;
                }
            }
            else
            {
                // Y축은 고정 (점프해도 카메라가 따라가지 않음)
                targetPos.y = offset.y;
            }

            // Z축은 항상 고정
            targetPos.z = offset.z;

            // 카메라 이동 제한 적용
            if (useBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                if (followY)
                {
                    targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
                }
            }

            // 부드러운 카메라 이동
            //transform.position = Vector3.Lerp(currentPos, targetPos, smoothSpeed * Time.deltaTime);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
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
