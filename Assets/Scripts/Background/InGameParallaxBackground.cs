using UnityEngine;
using System.Collections.Generic;

namespace LostSpells.Background
{
    /// <summary>
    /// 인게임 패럴랙스 배경 시스템 (2D Scrolling Parallax Background Pack 사용)
    /// GameStateManager의 맵 설정에 따라 자동으로 배경 선택
    /// 무한 타일링 지원
    /// </summary>
    public class InGameParallaxBackground : MonoBehaviour
    {
        [Header("배경 레이어 렌더러")]
        public SpriteRenderer skyRenderer;
        public SpriteRenderer mountainRenderer;
        public SpriteRenderer groundRenderer;

        [Header("패럴랙스 스크롤 속도")]
        public float skySpeed = 0.5f;
        public float mountainSpeed = 1.5f;
        public float groundSpeed = 3f;

        private UnityEngine.Camera mainCamera;

        // 무한 타일링을 위한 타일 배열 (왼쪽, 중앙, 오른쪽)
        private SpriteRenderer[] skyTiles = new SpriteRenderer[3];
        private SpriteRenderer[] mountainTiles = new SpriteRenderer[3];
        private SpriteRenderer[] groundTiles = new SpriteRenderer[3];

        private float skyTileWidth;
        private float mountainTileWidth;
        private float groundTileWidth;

        // 스프라이트 캐시 (static으로 씬 전환 시에도 유지)
        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // 타일링 업데이트 최적화
        private float lastCameraX;
        private const float UPDATE_THRESHOLD = 0.5f;

        void Start()
        {
            // 메인 카메라 찾기
            mainCamera = UnityEngine.Camera.main;

            // GameStateManager의 맵 설정 적용 (필요한 텍스처만 로드)
            ApplyMapFromGameState();

            // 카메라 높이에 맞게 스케일 조정
            ScaleToFitCamera();

            // 무한 타일링 설정
            SetupInfiniteTiling();
        }

        private void ScaleToFitCamera()
        {
            if (mainCamera == null || !mainCamera.orthographic)
                return;

            // 카메라의 월드 높이 계산
            float cameraHeight = mainCamera.orthographicSize * 2f;

            // 각 레이어의 스프라이트를 카메라 높이에 맞게 스케일
            ScaleSpriteToHeight(skyRenderer, cameraHeight);
            ScaleSpriteToHeight(mountainRenderer, cameraHeight);
            ScaleSpriteToHeight(groundRenderer, cameraHeight);
        }

        private void ScaleSpriteToHeight(SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null)
                return;

            float spriteHeight = renderer.sprite.bounds.size.y;
            float scale = targetHeight / spriteHeight;

            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private Sprite LoadSprite(string folder, int number)
        {
            string path = $"Backgrounds/{folder}/{folder}-{number}";

            // 캐시에서 먼저 확인
            if (spriteCache.TryGetValue(path, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            // 캐시에 없으면 로드
            Texture2D tex = Resources.Load<Texture2D>(path);

            if (tex != null)
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = tex.name;
                spriteCache[path] = sprite; // 캐시에 저장
                return sprite;
            }

            Debug.LogWarning($"[InGameParallax] Failed to load texture: {path}");
            return null;
        }

        private void ApplyMapFromGameState()
        {
            var gameState = LostSpells.Systems.GameStateManager.Instance;

            int skyNumber = 12;
            int mountainNumber = 1;
            int groundNumber = 1;

            if (gameState != null)
            {
                skyNumber = gameState.GetCurrentSkyNumber();
                mountainNumber = gameState.GetCurrentMountainNumber();
                groundNumber = gameState.GetCurrentGroundNumber();
            }

            // 필요한 스프라이트만 로드
            Sprite skySprite = LoadSprite("Sky", skyNumber);
            Sprite mountainSprite = LoadSprite("Mountain", mountainNumber);
            Sprite groundSprite = LoadSprite("Ground", groundNumber);

            if (skyRenderer != null && skySprite != null)
                skyRenderer.sprite = skySprite;

            if (mountainRenderer != null && mountainSprite != null)
                mountainRenderer.sprite = mountainSprite;

            if (groundRenderer != null && groundSprite != null)
                groundRenderer.sprite = groundSprite;

            Debug.Log($"[InGameParallax] Applied Map: Sky-{skyNumber}, Mountain-{mountainNumber}, Ground-{groundNumber}");
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

            // 타일 너비 계산 (스케일 적용된 실제 월드 너비)
            if (skyRenderer != null && skyRenderer.sprite != null)
            {
                skyTileWidth = skyRenderer.sprite.bounds.size.x * skyRenderer.transform.localScale.x;
                CreateTiles(skyTiles, skyRenderer, "Sky", skyTileWidth);
            }

            if (mountainRenderer != null && mountainRenderer.sprite != null)
            {
                mountainTileWidth = mountainRenderer.sprite.bounds.size.x * mountainRenderer.transform.localScale.x;
                CreateTiles(mountainTiles, mountainRenderer, "Mountain", mountainTileWidth);
            }

            if (groundRenderer != null && groundRenderer.sprite != null)
            {
                groundTileWidth = groundRenderer.sprite.bounds.size.x * groundRenderer.transform.localScale.x;
                CreateTiles(groundTiles, groundRenderer, "Ground", groundTileWidth);
            }
        }

        private void CreateTiles(SpriteRenderer[] tiles, SpriteRenderer original, string layerName, float worldWidth)
        {
            // 왼쪽 타일 생성
            GameObject leftTile = new GameObject($"{layerName}_Left");
            leftTile.transform.SetParent(original.transform.parent);
            leftTile.transform.position = original.transform.position + Vector3.left * worldWidth;
            leftTile.transform.localScale = original.transform.localScale;
            SpriteRenderer leftRenderer = leftTile.AddComponent<SpriteRenderer>();
            leftRenderer.sprite = original.sprite;
            leftRenderer.sortingLayerName = original.sortingLayerName;
            leftRenderer.sortingOrder = original.sortingOrder;
            tiles[0] = leftRenderer;

            // 오른쪽 타일 생성
            GameObject rightTile = new GameObject($"{layerName}_Right");
            rightTile.transform.SetParent(original.transform.parent);
            rightTile.transform.position = original.transform.position + Vector3.right * worldWidth;
            rightTile.transform.localScale = original.transform.localScale;
            SpriteRenderer rightRenderer = rightTile.AddComponent<SpriteRenderer>();
            rightRenderer.sprite = original.sprite;
            rightRenderer.sortingLayerName = original.sortingLayerName;
            rightRenderer.sortingOrder = original.sortingOrder;
            tiles[2] = rightRenderer;
        }

        private void UpdateInfiniteTiling()
        {
            if (mainCamera == null)
                return;

            // 카메라가 일정 거리 이상 이동했을 때만 업데이트
            float currentCameraX = mainCamera.transform.position.x;
            if (Mathf.Abs(currentCameraX - lastCameraX) < UPDATE_THRESHOLD)
                return;

            lastCameraX = currentCameraX;

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

    }
}
