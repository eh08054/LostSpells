using UnityEngine;

namespace LostSpells.Background
{
    /// <summary>
    /// 인게임 패럴랙스 배경 시스템 (2D Scrolling Parallax Background Pack 사용)
    /// 챕터 ID에 따라 자동으로 배경 선택
    /// 무한 타일링 지원
    /// </summary>
    public class InGameParallaxBackground : MonoBehaviour
    {
        [Header("배경 레이어 렌더러")]
        public SpriteRenderer skyRenderer;
        public SpriteRenderer mountainRenderer;
        public SpriteRenderer groundRenderer;

        [Header("배경 스프라이트")]
        public Sprite[] skySprites;
        public Sprite[] mountainSprites;
        public Sprite[] groundSprites;

        [Header("패럴랙스 스크롤 속도")]
        public float skySpeed = 0.5f;
        public float mountainSpeed = 1.5f;
        public float groundSpeed = 3f;

        [Header("설정")]
        public bool useChapterBackground = true;
        public int manualBackgroundIndex = 0;

        private UnityEngine.Camera mainCamera;

        // 무한 타일링을 위한 타일 배열 (왼쪽, 중앙, 오른쪽)
        private SpriteRenderer[] skyTiles = new SpriteRenderer[3];
        private SpriteRenderer[] mountainTiles = new SpriteRenderer[3];
        private SpriteRenderer[] groundTiles = new SpriteRenderer[3];

        private float skyTileWidth;
        private float mountainTileWidth;
        private float groundTileWidth;

        void Start()
        {
            int bgIndex = GetBackgroundIndex();
            ApplyBackgroundVariant(bgIndex);

            // 무한 타일링 설정
            SetupInfiniteTiling();

            // 메인 카메라 찾기
            mainCamera = UnityEngine.Camera.main;
        }

        void LateUpdate()
        {
            // LateUpdate를 사용하여 카메라 이동 후 타일 재배치
            // Time.timeScale과 무관하게 실행됨
            UpdateInfiniteTiling();
        }

        private void SetupInfiniteTiling()
        {
            // 기존 렌더러를 중앙 타일로 설정
            skyTiles[1] = skyRenderer;
            mountainTiles[1] = mountainRenderer;
            groundTiles[1] = groundRenderer;

            // 타일 너비 계산
            if (skyRenderer != null && skyRenderer.sprite != null)
            {
                skyTileWidth = skyRenderer.sprite.bounds.size.x;
                CreateTiles(skyTiles, skyRenderer, "Sky");
            }

            if (mountainRenderer != null && mountainRenderer.sprite != null)
            {
                mountainTileWidth = mountainRenderer.sprite.bounds.size.x;
                CreateTiles(mountainTiles, mountainRenderer, "Mountain");
            }

            if (groundRenderer != null && groundRenderer.sprite != null)
            {
                groundTileWidth = groundRenderer.sprite.bounds.size.x;
                CreateTiles(groundTiles, groundRenderer, "Ground");
            }
        }

        private void CreateTiles(SpriteRenderer[] tiles, SpriteRenderer original, string layerName)
        {
            float tileWidth = original.sprite.bounds.size.x;

            // 왼쪽 타일 생성
            GameObject leftTile = new GameObject($"{layerName}_Left");
            leftTile.transform.SetParent(original.transform.parent);
            leftTile.transform.position = original.transform.position + Vector3.left * tileWidth;
            leftTile.transform.localScale = original.transform.localScale;
            SpriteRenderer leftRenderer = leftTile.AddComponent<SpriteRenderer>();
            leftRenderer.sprite = original.sprite;
            leftRenderer.sortingLayerName = original.sortingLayerName;
            leftRenderer.sortingOrder = original.sortingOrder;
            tiles[0] = leftRenderer;

            // 오른쪽 타일 생성
            GameObject rightTile = new GameObject($"{layerName}_Right");
            rightTile.transform.SetParent(original.transform.parent);
            rightTile.transform.position = original.transform.position + Vector3.right * tileWidth;
            rightTile.transform.localScale = original.transform.localScale;
            SpriteRenderer rightRenderer = rightTile.AddComponent<SpriteRenderer>();
            rightRenderer.sprite = original.sprite;
            rightRenderer.sortingLayerName = original.sortingLayerName;
            rightRenderer.sortingOrder = original.sortingOrder;
            tiles[2] = rightRenderer;
        }

        private int GetBackgroundIndex()
        {
            if (!useChapterBackground)
                return manualBackgroundIndex;

            if (LostSpells.Systems.GameStateManager.Instance != null)
            {
                int chapterId = LostSpells.Systems.GameStateManager.Instance.GetCurrentChapterId();

                if (chapterId >= 0 && chapterId < 8)
                {
                    return chapterId;
                }
            }

            return manualBackgroundIndex;
        }

        private void ApplyBackgroundVariant(int index)
        {
            index = Mathf.Clamp(index, 0, 7);

            int skyIndex = index % 4;
            int mountainIndex = index % 4;
            int groundIndex = index % 4;

            if (skySprites != null && skySprites.Length > skyIndex && skyRenderer != null)
            {
                skyRenderer.sprite = skySprites[skyIndex];
            }

            if (mountainSprites != null && mountainSprites.Length > mountainIndex && mountainRenderer != null)
            {
                mountainRenderer.sprite = mountainSprites[mountainIndex];
            }

            if (groundSprites != null && groundSprites.Length > groundIndex && groundRenderer != null)
            {
                groundRenderer.sprite = groundSprites[groundIndex];
            }
        }

        private void UpdateInfiniteTiling()
        {
            if (mainCamera == null)
                return;

            // 각 레이어별로 타일 재배치
            RepositionTiles(skyTiles, skyTileWidth);
            RepositionTiles(mountainTiles, mountainTileWidth);
            RepositionTiles(groundTiles, groundTileWidth);
        }

        private void RepositionTiles(SpriteRenderer[] tiles, float tileWidth)
        {
            if (tiles[1] == null || mainCamera == null)
                return;

            float cameraX = mainCamera.transform.position.x;

            // 왼쪽 타일이 카메라 시야를 완전히 벗어나면 오른쪽으로 이동
            if (tiles[0] != null && tiles[0].transform.position.x + tileWidth < cameraX - tileWidth)
            {
                tiles[0].transform.position = tiles[2].transform.position + Vector3.right * tileWidth;

                // 배열 순환 (왼쪽 -> 오른쪽으로 이동)
                SpriteRenderer temp = tiles[0];
                tiles[0] = tiles[1];
                tiles[1] = tiles[2];
                tiles[2] = temp;
            }
            // 오른쪽 타일이 카메라 시야를 완전히 벗어나면 왼쪽으로 이동
            else if (tiles[2] != null && tiles[2].transform.position.x - tileWidth > cameraX + tileWidth)
            {
                tiles[2].transform.position = tiles[0].transform.position + Vector3.left * tileWidth;

                // 배열 순환 (오른쪽 -> 왼쪽으로 이동)
                SpriteRenderer temp = tiles[2];
                tiles[2] = tiles[1];
                tiles[1] = tiles[0];
                tiles[0] = temp;
            }
        }

        public void SetBackgroundIndex(int index)
        {
            manualBackgroundIndex = Mathf.Clamp(index, 0, 7);
            useChapterBackground = false;
            ApplyBackgroundVariant(manualBackgroundIndex);
        }
    }
}
