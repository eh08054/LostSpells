using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using LostSpells.Data;
using LostSpells.Systems;
using System.Collections.Generic;
using System.Linq;

namespace LostSpells.UI
{
    /// <summary>
    /// 인게임 UI - 챕터 정보 표시 및 게임 UI 관리
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InGameUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Button menuButton;
        private Button resumeButton;
        private Button settingsButton;
        private Button storeButton;
        private Button mainMenuButton;

        private Label chapterInfoLabel;
        private Label waveInfoLabel;
        private Label scoreDisplayLabel;
        private VisualElement menuPopup;
        private Label menuTitle;

        // 게임오버 팝업 관련
        private VisualElement gameOverPopup;
        private Label gameOverTitle;
        private Label scoreLabel;
        private Label scoreValue;
        private Button retryButton;
        private Button reviveButton;
        private Button gameOverMainMenuButton;

        // 부활 확인 팝업
        private VisualElement reviveConfirmPopup;
        private Label reviveConfirmTitle;
        private Label currentReviveStoneLabel;
        private Label currentReviveStoneValue;
        private Label afterReviveStoneLabel;
        private Label afterReviveStoneValue;
        private Button confirmReviveButton;
        private Button cancelReviveButton;

        // 부활석 부족 팝업
        private VisualElement insufficientReviveStonePopup;
        private Label insufficientTitle;
        private Label insufficientMessage;
        private Button confirmInsufficientButton;

        // 스테이지 클리어 팝업
        private VisualElement stageClearPopup;
        private Label stageClearTitle;
        private Label clearScoreValue;
        private Button nextChapterButton;
        private Button clearMainMenuButton;

        // 상점 팝업
        private VisualElement storePopup;
        private Button storeCloseButton;
        private Label storeDiamondCount;
        private Label storeReviveStoneCount;
        private Button storeDiamondButton;
        private Button storeReviveStoneButton;
        private VisualElement storeDiamondPanel;
        private VisualElement storeReviveStonePanel;
        private VisualElement storeDiamondGrid;
        private VisualElement storeReviveStoneGrid;

        // 옵션 팝업
        private VisualElement optionsPopup;
        private Button optionsCloseButton;
        private InGameOptionsPanelController optionsPanelController;

        // 상점이 게임오버에서 열렸는지 추적
        private bool storeOpenedFromGameOver = false;

        // 화폐 표시 Label
        private Label diamondCountLabel;
        private Label reviveStoneCountLabel;

        // 음성인식 상태 패널 (화면 하단)
        private VisualElement voiceStatusPanel;
        private Label voiceStatusText;

        // 플레이어 상태창
        private Label playerLevelLabel;
        private VisualElement expBar;
        private VisualElement hpBar;
        private Label hpText;
        private VisualElement mpBar;
        private Label mpText;

        // 사이드바 관련
        private VisualElement leftSidebar;
        private VisualElement rightSidebar;

        // 스킬창 관련 (피치 기반)
        private VisualElement skillPanel;
        private Button allSkillButton;
        private Button lowPitchSkillButton;
        private Button midPitchSkillButton;
        private Button highPitchSkillButton;
        private ScrollView allSkillScrollView;
        private ScrollView lowPitchSkillScrollView;
        private ScrollView midPitchSkillScrollView;
        private ScrollView highPitchSkillScrollView;
        private VisualElement allSkillList;
        private VisualElement lowPitchSkillList;
        private VisualElement midPitchSkillList;
        private VisualElement highPitchSkillList;

        private PitchCategory? currentPitchCategory = null; // null이면 All 탭

        private PlayerSaveData saveData;
        private EnemySpawner enemySpawner;
        private VoiceRecognitionManager voiceRecognitionManager;
        private LostSpells.Components.PlayerComponent playerComponent;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            // EnemySpawner 찾기
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

            // VoiceRecognitionManager 찾기
            voiceRecognitionManager = FindFirstObjectByType<VoiceRecognitionManager>();

            // PlayerComponent 찾기
            playerComponent = FindFirstObjectByType<LostSpells.Components.PlayerComponent>();
            if (playerComponent == null)
            {
                Debug.LogWarning("[InGameUI] PlayerComponent를 찾을 수 없습니다!");
            }

            // 첫 웨이브 시작
            if (enemySpawner != null)
            {
                int currentWave = GameStateManager.Instance.GetCurrentWave();
                enemySpawner.StartWave(currentWave);
            }
            else
            {
                Debug.LogWarning("EnemySpawner를 찾을 수 없습니다!");
            }

            // 스킬 목록 로드
            LoadSkills();

            // 로컬라이제이션 적용 (Start 이후에 호출하여 모든 UI가 준비된 후 적용)
            UpdateLocalization();

