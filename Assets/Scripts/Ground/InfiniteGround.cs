using UnityEngine;

namespace LostSpells.Ground
{
    /// <summary>
    /// 무한 땅 시스템
    /// 카메라를 따라 땅 콜라이더를 재배치하여 플레이어가 계속 걸을 수 있게 함
    /// </summary>
    public class InfiniteGround : MonoBehaviour
    {
        [Header("Ground Settings")]
        [SerializeField] private float groundWidth = 40f; // 각 땅 타일의 너비
        [SerializeField] private float groundHeight = 2f; // 땅의 높이 (충돌 영역)
        [SerializeField] private float groundY = -4f; // 땅의 Y 위치 (중앙)

        [Header("Auto-Align with Background")]
        [SerializeField] private bool autoAlignWithGroundSprite = true; // Ground 스프라이트와 자동 정렬
        [SerializeField] private SpriteRenderer groundSpriteReference; // 참조할 Ground 스프라이트

        private UnityEngine.Camera mainCamera;
        private BoxCollider2D[] groundTiles = new BoxCollider2D[3]; // 왼쪽, 중앙, 오른쪽

        void Start()
        {
            mainCamera = UnityEngine.Camera.main;

            // Ground 스프라이트와 자동 정렬
            if (autoAlignWithGroundSprite && groundSpriteReference != null)
            {
                AlignWithGroundSprite();
            }

            SetupGroundTiles();
        }

        void Update()
        {
            UpdateGroundTiling();
        }

        private void SetupGroundTiles()
        {
            // 3개의 땅 타일 생성
            for (int i = 0; i < 3; i++)
            {
                GameObject groundObj = new GameObject($"Ground_Tile_{i}");
                groundObj.transform.SetParent(transform);
                groundObj.layer = LayerMask.NameToLayer("Default");

                // BoxCollider2D 추가
                BoxCollider2D collider = groundObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(groundWidth, groundHeight);

                // 위치 설정 (왼쪽: -1, 중앙: 0, 오른쪽: 1)
                float xPos = (i - 1) * groundWidth;
                groundObj.transform.position = new Vector3(xPos, groundY, 0);

                groundTiles[i] = collider;
            }
        }

        private void UpdateGroundTiling()
        {
            if (mainCamera == null)
                return;

            float cameraX = mainCamera.transform.position.x;

            // 왼쪽 타일이 카메라 시야를 완전히 벗어나면 오른쪽으로 이동
            if (groundTiles[0] != null && groundTiles[0].transform.position.x + groundWidth < cameraX - groundWidth)
            {
                groundTiles[0].transform.position = groundTiles[2].transform.position + Vector3.right * groundWidth;

                // 배열 순환
                BoxCollider2D temp = groundTiles[0];
                groundTiles[0] = groundTiles[1];
                groundTiles[1] = groundTiles[2];
                groundTiles[2] = temp;
            }
            // 오른쪽 타일이 카메라 시야를 완전히 벗어나면 왼쪽으로 이동
            else if (groundTiles[2] != null && groundTiles[2].transform.position.x - groundWidth > cameraX + groundWidth)
            {
                groundTiles[2].transform.position = groundTiles[0].transform.position + Vector3.left * groundWidth;

                // 배열 순환
                BoxCollider2D temp = groundTiles[2];
                groundTiles[2] = groundTiles[1];
                groundTiles[1] = groundTiles[0];
                groundTiles[0] = temp;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 땅 타일 시각화
            Gizmos.color = Color.green;

            if (Application.isPlaying && groundTiles[0] != null)
            {
                // 런타임 중에는 실제 타일 표시
                foreach (var tile in groundTiles)
                {
                    if (tile != null)
                    {
                        Vector3 center = tile.transform.position;
                        Vector3 size = new Vector3(tile.size.x, tile.size.y, 0.1f);
                        Gizmos.DrawWireCube(center, size);
                    }
                }
            }
            else
            {
                // 에디터에서는 예상 위치 표시
                for (int i = 0; i < 3; i++)
                {
                    float xPos = (i - 1) * groundWidth;
                    Vector3 center = new Vector3(xPos, groundY, 0);
                    Vector3 size = new Vector3(groundWidth, groundHeight, 0.1f);
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }
#endif

        /// <summary>
        /// Ground 스프라이트와 정렬
        /// </summary>
        private void AlignWithGroundSprite()
        {
            if (groundSpriteReference == null || groundSpriteReference.sprite == null)
                return;

            // Ground 스프라이트의 bounds 정보 가져오기
            Bounds spriteBounds = groundSpriteReference.sprite.bounds;
            Vector3 spritePosition = groundSpriteReference.transform.position;

            // Ground 스프라이트 상단에 콜라이더 배치
            // 스프라이트의 상단 Y 위치 계산
            float spriteTop = spritePosition.y + spriteBounds.max.y;

            // 콜라이더를 스프라이트 상단에 배치 (콜라이더 높이의 절반만큼 위로)
            groundY = spriteTop - (groundHeight / 2f);

            // Debug.Log($"Ground aligned: Sprite top={spriteTop}, Collider Y={groundY}");
        }

        /// <summary>
        /// 땅의 Y 위치 설정
        /// </summary>
        public void SetGroundY(float y)
        {
            groundY = y;

            // 이미 생성된 타일들의 Y 위치 업데이트
            if (groundTiles[0] != null)
            {
                foreach (var tile in groundTiles)
                {
                    if (tile != null)
                    {
                        Vector3 pos = tile.transform.position;
                        pos.y = groundY;
                        tile.transform.position = pos;
                    }
                }
            }
        }

        /// <summary>
        /// 땅의 너비 설정
        /// </summary>
        public void SetGroundWidth(float width)
        {
            groundWidth = width;

            // 이미 생성된 타일들의 크기 업데이트
            if (groundTiles[0] != null)
            {
                foreach (var tile in groundTiles)
                {
                    if (tile != null)
                    {
                        tile.size = new Vector2(groundWidth, groundHeight);
                    }
                }
            }
        }
    }
}
