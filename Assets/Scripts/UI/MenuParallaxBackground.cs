using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LostSpells.UI
{
    /// <summary>
    /// 메뉴 화면의 UI 기반 패럴랙스 배경을 관리합니다.
    /// - 각 레이어를 다른 속도로 스크롤
    /// - 일정 시간마다 배경 이미지를 무작위로 변경
    /// </summary>
    public class MenuParallaxBackground : MonoBehaviour
    {
        [Header("Parallax Scroll Settings")]
        [SerializeField] private bool enableScrolling = true;
        [SerializeField] private float skyScrollSpeed = 2f;
        [SerializeField] private float mountainScrollSpeed = 5f;
        [SerializeField] private float groundScrollSpeed = 10f;

        [Header("Random Background Change Settings")]
        [SerializeField] private bool enableRandomChange = true;
        [SerializeField] private float changeInterval = 10f; // 10초마다 변경

        [Header("Background Sprites")]
        [SerializeField] private Texture2D[] skyTextures;
        [SerializeField] private Texture2D[] mountainTextures;
        [SerializeField] private Texture2D[] groundTextures;

        private VisualElement rootElement;
        private VisualElement skyLayer;
        private VisualElement mountainLayer;
        private VisualElement groundLayer;

        // Static으로 씬 전환 시에도 offset 유지
        private static float skyOffset = 0f;
        private static float mountainOffset = 0f;
        private static float groundOffset = 0f;

        private static float changeTimer = 0f;
        private static int currentVariant = 0;
        private static bool isInitialized = false; // 최초 1회만 초기화

        void Start()
        {
            InitializeLayers();
            LoadBackgroundTextures();

            // 최초 실행시에만 랜덤 배경 선택, 이후 씬 전환시에는 현재 배경 유지
            if (!isInitialized && enableRandomChange && HasValidTextures())
            {
                currentVariant = Random.Range(0, skyTextures.Length);
                isInitialized = true;
            }

            // 현재 배경 적용 (씬 전환시 동일한 배경 유지)
            if (HasValidTextures())
            {
                ApplyBackgroundVariant(currentVariant);
            }
        }

        void Update()
        {
            if (enableScrolling)
            {
                UpdateScrolling();
            }

            if (enableRandomChange && HasValidTextures())
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

            // 각 레이어 찾기
            skyLayer = rootElement.Q<VisualElement>("Sky");
            mountainLayer = rootElement.Q<VisualElement>("Mountain");
            groundLayer = rootElement.Q<VisualElement>("Ground");

            if (skyLayer == null || mountainLayer == null || groundLayer == null)
            {
                Debug.LogError("MenuParallaxBackground: Background layers not found in UI!");
            }
        }

        private void LoadBackgroundTextures()
        {
            if (skyTextures != null && skyTextures.Length > 0)
                return; // 이미 Inspector에서 설정됨

#if UNITY_EDITOR
            // 자동으로 배경 텍스처 로드
            string basePath = "Assets/Templates/2D Scrolling Parallax Background Pack/Backgrounds";

            skyTextures = LoadTexturesFromFolder($"{basePath}/Sky");
            mountainTextures = LoadTexturesFromFolder($"{basePath}/Mountain");
            groundTextures = LoadTexturesFromFolder($"{basePath}/Ground");

            Debug.Log($"Loaded {skyTextures.Length} sky textures, {mountainTextures.Length} mountain textures, {groundTextures.Length} ground textures");
#endif
        }

#if UNITY_EDITOR
        private Texture2D[] LoadTexturesFromFolder(string folderPath)
        {
            List<Texture2D> textures = new List<Texture2D>();

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    textures.Add(texture);
                }
            }

            return textures.ToArray();
        }
#endif

        private bool HasValidTextures()
        {
            return skyTextures != null && skyTextures.Length > 0 &&
                   mountainTextures != null && mountainTextures.Length > 0 &&
                   groundTextures != null && groundTextures.Length > 0;
        }

        private void UpdateScrolling()
        {
            if (skyLayer == null || mountainLayer == null || groundLayer == null)
                return;

            float deltaTime = Time.deltaTime;

            // 각 레이어의 offset 업데이트
            skyOffset += skyScrollSpeed * deltaTime;
            mountainOffset += mountainScrollSpeed * deltaTime;
            groundOffset += groundScrollSpeed * deltaTime;

            // offset이 100을 넘으면 0으로 리셋 (seamless loop)
            if (skyOffset >= 100f) skyOffset = 0f;
            if (mountainOffset >= 100f) mountainOffset = 0f;
            if (groundOffset >= 100f) groundOffset = 0f;

            // background-position 스타일 적용
            skyLayer.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(skyOffset, LengthUnit.Percent));
            mountainLayer.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(mountainOffset, LengthUnit.Percent));
            groundLayer.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(groundOffset, LengthUnit.Percent));
        }

        private void UpdateRandomChange()
        {
            changeTimer += Time.deltaTime;

            if (changeTimer >= changeInterval)
            {
                changeTimer = 0f;

                if (skyTextures == null || skyTextures.Length == 0)
                {
                    Debug.LogError("[MenuParallax] No textures loaded! Run 'LostSpells > Setup Menu Parallax Backgrounds'");
                    return;
                }

                // 현재와 다른 랜덤 번호 선택
                int newVariant;
                do
                {
                    newVariant = Random.Range(0, skyTextures.Length);
                } while (newVariant == currentVariant && skyTextures.Length > 1);

                currentVariant = newVariant;
                ApplyBackgroundVariant(currentVariant);
            }
        }

        private void ApplyBackgroundVariant(int index)
        {
            if (skyLayer == null || mountainLayer == null || groundLayer == null)
                return;

            if (!HasValidTextures())
                return;

            // 인덱스 범위 확인
            int skyIndex = Mathf.Min(index, skyTextures.Length - 1);
            int mountainIndex = Mathf.Min(index, mountainTextures.Length - 1);
            int groundIndex = Mathf.Min(index, groundTextures.Length - 1);

            // Unity UI Toolkit에서 background-image 변경
            skyLayer.style.backgroundImage = new StyleBackground(skyTextures[skyIndex]);
            mountainLayer.style.backgroundImage = new StyleBackground(mountainTextures[mountainIndex]);
            groundLayer.style.backgroundImage = new StyleBackground(groundTextures[groundIndex]);
        }
    }
}