            // 초기 점수 표시
            UpdateScore();
        }

        private void Update()
        {
            // 키바인딩에서 스킬 패널 키 가져오기
            if (Keyboard.current != null)
            {
                Key skillPanelKey = GetSkillPanelKey();
                if (Keyboard.current[skillPanelKey].wasPressedThisFrame)
                {
                    ToggleSkillPanel();
                }
            }

            // 플레이어 상태 업데이트
            UpdatePlayerStats();
        }

        /// <summary>
        /// SaveData에서 스킬 패널 키 가져오기
        /// </summary>
        private Key GetSkillPanelKey()
        {
            if (saveData != null && saveData.keyBindings != null && saveData.keyBindings.ContainsKey("SkillPanel"))
            {
                string keyString = saveData.keyBindings["SkillPanel"];
                return ParseKey(keyString, Key.Tab);
            }

            // 기본값: Tab
            return Key.Tab;
        }

        /// <summary>
        /// 키 문자열을 Key enum으로 변환 (Options의 GetKeyDisplayName 역함수)
        /// </summary>
        private Key ParseKey(string keyString, Key defaultKey)
        {
            // 특수 키 매핑 (Options의 GetKeyDisplayName과 반대)
            switch (keyString)
            {
                case "Space": return Key.Space;
                case "LShift": return Key.LeftShift;
                case "RShift": return Key.RightShift;
                case "LCtrl": return Key.LeftCtrl;
                case "RCtrl": return Key.RightCtrl;
                case "LAlt": return Key.LeftAlt;
                case "RAlt": return Key.RightAlt;
                case "Tab": return Key.Tab;
                case "Enter": return Key.Enter;
                case "Backspace": return Key.Backspace;
                default:
                    // 일반 키는 Enum.TryParse 시도
                    if (System.Enum.TryParse<Key>(keyString, true, out Key key))
                    {
                        return key;
                    }
                    return defaultKey;
            }
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // UI 요소 찾기
            menuButton = root.Q<Button>("MenuButton");
            chapterInfoLabel = root.Q<Label>("ChapterInfo");
            waveInfoLabel = root.Q<Label>("WaveInfo");
            scoreDisplayLabel = root.Q<Label>("ScoreDisplay");
            menuPopup = root.Q<VisualElement>("MenuPopup");
            menuTitle = menuPopup?.Q<Label>(className: "menu-title");

            // 화폐 표시 Label
            diamondCountLabel = root.Q<Label>("DiamondCount");
            reviveStoneCountLabel = root.Q<Label>("ReviveStoneCount");

            // 음성인식 상태 패널 (화면 하단)
            voiceStatusPanel = root.Q<VisualElement>("VoiceStatusPanel");
            voiceStatusText = root.Q<Label>("VoiceStatusText");

            // 플레이어 상태창 UI 요소
            playerLevelLabel = root.Q<Label>("PlayerLevel");
            expBar = root.Q<VisualElement>("ExpBar");
            hpBar = root.Q<VisualElement>("HPBar");
            hpText = root.Q<Label>("HPText");
            mpBar = root.Q<VisualElement>("MPBar");
            mpText = root.Q<Label>("MPText");

            // 사이드바 UI 요소
            leftSidebar = root.Q<VisualElement>("LeftSidebar");
            rightSidebar = root.Q<VisualElement>("RightSidebar");

            // 스킬창 UI 요소 (피치 기반)
            skillPanel = root.Q<VisualElement>("SkillPanel");
            allSkillButton = root.Q<Button>("AllSkillButton");
            lowPitchSkillButton = root.Q<Button>("LowPitchSkillButton");
            midPitchSkillButton = root.Q<Button>("MidPitchSkillButton");
            highPitchSkillButton = root.Q<Button>("HighPitchSkillButton");

            // ScrollView 요소 (표시/숨김 제어용)
            allSkillScrollView = root.Q<ScrollView>("AllSkillScrollView");
            lowPitchSkillScrollView = root.Q<ScrollView>("LowPitchSkillScrollView");
            midPitchSkillScrollView = root.Q<ScrollView>("MidPitchSkillScrollView");
            highPitchSkillScrollView = root.Q<ScrollView>("HighPitchSkillScrollView");

            // 내부 스킬 리스트 (스킬 아이템 추가용)
            allSkillList = root.Q<VisualElement>("AllSkillList");
            lowPitchSkillList = root.Q<VisualElement>("LowPitchSkillList");
            midPitchSkillList = root.Q<VisualElement>("MidPitchSkillList");
            highPitchSkillList = root.Q<VisualElement>("HighPitchSkillList");

            // 스킬창 UI 요소들의 키보드 네비게이션 비활성화 (A/D 키가 캐릭터 이동만 담당하도록)
            DisableKeyboardNavigation();

            // 메뉴 팝업 버튼들
            resumeButton = root.Q<Button>("ResumeButton");
            settingsButton = root.Q<Button>("SettingsButton");
            storeButton = root.Q<Button>("StoreButton");
            mainMenuButton = root.Q<Button>("MainMenuButton");

            // 게임오버 팝업 UI 요소
            gameOverPopup = root.Q<VisualElement>("GameOverPopup");
            gameOverTitle = gameOverPopup?.Q<Label>(className: "menu-title");
            scoreLabel = root.Q<Label>("ScoreLabel");
            scoreValue = root.Q<Label>("ScoreValue");
            retryButton = root.Q<Button>("RetryButton");
            reviveButton = root.Q<Button>("ReviveButton");
            gameOverMainMenuButton = root.Q<Button>("GameOverMainMenuButton");

            // 부활 확인 팝업 UI 요소
            reviveConfirmPopup = root.Q<VisualElement>("ReviveConfirmPopup");
            reviveConfirmTitle = root.Q<Label>("ReviveConfirmTitle");
            currentReviveStoneLabel = root.Q<Label>("CurrentReviveStoneLabel");
            currentReviveStoneValue = root.Q<Label>("CurrentReviveStoneValue");
            afterReviveStoneLabel = root.Q<Label>("AfterReviveStoneLabel");
            afterReviveStoneValue = root.Q<Label>("AfterReviveStoneValue");
            confirmReviveButton = root.Q<Button>("ConfirmReviveButton");
            cancelReviveButton = root.Q<Button>("CancelReviveButton");

            // 부활석 부족 팝업 UI 요소
            insufficientReviveStonePopup = root.Q<VisualElement>("InsufficientReviveStonePopup");
            insufficientTitle = root.Q<Label>("InsufficientTitle");
            insufficientMessage = root.Q<Label>("InsufficientMessage");
            confirmInsufficientButton = root.Q<Button>("ConfirmInsufficientButton");

            // 스테이지 클리어 팝업 UI 요소
            stageClearPopup = root.Q<VisualElement>("StageClearPopup");
            stageClearTitle = root.Q<Label>("StageClearTitle");
            clearScoreValue = root.Q<Label>("ClearScoreValue");
            nextChapterButton = root.Q<Button>("NextChapterButton");
            clearMainMenuButton = root.Q<Button>("ClearMainMenuButton");

            // 상점 팝업 UI 요소 (공유 템플릿 사용)
            storePopup = root.Q<VisualElement>("StorePopup");
            storeCloseButton = root.Q<Button>("StoreCloseButton");
            storeDiamondCount = root.Q<Label>("HeaderDiamonds");
            storeReviveStoneCount = root.Q<Label>("HeaderReviveStones");
            storeDiamondButton = root.Q<Button>("DiamondButton");
            storeReviveStoneButton = root.Q<Button>("ReviveStoneButton");
            storeDiamondPanel = root.Q<VisualElement>("DiamondPanel");
            storeReviveStonePanel = root.Q<VisualElement>("ReviveStonePanel");
            storeDiamondGrid = root.Q<VisualElement>("DiamondProductGrid");
            storeReviveStoneGrid = root.Q<VisualElement>("ReviveStoneProductGrid");

            // 옵션 팝업 UI 요소
            optionsPopup = root.Q<VisualElement>("OptionsPopup");
            optionsCloseButton = root.Q<Button>("OptionsCloseButton");

            // 옵션 패널 컨트롤러 초기화 (coroutineRunner로 this 전달)
            if (optionsPopup != null)
            {
                optionsPanelController = new InGameOptionsPanelController(root, optionsPopup, this);
            }

            // 이벤트 등록
            if (menuButton != null)
                menuButton.clicked += OnMenuButtonClicked;

            if (resumeButton != null)
                resumeButton.clicked += OnResumeButtonClicked;

            if (settingsButton != null)
                settingsButton.clicked += OnSettingsButtonClicked;

            if (storeButton != null)
                storeButton.clicked += OnStoreButtonClicked;

            if (mainMenuButton != null)
                mainMenuButton.clicked += OnMainMenuButtonClicked;

            // 게임오버 팝업 버튼 이벤트
            if (retryButton != null)
                retryButton.clicked += OnRetryButtonClicked;

            if (reviveButton != null)
                reviveButton.clicked += OnReviveButtonClicked;

            if (gameOverMainMenuButton != null)
                gameOverMainMenuButton.clicked += OnGameOverMainMenuButtonClicked;

            // 부활 확인 팝업 버튼 이벤트
            if (confirmReviveButton != null)
                confirmReviveButton.clicked += OnConfirmReviveButtonClicked;

            if (cancelReviveButton != null)
                cancelReviveButton.clicked += OnCancelReviveButtonClicked;

            // 부활석 부족 팝업 버튼 이벤트
            if (confirmInsufficientButton != null)
                confirmInsufficientButton.clicked += OnConfirmInsufficientButtonClicked;

            // 스테이지 클리어 팝업 버튼 이벤트
            if (nextChapterButton != null)
                nextChapterButton.clicked += OnNextChapterButtonClicked;
            if (clearMainMenuButton != null)
                clearMainMenuButton.clicked += OnClearMainMenuButtonClicked;

            // 상점 팝업 버튼 이벤트
            if (storeCloseButton != null)
                storeCloseButton.clicked += OnStoreCloseButtonClicked;

            if (storeDiamondButton != null)
                storeDiamondButton.clicked += OnStoreDiamondButtonClicked;

            if (storeReviveStoneButton != null)
                storeReviveStoneButton.clicked += OnStoreReviveStoneButtonClicked;

            // 옵션 팝업 버튼 이벤트
            if (optionsCloseButton != null)
                optionsCloseButton.clicked += OnOptionsCloseButtonClicked;

            // 스킬 카테고리 버튼 이벤트 (피치 기반)
            if (allSkillButton != null)
                allSkillButton.clicked += () => SwitchPitchCategory(null); // null = All

            if (lowPitchSkillButton != null)
                lowPitchSkillButton.clicked += () => SwitchPitchCategory(PitchCategory.Low);

            if (midPitchSkillButton != null)
                midPitchSkillButton.clicked += () => SwitchPitchCategory(PitchCategory.Medium);

            if (highPitchSkillButton != null)
                highPitchSkillButton.clicked += () => SwitchPitchCategory(PitchCategory.High);

            // 저장 데이터 가져오기
            saveData = SaveManager.Instance.GetCurrentSaveData();

            // 챕터 정보 표시
            UpdateChapterInfo();

            // 화폐 정보 표시
            UpdateCurrencyDisplay();

            // Localization 이벤트 등록 (실제 적용은 Start에서)
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;
        }

        private void OnDisable()
        {
            if (menuButton != null)
                menuButton.clicked -= OnMenuButtonClicked;

            if (resumeButton != null)
                resumeButton.clicked -= OnResumeButtonClicked;

            if (settingsButton != null)
                settingsButton.clicked -= OnSettingsButtonClicked;

            if (storeButton != null)
                storeButton.clicked -= OnStoreButtonClicked;

            if (mainMenuButton != null)
                mainMenuButton.clicked -= OnMainMenuButtonClicked;

            if (allSkillButton != null)
                allSkillButton.clicked -= () => SwitchPitchCategory(null);

            if (lowPitchSkillButton != null)
                lowPitchSkillButton.clicked -= () => SwitchPitchCategory(PitchCategory.Low);

            if (midPitchSkillButton != null)
                midPitchSkillButton.clicked -= () => SwitchPitchCategory(PitchCategory.Medium);

            if (highPitchSkillButton != null)
                highPitchSkillButton.clicked -= () => SwitchPitchCategory(PitchCategory.High);

            // Localization 이벤트 해제
            UnregisterLocalizationEvents();
        }

        private void OnDestroy()
        {
            UnregisterLocalizationEvents();
        }

        private void UnregisterLocalizationEvents()
        {
            if (LocalizationManager.Instance != null)
            {
                try
                {
                    LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalization;
                }
                catch (System.Exception)
                {
                    // 이미 해제된 경우 무시
                }
            }
        }

        // 현재 활성화된 피치별 속성 (태그 표시용)
        private HashSet<string> activePitchElements = new HashSet<string>();

        /// <summary>
        /// 스킬 목록 로드 (피치 기반 필터링)
        /// </summary>
        private void LoadSkills()
        {
            // DataManager에서 통합된 스킬 데이터 가져오기
            List<SkillData> allSkills = DataManager.Instance.GetAllSkillData();
            if (allSkills == null)
            {
                allSkills = new List<SkillData>();
            }

            if (allSkills.Count == 0)
            {
                Debug.LogWarning("[InGameUI] 스킬 데이터가 없습니다!");
                return;
            }

            // 피치별 속성 가져오기
            string lowElement = saveData?.lowPitchElement ?? "Fire";
            string midElement = saveData?.midPitchElement ?? "Ice";
            string highElement = saveData?.highPitchElement ?? "Electric";

            // 활성화된 속성 목록 업데이트 (태그 표시용)
            activePitchElements.Clear();
            activePitchElements.Add(lowElement);
            activePitchElements.Add(midElement);
            activePitchElements.Add(highElement);

            // 피치별로 사용 가능한 스킬 필터링
            var lowPitchSkills = GetSkillsForElement(allSkills, lowElement);
            var midPitchSkills = GetSkillsForElement(allSkills, midElement);
            var highPitchSkills = GetSkillsForElement(allSkills, highElement);

            // 전체 탭: 활성 피치 속성 중 하나라도 있는 스킬 (중복 없이)
            var allAvailableSkills = GetSkillsForActiveElements(allSkills);
            PopulateAllSkillList(allSkillList, allAvailableSkills);

            // 각 피치별 리스트 채우기
            PopulateSkillListWithElement(lowPitchSkillList, lowPitchSkills, lowElement);
            PopulateSkillListWithElement(midPitchSkillList, midPitchSkills, midElement);
            PopulateSkillListWithElement(highPitchSkillList, highPitchSkills, highElement);

            // 탭 버튼에 속성 색상 적용
            UpdatePitchTabColors(lowElement, midElement, highElement);

            // 초기 카테고리 표시 (All 탭)
            SwitchPitchCategory(currentPitchCategory);
        }

        /// <summary>
        /// 피치 탭 버튼에 속성별 색상 적용
        /// </summary>
        private void UpdatePitchTabColors(string lowElement, string midElement, string highElement)
        {
            // 모든 속성 클래스 제거 후 재적용
            string[] elementClasses = { "element-fire", "element-ice", "element-electric", "element-earth", "element-holy", "element-void" };

            // 저(Low) 탭
            if (lowPitchSkillButton != null)
            {
                foreach (var cls in elementClasses)
                    lowPitchSkillButton.RemoveFromClassList(cls);
                lowPitchSkillButton.AddToClassList($"element-{lowElement.ToLower()}");
            }

            // 중(Mid) 탭
            if (midPitchSkillButton != null)
            {
                foreach (var cls in elementClasses)
                    midPitchSkillButton.RemoveFromClassList(cls);
                midPitchSkillButton.AddToClassList($"element-{midElement.ToLower()}");
            }

            // 고(High) 탭
            if (highPitchSkillButton != null)
            {
                foreach (var cls in elementClasses)
                    highPitchSkillButton.RemoveFromClassList(cls);
                highPitchSkillButton.AddToClassList($"element-{highElement.ToLower()}");
            }
        }

        /// <summary>
        /// 활성화된 피치 속성 중 하나라도 있는 스킬 필터링 (중복 없이)
        /// </summary>
        private List<SkillData> GetSkillsForActiveElements(List<SkillData> allSkills)
        {
            var result = new List<SkillData>();

            foreach (var skill in allSkills)
            {
                // 제네릭 스킬인 경우 활성 속성 중 하나라도 있는지 확인
                if (skill.isGenericSkill && skill.elementVariants != null)
                {
                    foreach (var activeElem in activePitchElements)
                    {
                        if (skill.elementVariants.ContainsKey(activeElem))
                        {
                            result.Add(skill);
                            break; // 하나만 있어도 추가 (중복 방지)
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 속성에 대해 사용 가능한 스킬 필터링
        /// </summary>
        private List<SkillData> GetSkillsForElement(List<SkillData> allSkills, string element)
        {
            var result = new List<SkillData>();

            foreach (var skill in allSkills)
            {
                // 일반 스킬이 아니면 무조건 포함 (속성 무관)
                if (!skill.isGenericSkill)
                {
                    result.Add(skill);
                    continue;
                }

                // 일반 스킬인 경우 해당 속성의 변형이 있는지 확인
                if (skill.elementVariants != null && skill.elementVariants.ContainsKey(element))
                {
                    result.Add(skill);
                }
            }

            return result;
        }

        /// <summary>
        /// 속성의 현지화된 이름 반환
        /// </summary>
        private string GetElementLocalizedName(string element)
        {
            var loc = LocalizationManager.Instance;
            switch (element)
            {
                case "Fire": return loc.GetText("element_fire");
                case "Ice": return loc.GetText("element_ice");
                case "Electric": return loc.GetText("element_electric");
                case "Earth": return loc.GetText("element_earth");
                case "Holy": return loc.GetText("element_holy");
                case "Void": return loc.GetText("element_void");
                default: return element;
            }
        }

        /// <summary>
        /// 속성의 짧은 이름 반환 (태그 표시용)
        /// </summary>
        private string GetElementShortName(string element)
        {
            var loc = LocalizationManager.Instance;
            bool isKorean = loc.CurrentLanguage == LostSpells.Systems.Language.Korean;

            switch (element)
            {
                case "Fire": return isKorean ? "화" : "F";
                case "Ice": return isKorean ? "빙" : "I";
                case "Electric": return isKorean ? "뇌" : "E";
                case "Earth": return isKorean ? "지" : "G";
                case "Holy": return isKorean ? "성" : "H";
                case "Void": return isKorean ? "암" : "V";
                default: return element.Substring(0, 1);
            }
        }

        /// <summary>
        /// All 탭 UI 생성 (단순 목록)
        /// </summary>
        private void PopulateAllSkillList(VisualElement container, List<SkillData> skills)
        {
            if (container == null) return;

            container.Clear();

            foreach (var skill in skills)
            {
                AddSkillItemForAllTab(container, skill);
            }
        }

        /// <summary>
        /// 피치별 스킬 리스트 UI 생성 (속성 정보 포함)
        /// </summary>
        private void PopulateSkillListWithElement(VisualElement container, List<SkillData> skills, string element)
        {
            if (container == null) return;

            container.Clear();

            foreach (var skill in skills)
            {
                AddSkillItemWithElement(container, skill, element);
            }
        }

        /// <summary>
        /// 전체 탭용 스킬 아이템 추가 (활성 피치 속성만 태그로 표시)
        /// </summary>
        private void AddSkillItemForAllTab(VisualElement container, SkillData skill)
        {
            var skillItem = new VisualElement();
            skillItem.AddToClassList("skill-item");

            string displayName = skill.GetLocalizedName();
            var skillName = new Label(displayName);
            skillName.AddToClassList("skill-name");

            // 속성 태그 컨테이너 (활성화된 피치 속성만 표시)
            var elementsContainer = new VisualElement();
            elementsContainer.AddToClassList("skill-elements");

            if (skill.isGenericSkill && skill.elementVariants != null)
            {
                // 활성화된 피치 속성 중 이 스킬이 가진 속성만 표시
                string[] elementOrder = { "Fire", "Ice", "Electric", "Earth", "Holy", "Void" };
                foreach (var elem in elementOrder)
                {
                    if (activePitchElements.Contains(elem) && skill.elementVariants.ContainsKey(elem))
                    {
                        var tag = new Label(GetElementShortName(elem));
                        tag.AddToClassList("element-tag");
                        tag.AddToClassList($"element-tag-{elem.ToLower()}");
                        elementsContainer.Add(tag);
                    }
                }
            }

            skillItem.Add(skillName);
            skillItem.Add(elementsContainer);
            container.Add(skillItem);

            // 클릭 이벤트 - 전체 탭에서는 랜덤으로 활성 속성 선택
            var capturedSkill = skill;
            skillItem.RegisterCallback<ClickEvent>(evt => {
                string randomElement = GetRandomActiveElementForSkill(capturedSkill);
                OnSkillItemClickedWithElement(capturedSkill, randomElement);
            });
        }

        /// <summary>
        /// 스킬에 대해 랜덤으로 활성 피치 속성 반환 (전체 탭용)
        /// </summary>
        private string GetRandomActiveElementForSkill(SkillData skill)
        {
            if (skill.isGenericSkill && skill.elementVariants != null)
            {
                // 이 스킬이 가진 활성 속성 목록 수집
                var availableElements = new List<string>();
                foreach (var activeElem in activePitchElements)
                {
                    if (skill.elementVariants.ContainsKey(activeElem))
                    {
                        availableElements.Add(activeElem);
                    }
                }

                // 랜덤 선택
                if (availableElements.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, availableElements.Count);
                    return availableElements[randomIndex];
                }
            }
            return "Fire"; // 기본값
        }

        /// <summary>
        /// 피치별 탭용 스킬 아이템 추가 (해당 속성 태그만 표시)
        /// </summary>
        private void AddSkillItemWithElement(VisualElement container, SkillData skill, string element)
        {
            var skillItem = new VisualElement();
            skillItem.AddToClassList("skill-item");

            string displayName = skill.GetLocalizedName();
            var skillName = new Label(displayName);
            skillName.AddToClassList("skill-name");

            // 속성 태그 컨테이너 (해당 피치의 속성만 표시)
            var elementsContainer = new VisualElement();
            elementsContainer.AddToClassList("skill-elements");

            if (skill.isGenericSkill && skill.elementVariants != null && skill.elementVariants.ContainsKey(element))
            {
                var tag = new Label(GetElementShortName(element));
                tag.AddToClassList("element-tag");
                tag.AddToClassList($"element-tag-{element.ToLower()}");
                elementsContainer.Add(tag);
            }

            skillItem.Add(skillName);
            skillItem.Add(elementsContainer);
            container.Add(skillItem);

            // 클릭 이벤트 추가 - 스킬 발사 (속성 정보 포함)
            var capturedSkill = skill;
            var capturedElement = element;
            skillItem.RegisterCallback<ClickEvent>(evt => OnSkillItemClickedWithElement(capturedSkill, capturedElement));
        }

        /// <summary>
        /// 스킬 아이템 클릭 시 스킬 발사 (속성 정보 포함)
        /// </summary>
        private void OnSkillItemClickedWithElement(SkillData skill, string element)
        {
            Debug.Log($"[InGameUI] 스킬 클릭: {skill?.skillId}, 속성: {element}");

            if (playerComponent == null)
            {
                Debug.LogWarning("[InGameUI] PlayerComponent를 찾을 수 없습니다!");
                // playerComponent 다시 찾기 시도
                playerComponent = FindFirstObjectByType<LostSpells.Components.PlayerComponent>();
                if (playerComponent == null)
                {
                    Debug.LogError("[InGameUI] PlayerComponent를 찾을 수 없습니다! (재시도 실패)");
                    return;
                }
            }

            // 일반 스킬인 경우 속성 변형으로 발사
            if (skill.isGenericSkill && skill.elementVariants != null && skill.elementVariants.ContainsKey(element))
            {
                var variant = skill.elementVariants[element];
                Debug.Log($"[InGameUI] 속성 변형 스킬 발사: {variant.name}, 프리팹: {variant.effectPrefab}");
                bool success = playerComponent.CastSkillByDataWithVariant(skill, variant);
                Debug.Log($"[InGameUI] 스킬 발사 결과: {success}");
            }
            else
            {
                // 일반 스킬이 아니면 기본 발사
                Debug.Log($"[InGameUI] 기본 스킬 발사: {skill.skillId}");
                bool success = playerComponent.CastSkillByData(skill);
                Debug.Log($"[InGameUI] 기본 스킬 발사 결과: {success}");
            }
        }

        /// <summary>
        /// Tab으로 스킬창 토글
        /// </summary>
        private void ToggleSkillPanel()
        {
            if (rightSidebar != null)
            {
                bool isHidden = rightSidebar.ClassListContains("sidebar-hidden");

                if (isHidden)
                {
                    rightSidebar.RemoveFromClassList("sidebar-hidden");
                }
                else
                {
                    rightSidebar.AddToClassList("sidebar-hidden");
                }
            }
        }

        /// <summary>
        /// 피치 카테고리 전환
        /// </summary>
        private void SwitchPitchCategory(PitchCategory? category)
        {
            currentPitchCategory = category;

            // 모든 ScrollView 숨기기
            if (allSkillScrollView != null)
                allSkillScrollView.style.display = DisplayStyle.None;
            if (lowPitchSkillScrollView != null)
                lowPitchSkillScrollView.style.display = DisplayStyle.None;
            if (midPitchSkillScrollView != null)
                midPitchSkillScrollView.style.display = DisplayStyle.None;
            if (highPitchSkillScrollView != null)
                highPitchSkillScrollView.style.display = DisplayStyle.None;

            // 선택된 카테고리만 표시
            if (category == null)
            {
                // All 탭
                if (allSkillScrollView != null)
                    allSkillScrollView.style.display = DisplayStyle.Flex;
            }
            else
            {
                switch (category.Value)
                {
                    case PitchCategory.Low:
                        if (lowPitchSkillScrollView != null)
                            lowPitchSkillScrollView.style.display = DisplayStyle.Flex;
                        break;
                    case PitchCategory.Medium:
                        if (midPitchSkillScrollView != null)
                            midPitchSkillScrollView.style.display = DisplayStyle.Flex;
                        break;
                    case PitchCategory.High:
                        if (highPitchSkillScrollView != null)
                            highPitchSkillScrollView.style.display = DisplayStyle.Flex;
                        break;
                }
            }

            // 음성인식 매니저에 현재 카테고리 스킬 전달
            UpdateVoiceRecognitionSkills();

            // 음성인식 매니저에 고정 속성 모드 설정
            UpdateVoiceRecognitionFixedElement(category);
        }

        /// <summary>
        /// 음성인식 매니저에 고정 속성 모드 설정
        /// </summary>
        private void UpdateVoiceRecognitionFixedElement(PitchCategory? category)
        {
            if (voiceRecognitionManager == null) return;

            string element = null;
            if (category != null)
            {
                element = GetElementForCurrentPitchCategory();
            }

            voiceRecognitionManager.SetFixedElement(category, element);
        }

        /// <summary>
        /// 현재 선택된 카테고리의 스킬만 음성인식 대상으로 설정
        /// </summary>
        private void UpdateVoiceRecognitionSkills()
        {
            if (voiceRecognitionManager == null)
            {
                // VoiceRecognitionManager가 없어도 게임 플레이는 가능
                return;
            }

            var currentSkills = GetCurrentCategorySkills();
            if (currentSkills != null && currentSkills.Count > 0)
            {
                voiceRecognitionManager.SetActiveSkills(currentSkills);
            }
        }

        /// <summary>
        /// 현재 카테고리의 스킬 목록 가져오기
        /// </summary>
        public List<SkillData> GetCurrentCategorySkills()
        {
            // PlayerComponent에서 스킬 가져오기 (UI에 표시된 스킬과 동일하게)
            List<SkillData> allSkills = new List<SkillData>();

            if (playerComponent != null)
            {
                var playerSkills = playerComponent.GetAllSkills();
                if (playerSkills != null)
                {
                    foreach (var skill in playerSkills)
                    {
                        if (skill != null)
                        {
                            allSkills.Add(skill);
                        }
                    }
                }
            }

            // PlayerComponent에 스킬이 없으면 DataManager에서 가져오기 (폴백)
            if (allSkills.Count == 0)
            {
                var dataManagerSkills = DataManager.Instance.GetAllSkillData();
                if (dataManagerSkills != null)
                {
                    allSkills = dataManagerSkills;
                }
            }

            if (allSkills.Count == 0) return new List<SkillData>();

            // All 탭 (null)이면 모든 스킬 반환
            if (currentPitchCategory == null)
                return allSkills;

            // 피치별 속성 가져와서 해당 속성의 스킬만 반환
            string element = GetElementForCurrentPitchCategory();
            return GetSkillsForElement(allSkills, element);
        }

        /// <summary>
        /// 현재 피치 카테고리에 해당하는 속성 반환
        /// </summary>
        private string GetElementForCurrentPitchCategory()
        {
            if (currentPitchCategory == null) return "Fire";

            switch (currentPitchCategory.Value)
            {
                case PitchCategory.Low:
                    return saveData?.lowPitchElement ?? "Fire";
                case PitchCategory.Medium:
                    return saveData?.midPitchElement ?? "Ice";
                case PitchCategory.High:
                    return saveData?.highPitchElement ?? "Electric";
                default:
                    return "Fire";
            }
        }

        /// <summary>
        /// 음성인식 결과 표시 (VoiceRecognitionManager에서 호출) - 하단 패널로 이동하여 더 이상 사용하지 않음
        /// </summary>
        public void UpdateVoiceRecognitionDisplay(string message)
        {
            // 스킬창 내 음성인식 결과 표시 제거됨 - 하단 VoiceStatusPanel 사용
        }

        /// <summary>
        /// 음성인식 상태 패널 업데이트 (명령어만 표시)
        /// </summary>
        public void UpdateVoiceStatusPanel(string recognizedText, string executedCommand)
        {
            bool hasContent = !string.IsNullOrEmpty(executedCommand);

            if (voiceStatusPanel != null)
            {
                voiceStatusPanel.style.display = hasContent ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (voiceStatusText != null && hasContent)
            {
                voiceStatusText.text = executedCommand;
            }
        }

        /// <summary>
        /// 챕터 정보 업데이트
        /// </summary>
        private void UpdateChapterInfo()
        {
            // GameStateManager에서 현재 챕터 정보 가져오기
            ChapterData currentChapter = GameStateManager.Instance.GetCurrentChapterData();

            if (currentChapter != null && chapterInfoLabel != null)
            {
                string chapterText = LocalizationManager.Instance.GetText("ingame_chapter");
                string chapterName = currentChapter.GetLocalizedName();
                chapterInfoLabel.text = $"{chapterText} {currentChapter.chapterId} - {chapterName}";
            }
            else if (chapterInfoLabel != null)
            {
                // 챕터 정보가 없으면 기본값
                string chapterText = LocalizationManager.Instance.GetText("ingame_chapter");
                chapterInfoLabel.text = $"{chapterText} 0 - Tutorial";
            }

            // 웨이브 정보 업데이트
            UpdateWaveInfo();
        }

        /// <summary>
        /// 웨이브 정보 업데이트
        /// </summary>
        public void UpdateWaveInfo()
        {
            if (waveInfoLabel != null)
            {
                int currentWave = GameStateManager.Instance.GetCurrentWave();
                string waveText = LocalizationManager.Instance.GetText("ingame_wave");
                waveInfoLabel.text = $"{waveText} {currentWave}";
            }
        }

        /// <summary>
        /// 화폐 정보 업데이트
        /// </summary>
        public void UpdateCurrencyDisplay()
        {
            if (saveData != null)
            {
                if (diamondCountLabel != null)
                    diamondCountLabel.text = saveData.diamonds.ToString();

                if (reviveStoneCountLabel != null)
                    reviveStoneCountLabel.text = saveData.reviveStones.ToString();
            }
        }

        /// <summary>
        /// 웨이브 변경 시 호출 (게임 로직에서 호출)
        /// </summary>
        public void SetWave(int waveNumber)
        {
            GameStateManager.Instance.SetCurrentWave(waveNumber);
            UpdateWaveInfo();
        }

        /// <summary>
        /// 다음 웨이브 시작
        /// </summary>
        public void StartNextWave()
        {
            if (enemySpawner != null)
            {
                int currentWave = GameStateManager.Instance.GetCurrentWave();
                int nextWave = currentWave + 1;

                GameStateManager.Instance.SetCurrentWave(nextWave);
                UpdateWaveInfo();

                enemySpawner.StartWave(nextWave);
            }
        }

        private void OnMenuButtonClicked()
        {
            if (menuPopup != null)
            {
                // 메뉴 팝업 토글
                bool isVisible = menuPopup.style.display == DisplayStyle.Flex;

                if (isVisible)
                {
                    // 메뉴 닫기: 팝업 숨기고 사이드바 표시, 게임 재개
                    menuPopup.style.display = DisplayStyle.None;
                    ShowSidebars();
                    ResumeGame();
                }
                else
                {
                    // 메뉴 열기: 팝업 표시하고 사이드바 숨기기, 게임 일시정지
                    menuPopup.style.display = DisplayStyle.Flex;
                    HideSidebars();
                    PauseGame();
                }
            }
        }

        private void OnResumeButtonClicked()
        {
            if (menuPopup != null)
            {
                // 메뉴 닫기 및 게임 재개
                menuPopup.style.display = DisplayStyle.None;
                ShowSidebars();
                ResumeGame();
            }
        }

        /// <summary>
        /// 게임 일시정지
        /// </summary>
        private void PauseGame()
        {
            Time.timeScale = 0f;
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        private void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        /// <summary>
        /// 사이드바 숨기기
        /// </summary>
        private void HideSidebars()
        {
            if (leftSidebar != null)
                leftSidebar.style.display = DisplayStyle.None;

            if (rightSidebar != null)
                rightSidebar.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 사이드바 표시
        /// </summary>
        private void ShowSidebars()
        {
            if (leftSidebar != null)
                leftSidebar.style.display = DisplayStyle.Flex;

            if (rightSidebar != null)
                rightSidebar.style.display = DisplayStyle.Flex;
        }

        private void OnSettingsButtonClicked()
        {
            // 메뉴 팝업 닫기
            if (menuPopup != null)
                menuPopup.style.display = DisplayStyle.None;

            // 옵션 팝업 표시
            ShowOptionsPopup();
        }

        /// <summary>
        /// 옵션 팝업 표시
        /// </summary>
        private void ShowOptionsPopup()
        {
            if (optionsPopup != null)
            {
                optionsPopup.style.display = DisplayStyle.Flex;
                optionsPanelController?.OnPanelShown();
                optionsPanelController?.UpdateLocalization(LocalizationManager.Instance);
            }
        }

        /// <summary>
        /// 옵션 팝업 숨기기
        /// </summary>
        private void HideOptionsPopup()
        {
            if (optionsPopup != null)
            {
                optionsPopup.style.display = DisplayStyle.None;
                optionsPanelController?.OnPanelHidden();
            }
        }

        /// <summary>
        /// 옵션 닫기 버튼 클릭
        /// </summary>
        private void OnOptionsCloseButtonClicked()
        {
            CloseOptionsPopup();
        }

        /// <summary>
        /// 옵션 팝업 열기 (외부에서 호출용)
        /// </summary>
        public void OpenOptionsPopup()
        {
            // 메뉴 팝업이 열려있으면 닫기
            if (menuPopup != null)
                menuPopup.style.display = DisplayStyle.None;

            HideSidebars();
            ShowOptionsPopup();
        }

        /// <summary>
        /// 옵션 팝업 닫기 (외부에서 호출용)
        /// </summary>
        public void CloseOptionsPopup()
        {
            HideOptionsPopup();
            ShowSidebars();
            ResumeGame();

            // 언어가 변경되었을 수 있으므로 UI 업데이트
            UpdateLocalization();
        }

        /// <summary>
        /// 옵션 팝업이 열려있는지 확인
        /// </summary>
        public bool IsOptionsPopupVisible()
        {
            return optionsPopup != null && optionsPopup.style.display == DisplayStyle.Flex;
        }

        /// <summary>
        /// InGameOptionsPanelController 반환 (음성 명령용)
        /// </summary>
        public InGameOptionsPanelController GetOptionsPanelController()
        {
            return optionsPanelController;
        }

        /// <summary>
        /// 상점 팝업 열기 (외부에서 호출용)
        /// </summary>
        public void OpenStorePopup()
        {
            // 메뉴 팝업이 열려있으면 닫기
            if (menuPopup != null)
                menuPopup.style.display = DisplayStyle.None;

            HideSidebars();
            PauseGame();
            ShowStorePopupFromMenu();
        }

        /// <summary>
        /// 상점 팝업 닫기 (외부에서 호출용)
        /// </summary>
        public void CloseStorePopup()
        {
            if (storePopup != null)
                storePopup.style.display = DisplayStyle.None;

            ShowSidebars();
            ResumeGame();
            UpdateCurrencyDisplay();
        }

        /// <summary>
        /// 상점 팝업이 열려있는지 확인
        /// </summary>
        public bool IsStorePopupVisible()
        {
            return storePopup != null && storePopup.style.display == DisplayStyle.Flex;
        }

        private void OnStoreButtonClicked()
        {
            // 메뉴 팝업 닫기
            if (menuPopup != null)
                menuPopup.style.display = DisplayStyle.None;

            // 내장 상점 팝업 표시
            ShowStorePopupFromMenu();
        }

        /// <summary>
        /// 메뉴에서 상점 팝업 표시
        /// </summary>
        private void ShowStorePopupFromMenu()
        {
            if (storePopup != null)
            {
                storePopup.style.display = DisplayStyle.Flex;

                // 화폐 표시 업데이트
                UpdateStoreCurrencyDisplay();

                // 상품 목록 생성 (처음 한번만)
                if (storeDiamondGrid != null && storeDiamondGrid.childCount == 0)
                {
                    PopulateStoreProducts();
                }

                // 다이아몬드 탭 기본 표시
                ShowStoreDiamondPanel();
            }
        }

        private void OnMainMenuButtonClicked()
        {
            // 게임 시간 복구 및 게임 상태 초기화
            Time.timeScale = 1f;
            GameStateManager.Instance.ResetGameState();
            SceneManager.LoadScene("Menu");
        }

        /// <summary>
        /// 플레이어 상태 업데이트 (레벨, 경험치, HP, MP)
        /// </summary>
        private void UpdatePlayerStats()
        {
            if (saveData == null || playerComponent == null) return;

            // 레벨 표시
            if (playerLevelLabel != null)
            {
                playerLevelLabel.text = saveData.level.ToString();
            }

            // 경험치 바 (TODO: SaveData에 경험치 정보 추가 필요, 일단 0%로 표시)
            if (expBar != null)
            {
                // 경험치 진행률 계산 (임시로 0%)
                float expPercent = 0f;
                var style = expBar.style;
                style.height = new StyleLength(new Length(expPercent, LengthUnit.Percent));
            }

            // HP 바
            if (hpBar != null && hpText != null)
            {
                int currentHP = playerComponent.GetCurrentHealth();
                int maxHP = playerComponent.GetMaxHealth();
                float hpPercent = (float)currentHP / maxHP;

                // HP 바 너비 조정
                var style = hpBar.style;
                style.width = new StyleLength(new Length(hpPercent * 100f, LengthUnit.Percent));

                // HP 텍스트 업데이트
                hpText.text = $"{currentHP} / {maxHP}";
            }

            // MP 바
            if (mpBar != null && mpText != null)
            {
                int currentMP = playerComponent.GetCurrentMana();
                int maxMP = playerComponent.GetMaxMana();
                float mpPercent = (float)currentMP / maxMP;

                // MP 바 너비 조정
                var style = mpBar.style;
                style.width = new StyleLength(new Length(mpPercent * 100f, LengthUnit.Percent));

                // MP 텍스트 업데이트
                mpText.text = $"{currentMP} / {maxMP}";
            }
        }

        /// <summary>
        /// 로컬라이제이션 업데이트
        /// </summary>
        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            // 메뉴 팝업 제목 로컬라이즈
            if (menuTitle != null)
                menuTitle.text = loc.GetText("ingame_pause");

            // 메뉴 팝업 버튼들 로컬라이즈
            if (resumeButton != null)
                resumeButton.text = loc.GetText("ingame_resume");

            if (mainMenuButton != null)
                mainMenuButton.text = loc.GetText("ingame_quit");

            if (settingsButton != null)
                settingsButton.text = loc.GetText("main_menu_options");

            if (storeButton != null)
                storeButton.text = loc.GetText("main_menu_store");

            // 게임오버 팝업 로컬라이즈
            if (gameOverTitle != null)
                gameOverTitle.text = loc.GetText("gameover_title");

            if (scoreLabel != null)
                scoreLabel.text = loc.GetText("gameover_score");

            if (retryButton != null)
                retryButton.text = loc.GetText("gameover_retry");

            if (reviveButton != null)
                reviveButton.text = loc.GetText("gameover_revive");

            if (gameOverMainMenuButton != null)
                gameOverMainMenuButton.text = loc.GetText("gameover_quit");

            // 부활 확인 팝업 로컬라이즈
            if (reviveConfirmTitle != null)
                reviveConfirmTitle.text = loc.GetText("revive_confirm_title");

            if (currentReviveStoneLabel != null)
                currentReviveStoneLabel.text = loc.GetText("revive_current_stones");

            if (afterReviveStoneLabel != null)
                afterReviveStoneLabel.text = loc.GetText("revive_after_stones");

            if (confirmReviveButton != null)
                confirmReviveButton.text = loc.GetText("revive_confirm_button");

            if (cancelReviveButton != null)
                cancelReviveButton.text = loc.GetText("revive_cancel_button");

            // 부활석 부족 팝업 로컬라이즈
            if (insufficientTitle != null)
                insufficientTitle.text = loc.GetText("insufficient_revive_title");

            if (insufficientMessage != null)
                insufficientMessage.text = loc.GetText("insufficient_revive_message");

            if (confirmInsufficientButton != null)
                confirmInsufficientButton.text = loc.GetText("insufficient_confirm");

            // 스킬 카테고리 버튼 텍스트 업데이트 (피치 기반)
            if (allSkillButton != null)
                allSkillButton.text = loc.GetText("skill_category_all");

            if (lowPitchSkillButton != null)
                lowPitchSkillButton.text = loc.GetText("pitch_low");

            if (midPitchSkillButton != null)
                midPitchSkillButton.text = loc.GetText("pitch_mid");

            if (highPitchSkillButton != null)
                highPitchSkillButton.text = loc.GetText("pitch_high");

            // 챕터 및 웨이브 정보 업데이트
            UpdateChapterInfo();
            UpdateWaveInfo();

            // 스킬 목록 다시 로드 (스킬 이름이 언어별로 다름)
            LoadSkills();
        }

        /// <summary>
        /// 게임오버 팝업 표시
        /// </summary>
        public void ShowGameOver()
        {
            if (gameOverPopup != null)
            {
                // 최종 점수 표시
                if (scoreValue != null && GameStateManager.Instance != null)
                {
                    scoreValue.text = GameStateManager.Instance.GetScore().ToString();
                }

                gameOverPopup.style.display = DisplayStyle.Flex;
                Time.timeScale = 0; // 게임 일시정지
            }
        }

        /// <summary>
        /// 점수 업데이트
        /// </summary>
        public void UpdateScore()
        {
            if (GameStateManager.Instance != null)
            {
                int score = GameStateManager.Instance.GetScore();

                // 게임오버 팝업 점수 업데이트
                if (scoreValue != null)
                {
                    scoreValue.text = score.ToString();
                }

                // HUD 점수 표시 업데이트
                if (scoreDisplayLabel != null)
                {
                    string scoreText = LocalizationManager.Instance?.GetText("ingame_score") ?? "점수";
                    scoreDisplayLabel.text = $"{scoreText}: {score}";
                }
            }
        }

        /// <summary>
        /// 스테이지 클리어 팝업 표시
        /// </summary>
        public void ShowStageClear()
        {
            if (stageClearPopup != null)
            {
                // 최종 점수 표시
                if (clearScoreValue != null && GameStateManager.Instance != null)
                {
                    clearScoreValue.text = GameStateManager.Instance.GetScore().ToString();
                }

                stageClearPopup.style.display = DisplayStyle.Flex;
                Time.timeScale = 0; // 게임 일시정지
            }
        }

        /// <summary>
        /// 다음 챕터 버튼 클릭
        /// </summary>
        private void OnNextChapterButtonClicked()
        {
            Time.timeScale = 1; // 게임 재개

            // 다음 챕터로 이동
            if (GameStateManager.Instance != null)
            {
                int currentChapter = GameStateManager.Instance.GetCurrentChapterId();
                int nextChapter = currentChapter + 1;

                // 최대 챕터 체크 (12챕터까지)
                if (nextChapter > 12)
                {
                    // 모든 챕터 클리어 - 메인 메뉴로
                    SceneManager.LoadScene("Menu");
                }
                else
                {
                    // 다음 챕터 시작
                    GameStateManager.Instance.StartChapter(nextChapter);
                    SceneManager.LoadScene("InGame");
                }
            }
            else
            {
                SceneManager.LoadScene("Menu");
            }
        }

        /// <summary>
        /// 클리어 후 메인 메뉴 버튼 클릭
        /// </summary>
        private void OnClearMainMenuButtonClicked()
        {
            Time.timeScale = 1; // 게임 재개
            SceneManager.LoadScene("Menu");
        }

        /// <summary>
        /// 재도전 버튼 클릭
        /// </summary>
        private void OnRetryButtonClicked()
        {
            Time.timeScale = 1; // 게임 재개
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 현재 씬 다시 로드
        }

        /// <summary>
        /// 부활 버튼 클릭 - 부활석이 있으면 확인 팝업, 없으면 아무것도 안함
        /// </summary>
        private void OnReviveButtonClicked()
        {
            PlayerSaveData currentSaveData = SaveManager.Instance.GetCurrentSaveData();
            int currentReviveStones = currentSaveData.reviveStones;

            // 부활석이 있으면 확인 팝업 표시
            if (currentReviveStones > 0)
            {
                // 현재 부활석 개수와 부활 후 개수 표시
                if (currentReviveStoneValue != null)
                    currentReviveStoneValue.text = currentReviveStones.ToString();

                if (afterReviveStoneValue != null)
                    afterReviveStoneValue.text = (currentReviveStones - 1).ToString();

                // 게임오버 팝업 숨기고 부활 확인 팝업 표시
                if (gameOverPopup != null)
                    gameOverPopup.style.display = DisplayStyle.None;

                if (reviveConfirmPopup != null)
                    reviveConfirmPopup.style.display = DisplayStyle.Flex;
            }
            else
            {
                // 부활석이 없으면 부족 팝업 표시
                if (gameOverPopup != null)
                    gameOverPopup.style.display = DisplayStyle.None;

                if (insufficientReviveStonePopup != null)
                    insufficientReviveStonePopup.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// 부활 확인 버튼 클릭 - 실제로 부활 실행
        /// </summary>
        private void OnConfirmReviveButtonClicked()
        {
            // 부활석 소비
            if (SaveManager.Instance.SpendReviveStones(1))
            {
                // 플레이어 부활
                if (playerComponent != null)
                {
                    playerComponent.Revive();
                }

                // 부활 확인 팝업 닫기
                if (reviveConfirmPopup != null)
                {
                    reviveConfirmPopup.style.display = DisplayStyle.None;
                }

                Time.timeScale = 1; // 게임 재개

                // 화폐 UI 업데이트
                UpdateCurrencyDisplay();

                // Debug.Log("부활 완료! 남은 부활석: " + SaveManager.Instance.GetCurrentSaveData().reviveStones);
            }
        }

        /// <summary>
        /// 부활 취소 버튼 클릭 - 게임오버 팝업으로 돌아가기
        /// </summary>
        private void OnCancelReviveButtonClicked()
        {
            // 부활 확인 팝업 숨기고 게임오버 팝업 다시 표시
            if (reviveConfirmPopup != null)
                reviveConfirmPopup.style.display = DisplayStyle.None;

            if (gameOverPopup != null)
                gameOverPopup.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 상점 가기 버튼 클릭 - 상점 팝업 열기
        /// </summary>
        private void OnGoToStoreButtonClicked()
        {
            // 게임오버에서 열렸음을 표시
            storeOpenedFromGameOver = true;

            // 부족 팝업 숨기고 상점 팝업 표시
            if (insufficientReviveStonePopup != null)
                insufficientReviveStonePopup.style.display = DisplayStyle.None;

            if (storePopup != null)
            {
                storePopup.style.display = DisplayStyle.Flex;

                // 화폐 표시 업데이트
                UpdateStoreCurrencyDisplay();

                // 상품 목록 생성 (처음 한번만)
                if (storeDiamondGrid != null && storeDiamondGrid.childCount == 0)
                {
                    PopulateStoreProducts();
                }

                // 다이아몬드 탭 기본 표시
                ShowStoreDiamondPanel();
            }
        }

        /// <summary>
        /// 상점 닫기 버튼 클릭
        /// </summary>
        private void OnStoreCloseButtonClicked()
        {
            if (storePopup != null)
                storePopup.style.display = DisplayStyle.None;

            // 게임오버 상태인지 확인 (Time.timeScale이 0이고 게임오버 팝업이 있었던 경우)
            // 게임오버 팝업에서 열린 경우 게임오버 팝업으로 복귀
            // 메뉴에서 열린 경우 사이드바 표시하고 게임 재개
            if (gameOverPopup != null && storeOpenedFromGameOver)
            {
                gameOverPopup.style.display = DisplayStyle.Flex;
                storeOpenedFromGameOver = false;
            }
            else
            {
                ShowSidebars();
                ResumeGame();
            }

            // 화폐 UI 업데이트
            UpdateCurrencyDisplay();
        }

        /// <summary>
        /// 상점 다이아몬드 탭 클릭
        /// </summary>
        private void OnStoreDiamondButtonClicked()
        {
            ShowStoreDiamondPanel();
        }

        /// <summary>
        /// 상점 부활석 탭 클릭
        /// </summary>
        private void OnStoreReviveStoneButtonClicked()
        {
            ShowStoreReviveStonePanel();
        }

        /// <summary>
        /// 다이아몬드 패널 표시
        /// </summary>
        private void ShowStoreDiamondPanel()
        {
            if (storeDiamondPanel != null)
                storeDiamondPanel.style.display = DisplayStyle.Flex;

            if (storeReviveStonePanel != null)
                storeReviveStonePanel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 부활석 패널 표시
        /// </summary>
        private void ShowStoreReviveStonePanel()
        {
            if (storeDiamondPanel != null)
                storeDiamondPanel.style.display = DisplayStyle.None;

            if (storeReviveStonePanel != null)
                storeReviveStonePanel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 상점 화폐 표시 업데이트
        /// </summary>
        private void UpdateStoreCurrencyDisplay()
        {
            PlayerSaveData currentSaveData = SaveManager.Instance.GetCurrentSaveData();

            if (storeDiamondCount != null)
                storeDiamondCount.text = currentSaveData.diamonds.ToString();

            if (storeReviveStoneCount != null)
                storeReviveStoneCount.text = currentSaveData.reviveStones.ToString();
        }

        /// <summary>
        /// 상점 상품 목록 생성
        /// </summary>
        private void PopulateStoreProducts()
        {
            // 다이아몬드 상품 (실제 돈으로 구매 - 테스트용으로 바로 지급)
            var diamondItems = new List<(int quantity, int price)>
            {
                (1, 1100),
                (5, 5000),
                (10, 9000),
                (25, 20000),
                (50, 35000),
                (100, 60000)
            };

            // 다이아몬드 상품 카드 생성
            if (storeDiamondGrid != null)
            {
                storeDiamondGrid.Clear();
                foreach (var item in diamondItems)
                {
                    var card = CreateDiamondProductCard(item.quantity, item.price);
                    storeDiamondGrid.Add(card);
                }
            }

            // 부활석 상품 (다이아몬드로 구매)
            var reviveStoneItems = new List<(int quantity, int price)>
            {
                (1, 1),
                (5, 4),
                (10, 8),
                (25, 18),
                (50, 32),
                (100, 55)
            };

            // 부활석 상품 카드 생성
            if (storeReviveStoneGrid != null)
            {
                storeReviveStoneGrid.Clear();
                foreach (var item in reviveStoneItems)
                {
                    var card = CreateStoreProductCard(item.quantity, item.price, true);
                    storeReviveStoneGrid.Add(card);
                }
            }
        }

        /// <summary>
        /// 다이아몬드 상품 카드 생성 (실제 돈 결제용)
        /// </summary>
        private VisualElement CreateDiamondProductCard(int quantity, int price)
        {
            var card = new VisualElement();
            card.AddToClassList("product-card");

            // 아이템 정보 행
            var infoRow = new VisualElement();
            infoRow.AddToClassList("product-info-row");

            var icon = new VisualElement();
            icon.AddToClassList("product-icon-large");
            icon.AddToClassList("diamond-icon-product");
            infoRow.Add(icon);

            var multiplication = new Label("X");
            multiplication.AddToClassList("multiplication-sign");
            infoRow.Add(multiplication);

            var quantityLabel = new Label($"{quantity:N0}");
            quantityLabel.AddToClassList("product-quantity");
            infoRow.Add(quantityLabel);

            card.Add(infoRow);

            // 가격 (실제 돈)
            var priceLabel = new Label($"₩{price:N0}");
            priceLabel.AddToClassList("product-price");
            card.Add(priceLabel);

            // 구매 버튼
            var buyButton = new Button();
            buyButton.text = "BUY";
            buyButton.AddToClassList("buy-button");
            int capturedQuantity = quantity;
            buyButton.clicked += () => OnDiamondBuyButtonClicked(capturedQuantity);
            card.Add(buyButton);

            return card;
        }

        /// <summary>
        /// 다이아몬드 구매 버튼 클릭 (테스트용 - 바로 지급)
        /// </summary>
        private void OnDiamondBuyButtonClicked(int quantity)
        {
            // TODO: 실제 결제 로직 구현 (IAP)
            // 현재는 테스트로 바로 다이아몬드 지급
            SaveManager.Instance.AddDiamonds(quantity);
            UpdateStoreCurrencyDisplay();
        }

        /// <summary>
        /// 상품 카드 생성
        /// </summary>
        private VisualElement CreateStoreProductCard(int quantity, int price, bool isReviveStone)
        {
            var card = new VisualElement();
            card.AddToClassList("product-card");

            // 아이템 정보 행
            var infoRow = new VisualElement();
            infoRow.AddToClassList("product-info-row");

            var icon = new VisualElement();
            icon.AddToClassList("product-icon-large");
            icon.AddToClassList(isReviveStone ? "revive-stone-icon-product" : "diamond-icon-product");
            infoRow.Add(icon);

            var multiplication = new Label("X");
            multiplication.AddToClassList("multiplication-sign");
            infoRow.Add(multiplication);

            var quantityLabel = new Label($"{quantity:N0}");
            quantityLabel.AddToClassList("product-quantity");
            infoRow.Add(quantityLabel);

            card.Add(infoRow);

            // 가격 (다이아몬드)
            var priceContainer = new VisualElement();
            priceContainer.AddToClassList("product-price-container");

            var priceIcon = new VisualElement();
            priceIcon.AddToClassList("product-price-icon");
            priceContainer.Add(priceIcon);

            var priceMultiplication = new Label("X");
            priceMultiplication.AddToClassList("price-multiplication-sign");
            priceContainer.Add(priceMultiplication);

            var priceText = new Label($"{price:N0}");
            priceText.AddToClassList("product-price-text");
            priceContainer.Add(priceText);

            card.Add(priceContainer);

            // 구매 버튼
            var buyButton = new Button();
            buyButton.text = "BUY";
            buyButton.AddToClassList("buy-button");
            buyButton.clicked += () => OnStoreBuyButtonClicked(quantity, price, isReviveStone);
            card.Add(buyButton);

            return card;
        }

        /// <summary>
        /// 상점 구매 버튼 클릭
        /// </summary>
        private void OnStoreBuyButtonClicked(int quantity, int price, bool isReviveStone)
        {
            if (isReviveStone)
            {
                // 다이아몬드로 부활석 구매
                if (SaveManager.Instance.SpendDiamonds(price))
                {
                    SaveManager.Instance.AddReviveStones(quantity);
                    UpdateStoreCurrencyDisplay();
                    // Debug.Log($"부활석 {quantity}개 구매 완료!");
                }
                else
                {
                    // Debug.Log("다이아몬드가 부족합니다!");
                }
            }
        }

        /// <summary>
        /// 부활석 부족 팝업 확인 버튼 - 게임오버 팝업으로 돌아가기
        /// </summary>
        private void OnConfirmInsufficientButtonClicked()
        {
            // 부족 팝업 숨기고 게임오버 팝업 다시 표시
            if (insufficientReviveStonePopup != null)
                insufficientReviveStonePopup.style.display = DisplayStyle.None;

            if (gameOverPopup != null)
                gameOverPopup.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 게임오버 종료 버튼 클릭 - 메뉴 화면으로
        /// </summary>
        private void OnGameOverMainMenuButtonClicked()
        {
            Time.timeScale = 1; // 게임 재개
            GameStateManager.Instance.ResetGameState(); // 게임 상태 초기화
            SceneManager.LoadScene("Menu");
        }

        /// <summary>
        /// 키보드 네비게이션 비활성화 (A/D 키가 캐릭터 이동만 담당하도록)
        /// </summary>
        private void DisableKeyboardNavigation()
        {
            // 스킬 카테고리 버튼들의 포커스 비활성화
            if (allSkillButton != null) allSkillButton.focusable = false;
            if (lowPitchSkillButton != null) lowPitchSkillButton.focusable = false;
            if (midPitchSkillButton != null) midPitchSkillButton.focusable = false;
            if (highPitchSkillButton != null) highPitchSkillButton.focusable = false;

            // ScrollView들의 포커스 비활성화
            if (allSkillScrollView != null) allSkillScrollView.focusable = false;
            if (lowPitchSkillScrollView != null) lowPitchSkillScrollView.focusable = false;
            if (midPitchSkillScrollView != null) midPitchSkillScrollView.focusable = false;
            if (highPitchSkillScrollView != null) highPitchSkillScrollView.focusable = false;

            // 스킬 카테고리 스크롤뷰 포커스 비활성화
            var skillCategoryScrollView = uiDocument.rootVisualElement.Q<ScrollView>("SkillCategoryScrollView");
            if (skillCategoryScrollView != null) skillCategoryScrollView.focusable = false;

            // 스킬 패널 내 모든 버튼의 포커스 비활성화
            if (skillPanel != null)
            {
                var allButtons = skillPanel.Query<Button>().ToList();
                foreach (var button in allButtons)
                {
                    button.focusable = false;
                }
            }
        }
    }
}
