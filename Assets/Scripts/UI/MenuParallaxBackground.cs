using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace LostSpells.UI
{
    /// <summary>
    /// 메뉴 화면의 UI 기반 패럴랙스 배경을 관리합니다.
    /// - 각 레이어를 다른 속도로 스크롤
    /// - 두 개의 이미지를 나란히 배치하여 끊김 없는 무한 스크롤 구현
    /// - 12개의 미리 정의된 맵 중 무작위로 변경
    /// </summary>
    public class MenuParallaxBackground : MonoBehaviour
    {
        /// <summary>
        /// 맵 구성 (Sky, Mountain, Ground 번호)
        /// </summary>
        [System.Serializable]
        public struct MapConfig
        {
            public int skyNumber;      // Sky-N의 N
            public int mountainNumber; // Mountain-N의 N
            public int groundNumber;   // Ground-N의 N

            public MapConfig(int sky, int mountain, int ground)
            {
                skyNumber = sky;
                mountainNumber = mountain;
                groundNumber = ground;
            }
        }

        // 12개의 미리 정의된 맵 (Demo 1~12 씬에서 추출한 정확한 구성)
        private static readonly MapConfig[] predefinedMaps = new MapConfig[]
        {
            new MapConfig(12, 1, 1),   // Demo 1: Sky-12, Mountain-1, Ground-1
            new MapConfig(5, 6, 4),    // Demo 2: Sky-5, Mountain-6, Ground-4
            new MapConfig(15, 8, 6),   // Demo 3: Sky-15, Mountain-8, Ground-6
            new MapConfig(6, 9, 19),   // Demo 4: Sky-6, Mountain-9, Ground-19
            new MapConfig(9, 3, 13),   // Demo 5: Sky-9, Mountain-3, Ground-13
            new MapConfig(14, 5, 16),  // Demo 6: Sky-14, Mountain-5, Ground-16
            new MapConfig(15, 5, 10),  // Demo 7: Sky-15, Mountain-5, Ground-10
            new MapConfig(11, 4, 17),  // Demo 8: Sky-11, Mountain-4, Ground-17
            new MapConfig(13, 5, 20),  // Demo 9: Sky-13, Mountain-5, Ground-20
            new MapConfig(3, 2, 4),    // Demo 10: Sky-3, Mountain-2, Ground-4
            new MapConfig(4, 7, 18),   // Demo 11: Sky-4, Mountain-7, Ground-18
            new MapConfig(5, 1, 21),   // Demo 12: Sky-5, Mountain-1, Ground-21
        };

        [Header("Parallax Scroll Settings")]
        [SerializeField] private bool enableScrolling = true;
        [SerializeField] private float skyScrollSpeed = 30f;
        [SerializeField] private float mountainScrollSpeed = 60f;
        [SerializeField] private float groundScrollSpeed = 120f;

        [Header("Random Map Change Settings")]
        [SerializeField] private bool enableRandomChange = true;
        [SerializeField] private float changeInterval = 10f;

        private VisualElement rootElement;

        // 각 레이어의 컨테이너
        private VisualElement skyLayer;
        private VisualElement mountainLayer;
        private VisualElement groundLayer;

        // 각 레이어의 두 개 이미지 (A, B)
        private VisualElement skyA, skyB;
        private VisualElement mountainA, mountainB;
        private VisualElement groundA, groundB;

        // 스크롤 오프셋 (Static으로 씬 전환 시에도 유지)
        private static float skyOffset = 0f;
        private static float mountainOffset = 0f;
        private static float groundOffset = 0f;

        // 각 이미지의 실제 너비 (화면 높이에 맞춰 스케일된 값)
        private float skyImageWidth = 0f;
        private float mountainImageWidth = 0f;
        private float groundImageWidth = 0f;

        // 텍스처 캐시 (번호 -> 텍스처)
        private Dictionary<int, Texture2D> skyTextureCache = new Dictionary<int, Texture2D>();
        private Dictionary<int, Texture2D> mountainTextureCache = new Dictionary<int, Texture2D>();
        private Dictionary<int, Texture2D> groundTextureCache = new Dictionary<int, Texture2D>();

        // 현재 적용된 텍스처
        private Texture2D currentSkyTexture;
        private Texture2D currentMountainTexture;
        private Texture2D currentGroundTexture;

        private static float changeTimer = 0f;
        private static int currentMapIndex = 0;
        private static bool isInitialized = false;

        void Start()
        {
            InitializeLayers();
            LoadBackgroundTextures();

            if (!isInitialized && enableRandomChange)
            {
                currentMapIndex = Random.Range(0, predefinedMaps.Length);
                isInitialized = true;
            }

            ApplyMap(currentMapIndex);
        }

        void Update()
        {
            if (enableScrolling)
            {
                UpdateScrolling();
            }

            if (enableRandomChange)
            {
                UpdateRandomChange();
            }
        }

        private void InitializeLayers()
        {
            UIDocument uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("MenuParallaxBackground: UIDocument component not found!");
                return;
            }

            rootElement = uiDocument.rootVisualElement;

            // 각 레이어 컨테이너 찾기
            skyLayer = rootElement.Q<VisualElement>("Sky");
            mountainLayer = rootElement.Q<VisualElement>("Mountain");
            groundLayer = rootElement.Q<VisualElement>("Ground");

            if (skyLayer == null || mountainLayer == null || groundLayer == null)
            {
                Debug.LogError("MenuParallaxBackground: Background layers not found in UI!");
                return;
            }

            // 각 레이어에 두 개의 자식 이미지 생성
            CreateDualImages(skyLayer, out skyA, out skyB);
            CreateDualImages(mountainLayer, out mountainA, out mountainB);
            CreateDualImages(groundLayer, out groundA, out groundB);
        }

        private void CreateDualImages(VisualElement parent, out VisualElement imageA, out VisualElement imageB)
        {
            // 부모의 배경 이미지 제거 (자식이 처리)
            parent.style.backgroundImage = StyleKeyword.None;

            // 자식 요소가 부모 영역 밖으로 나가도 보이도록 설정
            parent.style.overflow = Overflow.Visible;

            // 첫 번째 이미지 (A)
            imageA = new VisualElement();
            imageA.name = parent.name + "_A";
            imageA.style.position = Position.Absolute;
            imageA.style.top = 0;
            imageA.style.height = Length.Percent(100);
            imageA.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;

            // 두 번째 이미지 (B)
            imageB = new VisualElement();
            imageB.name = parent.name + "_B";
            imageB.style.position = Position.Absolute;
            imageB.style.top = 0;
            imageB.style.height = Length.Percent(100);
            imageB.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;

            parent.Add(imageA);
            parent.Add(imageB);
        }

        private void LoadBackgroundTextures()
        {
            // Resources 폴더에서 모든 텍스처 로드
            var skyTextures = Resources.LoadAll<Texture2D>("Backgrounds/Sky");
            var mountainTextures = Resources.LoadAll<Texture2D>("Backgrounds/Mountain");
            var groundTextures = Resources.LoadAll<Texture2D>("Backgrounds/Ground");

            // 텍스처 이름에서 번호 추출하여 캐시에 저장
            foreach (var tex in skyTextures)
            {
                int number = ExtractNumberFromName(tex.name);
                if (number > 0) skyTextureCache[number] = tex;
            }

            foreach (var tex in mountainTextures)
            {
                int number = ExtractNumberFromName(tex.name);
                if (number > 0) mountainTextureCache[number] = tex;
            }

            foreach (var tex in groundTextures)
            {
                int number = ExtractNumberFromName(tex.name);
                if (number > 0) groundTextureCache[number] = tex;
            }

            Debug.Log($"[MenuParallax] Loaded {skyTextureCache.Count} sky, {mountainTextureCache.Count} mountain, {groundTextureCache.Count} ground textures");
        }

        private int ExtractNumberFromName(string name)
        {
            // "Sky-1", "Mountain-2", "Ground-12" 등에서 숫자 추출
            var parts = name.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int number))
            {
                return number;
            }
            return -1;
        }

        private Texture2D GetTexture(Dictionary<int, Texture2D> cache, int number)
        {
            if (cache.TryGetValue(number, out Texture2D tex))
                return tex;

            // 해당 번호가 없으면 가장 가까운 번호 사용
            if (cache.Count > 0)
            {
                var keys = cache.Keys.OrderBy(k => k).ToList();
                int closest = keys.FirstOrDefault(k => k >= number);
                if (closest == 0) closest = keys.Last();
                return cache[closest];
            }

            return null;
        }

        /// <summary>
        /// 이미지의 스케일된 너비 계산 (높이에 맞춰 비율 유지)
        /// </summary>
        private float CalculateScaledWidth(Texture2D texture, float containerHeight)
        {
            if (texture == null || containerHeight <= 0) return 0;
            float aspectRatio = (float)texture.width / texture.height;
            return containerHeight * aspectRatio;
        }

        private void UpdateScrolling()
        {
            if (skyLayer == null || skyA == null)
                return;

            // 컨테이너 크기 가져오기
            float containerHeight = skyLayer.resolvedStyle.height;
            if (containerHeight <= 0) return;

            // 현재 텍스처의 스케일된 너비 계산
            if (currentSkyTexture == null || currentMountainTexture == null || currentGroundTexture == null)
                return;

            skyImageWidth = CalculateScaledWidth(currentSkyTexture, containerHeight);
            mountainImageWidth = CalculateScaledWidth(currentMountainTexture, containerHeight);
            groundImageWidth = CalculateScaledWidth(currentGroundTexture, containerHeight);

            if (skyImageWidth <= 0 || mountainImageWidth <= 0 || groundImageWidth <= 0) return;

            // 이미지 요소의 너비 설정
            skyA.style.width = skyImageWidth;
            skyB.style.width = skyImageWidth;
            mountainA.style.width = mountainImageWidth;
            mountainB.style.width = mountainImageWidth;
            groundA.style.width = groundImageWidth;
            groundB.style.width = groundImageWidth;

            // 오프셋 업데이트
            float deltaTime = Time.deltaTime;
            skyOffset += skyScrollSpeed * deltaTime;
            mountainOffset += mountainScrollSpeed * deltaTime;
            groundOffset += groundScrollSpeed * deltaTime;

            // 오프셋이 이미지 너비를 넘으면 리셋 (무한 루프)
            if (skyOffset >= skyImageWidth) skyOffset -= skyImageWidth;
            if (mountainOffset >= mountainImageWidth) mountainOffset -= mountainImageWidth;
            if (groundOffset >= groundImageWidth) groundOffset -= groundImageWidth;

            // 각 레이어의 두 이미지 위치 업데이트
            UpdateLayerPositions(skyA, skyB, skyOffset, skyImageWidth);
            UpdateLayerPositions(mountainA, mountainB, mountainOffset, mountainImageWidth);
            UpdateLayerPositions(groundA, groundB, groundOffset, groundImageWidth);
        }

        private void UpdateLayerPositions(VisualElement imgA, VisualElement imgB, float offset, float imgWidth)
        {
            if (imgA == null || imgB == null || imgWidth <= 0) return;

            // A는 왼쪽으로 offset만큼 이동
            float posA = -offset;

            // B는 A 바로 오른쪽에 위치
            float posB = imgWidth - offset;

            imgA.style.left = posA;
            imgB.style.left = posB;
        }

        private void UpdateRandomChange()
        {
            changeTimer += Time.deltaTime;

            if (changeTimer >= changeInterval)
            {
                changeTimer = 0f;

                int newMapIndex;
                do
                {
                    newMapIndex = Random.Range(0, predefinedMaps.Length);
                } while (newMapIndex == currentMapIndex && predefinedMaps.Length > 1);

                currentMapIndex = newMapIndex;
                ApplyMap(currentMapIndex);
            }
        }

        private void ApplyMap(int mapIndex)
        {
            if (skyA == null || mountainA == null || groundA == null)
                return;

            if (mapIndex < 0 || mapIndex >= predefinedMaps.Length)
                return;

            var map = predefinedMaps[mapIndex];

            // 각 레이어에 맞는 텍스처 가져오기
            currentSkyTexture = GetTexture(skyTextureCache, map.skyNumber);
            currentMountainTexture = GetTexture(mountainTextureCache, map.mountainNumber);
            currentGroundTexture = GetTexture(groundTextureCache, map.groundNumber);

            if (currentSkyTexture == null || currentMountainTexture == null || currentGroundTexture == null)
            {
                Debug.LogError($"[MenuParallax] Failed to load textures for map {mapIndex + 1}");
                return;
            }

            // A와 B 모두에 같은 이미지 적용
            var skyBg = new StyleBackground(currentSkyTexture);
            var mountBg = new StyleBackground(currentMountainTexture);
            var groundBg = new StyleBackground(currentGroundTexture);

            skyA.style.backgroundImage = skyBg;
            skyB.style.backgroundImage = skyBg;
            mountainA.style.backgroundImage = mountBg;
            mountainB.style.backgroundImage = mountBg;
            groundA.style.backgroundImage = groundBg;
            groundB.style.backgroundImage = groundBg;

            // 오프셋 리셋 (새 이미지 너비가 다를 수 있으므로)
            skyOffset = 0f;
            mountainOffset = 0f;
            groundOffset = 0f;

            Debug.Log($"[MenuParallax] Applied Map {mapIndex + 1}: Sky-{map.skyNumber}, Mountain-{map.mountainNumber}, Ground-{map.groundNumber}");
        }
    }
}
