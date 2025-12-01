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

        // 상점 팝업
        private VisualElement storePopup;
        private Button storeCloseButton;
        private Label storeDiamondCount;
        private Label storeReviveStoneCount;
        private Button storeDiamondButton;
        private Button storeReviveStoneButton;
        private ScrollView storeDiamondPanel;
        private ScrollView storeReviveStonePanel;
        private VisualElement storeDiamondGrid;
        private VisualElement storeReviveStoneGrid;

        // 화폐 표시 Label
        private Label diamondCountLabel;
        private Label reviveStoneCountLabel;

        // 음성인식 결과 표시
        private Label voiceRecognitionText;

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

        // 스킬창 관련
        private VisualElement skillPanel;
        private Button allSkillButton;
        private Button attackSkillButton;
        private Button defenseSkillButton;
        private ScrollView allSkillScrollView;
        private ScrollView attackSkillScrollView;
        private ScrollView defenseSkillScrollView;
        private VisualElement allSkillList;
        private VisualElement attackSkillList;
        private VisualElement defenseSkillList;

        private SkillType? currentSkillCategory = null; // null이면 All 탭
        private Dictionary<string, float> skillAccuracyMap = new Dictionary<string, float>();

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
            menuPopup = root.Q<VisualElement>("MenuPopup");
            menuTitle = menuPopup?.Q<Label>(className: "menu-title");

            // 화폐 표시 Label
            diamondCountLabel = root.Q<Label>("DiamondCount");
            reviveStoneCountLabel = root.Q<Label>("ReviveStoneCount");

            // 음성인식 결과 표시
            voiceRecognitionText = root.Q<Label>("VoiceRecognitionText");

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

            // 스킬창 UI 요소
            skillPanel = root.Q<VisualElement>("SkillPanel");
            allSkillButton = root.Q<Button>("AllSkillButton");
            attackSkillButton = root.Q<Button>("AttackSkillButton");
            defenseSkillButton = root.Q<Button>("DefenseSkillButton");

            // ScrollView 요소 (표시/숨김 제어용)
            allSkillScrollView = root.Q<ScrollView>("AllSkillScrollView");
            attackSkillScrollView = root.Q<ScrollView>("AttackSkillScrollView");
            defenseSkillScrollView = root.Q<ScrollView>("DefenseSkillScrollView");

            // 내부 스킬 리스트 (스킬 아이템 추가용)
            allSkillList = root.Q<VisualElement>("AllSkillList");
            attackSkillList = root.Q<VisualElement>("AttackSkillList");
            defenseSkillList = root.Q<VisualElement>("DefenseSkillList");

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

            // 상점 팝업 UI 요소
            storePopup = root.Q<VisualElement>("StorePopup");
            storeCloseButton = root.Q<Button>("StoreCloseButton");
            storeDiamondCount = root.Q<Label>("StoreDiamondCount");
            storeReviveStoneCount = root.Q<Label>("StoreReviveStoneCount");
            storeDiamondButton = root.Q<Button>("StoreDiamondButton");
            storeReviveStoneButton = root.Q<Button>("StoreReviveStoneButton");
            storeDiamondPanel = root.Q<ScrollView>("StoreDiamondPanel");
            storeReviveStonePanel = root.Q<ScrollView>("StoreReviveStonePanel");
            storeDiamondGrid = root.Q<VisualElement>("StoreDiamondGrid");
            storeReviveStoneGrid = root.Q<VisualElement>("StoreReviveStoneGrid");

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

            // 상점 팝업 버튼 이벤트
            if (storeCloseButton != null)
                storeCloseButton.clicked += OnStoreCloseButtonClicked;

            if (storeDiamondButton != null)
                storeDiamondButton.clicked += OnStoreDiamondButtonClicked;

            if (storeReviveStoneButton != null)
                storeReviveStoneButton.clicked += OnStoreReviveStoneButtonClicked;

            // 스킬 카테고리 버튼 이벤트
            if (allSkillButton != null)
                allSkillButton.clicked += () => SwitchSkillCategory(null); // null = All

            if (attackSkillButton != null)
                attackSkillButton.clicked += () => SwitchSkillCategory(SkillType.Attack);

            if (defenseSkillButton != null)
                defenseSkillButton.clicked += () => SwitchSkillCategory(SkillType.Defense);

            // 저장 데이터 가져오기
            saveData = SaveManager.Instance.GetCurrentSaveData();

            // 챕터 정보 표시
            UpdateChapterInfo();

            // 화폐 정보 표시
            UpdateCurrencyDisplay();

            // Localization 이벤트 등록 (실제 적용은 Start에서)
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // Additive 씬 언로드 감지 (Options/Store에서 돌아올 때)
            SceneManager.sceneUnloaded += OnSceneUnloaded;
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
                allSkillButton.clicked -= () => SwitchSkillCategory(null);

            if (attackSkillButton != null)
                attackSkillButton.clicked -= () => SwitchSkillCategory(SkillType.Attack);

            if (defenseSkillButton != null)
                defenseSkillButton.clicked -= () => SwitchSkillCategory(SkillType.Defense);

            // Localization 이벤트 해제
            UnregisterLocalizationEvents();

            // Additive 씬 언로드 이벤트 해제
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
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

        /// <summary>
        /// 스킬 목록 로드
        /// </summary>
        private void LoadSkills()
        {
            var allSkills = DataManager.Instance.GetAllSkillData();

            if (allSkills == null || allSkills.Count == 0)
            {
                Debug.LogWarning("[InGameUI] 스킬 데이터가 없습니다!");
                return;
            }

            // 카테고리별로 스킬 분류
            var attackSkills = allSkills.Where(s => s.skillType == SkillType.Attack).ToList();
            var defenseSkills = allSkills.Where(s => s.skillType == SkillType.Defense).ToList();

            // All 리스트 채우기 (카테고리별로 그룹화)
            PopulateAllSkillList(allSkillList, attackSkills, defenseSkills);

            // 각 카테고리별 리스트 채우기
            PopulateSkillList(attackSkillList, attackSkills);
            PopulateSkillList(defenseSkillList, defenseSkills);

            // 초기 카테고리 표시 (All 탭)
            SwitchSkillCategory(currentSkillCategory);
        }

        /// <summary>
        /// 스킬 리스트 UI 생성
        /// </summary>
        private void PopulateSkillList(VisualElement container, List<SkillData> skills)
        {
            if (container == null) return;

            container.Clear();

            foreach (var skill in skills)
            {
                // 스킬 아이템 컨테이너
                var skillItem = new VisualElement();
                skillItem.AddToClassList("skill-item");

                // 스킬명 (현재 언어에 맞게)
                var skillName = new Label(skill.GetLocalizedName());
                skillName.AddToClassList("skill-name");

                // 정확도 (기본값 0%, 나중에 음성인식 결과로 업데이트)
                var accuracy = new Label("0%");
                accuracy.AddToClassList("skill-accuracy");
                accuracy.name = $"Accuracy_{skill.voiceKeyword}";

                skillItem.Add(skillName);
                skillItem.Add(accuracy);
                container.Add(skillItem);

                // 정확도 맵 초기화
                if (!string.IsNullOrEmpty(skill.voiceKeyword))
                {
                    skillAccuracyMap[skill.voiceKeyword] = 0f;
                }
            }
        }

        /// <summary>
        /// All 탭 UI 생성 (카테고리별로 그룹화)
        /// </summary>
        private void PopulateAllSkillList(VisualElement container, List<SkillData> attackSkills, List<SkillData> defenseSkills)
        {
            if (container == null) return;

            container.Clear();

            // Attack 섹션
            if (attackSkills.Count > 0)
            {
                var attackHeader = new Label(LocalizationManager.Instance.GetText("skill_category_attack"));
                attackHeader.AddToClassList("skill-category-header");
                container.Add(attackHeader);

                foreach (var skill in attackSkills)
                {
                    AddSkillItem(container, skill);
                }
            }

            // Defense 섹션
            if (defenseSkills.Count > 0)
            {
                var defenseHeader = new Label(LocalizationManager.Instance.GetText("skill_category_defense"));
                defenseHeader.AddToClassList("skill-category-header");
                container.Add(defenseHeader);

                foreach (var skill in defenseSkills)
                {
                    AddSkillItem(container, skill);
                }
            }
        }

        /// <summary>
        /// 스킬 아이템 추가 (공통 로직)
        /// </summary>
        private void AddSkillItem(VisualElement container, SkillData skill)
        {
            var skillItem = new VisualElement();
            skillItem.AddToClassList("skill-item");

            var skillName = new Label(skill.GetLocalizedName());
            skillName.AddToClassList("skill-name");

            var accuracy = new Label("0%");
            accuracy.AddToClassList("skill-accuracy");
            accuracy.name = $"Accuracy_{skill.voiceKeyword}";

            skillItem.Add(skillName);
            skillItem.Add(accuracy);
            container.Add(skillItem);

            if (!string.IsNullOrEmpty(skill.voiceKeyword))
            {
                skillAccuracyMap[skill.voiceKeyword] = 0f;
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
        /// 스킬 카테고리 전환
        /// </summary>
        private void SwitchSkillCategory(SkillType? category)
        {
            currentSkillCategory = category;

            // 모든 ScrollView 숨기기
            if (allSkillScrollView != null)
                allSkillScrollView.style.display = DisplayStyle.None;
            if (attackSkillScrollView != null)
                attackSkillScrollView.style.display = DisplayStyle.None;
            if (defenseSkillScrollView != null)
                defenseSkillScrollView.style.display = DisplayStyle.None;

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
                    case SkillType.Attack:
                        if (attackSkillScrollView != null)
                            attackSkillScrollView.style.display = DisplayStyle.Flex;
                        break;
                    case SkillType.Defense:
                        if (defenseSkillScrollView != null)
                            defenseSkillScrollView.style.display = DisplayStyle.Flex;
                        break;
                }
            }

            // 음성인식 매니저에 현재 카테고리 스킬 전달
            UpdateVoiceRecognitionSkills();
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
            var allSkills = DataManager.Instance.GetAllSkillData();
            if (allSkills == null) return new List<SkillData>();

            // All 탭 (null)이면 모든 스킬 반환
            if (currentSkillCategory == null)
                return allSkills;

            return allSkills.Where(s => s.skillType == currentSkillCategory.Value).ToList();
        }

        /// <summary>
        /// 모든 스킬 정확도 초기화 (새로운 음성 인식 시작 시)
        /// </summary>
        public void ClearSkillAccuracy()
        {
            // 맵 초기화
            skillAccuracyMap.Clear();

            // 모든 ScrollView의 라벨을 0%로 초기화
            ClearAccuracyLabelsInScrollView(allSkillScrollView);
            ClearAccuracyLabelsInScrollView(attackSkillScrollView);
            ClearAccuracyLabelsInScrollView(defenseSkillScrollView);
        }

        /// <summary>
        /// 특정 ScrollView의 모든 정확도 라벨을 0%로 초기화
        /// </summary>
        private void ClearAccuracyLabelsInScrollView(ScrollView scrollView)
        {
            if (scrollView == null) return;

            var allLabels = scrollView.Query<Label>().Where(label => label.name != null && label.name.StartsWith("Accuracy_")).ToList();
            foreach (var label in allLabels)
            {
                label.text = "0%";
                label.style.color = Color.white;
            }
        }

        /// <summary>
        /// 음성인식 정확도 업데이트 (VoiceRecognitionManager에서 호출)
        /// </summary>
        public void UpdateSkillAccuracy(Dictionary<string, float> accuracyScores)
        {
            foreach (var kvp in accuracyScores)
            {
                string keyword = kvp.Key;
                float score = kvp.Value;

                // 정확도 맵 업데이트
                skillAccuracyMap[keyword] = score;
            }

            // 현재 표시 중인 ScrollView에서만 UI 업데이트
            ScrollView currentScrollView = GetCurrentScrollView();
            if (currentScrollView != null)
            {
                UpdateAccuracyLabelsInScrollView(currentScrollView, accuracyScores);
            }
        }

        /// <summary>
        /// 현재 활성화된 ScrollView 가져오기
        /// </summary>
        private ScrollView GetCurrentScrollView()
        {
            if (currentSkillCategory == null && allSkillScrollView != null && allSkillScrollView.style.display == DisplayStyle.Flex)
                return allSkillScrollView;

            if (currentSkillCategory == SkillType.Attack && attackSkillScrollView != null && attackSkillScrollView.style.display == DisplayStyle.Flex)
                return attackSkillScrollView;

            if (currentSkillCategory == SkillType.Defense && defenseSkillScrollView != null && defenseSkillScrollView.style.display == DisplayStyle.Flex)
                return defenseSkillScrollView;

            return null;
        }

        /// <summary>
        /// 특정 ScrollView 내의 정확도 라벨 업데이트
        /// </summary>
        private void UpdateAccuracyLabelsInScrollView(ScrollView scrollView, Dictionary<string, float> accuracyScores)
        {
            foreach (var kvp in accuracyScores)
            {
                string keyword = kvp.Key;
                float score = kvp.Value;

                // 해당 ScrollView 내에서 라벨 찾기
                var accuracyLabel = scrollView.Q<Label>($"Accuracy_{keyword}");
                if (accuracyLabel != null)
                {
                    int percentage = Mathf.RoundToInt(score * 100f);
                    accuracyLabel.text = $"{percentage}%";

                    // 색상 변경 (높을수록 녹색)
                    if (score >= 0.7f)
                        accuracyLabel.style.color = Color.green;
                    else if (score >= 0.4f)
                        accuracyLabel.style.color = Color.yellow;
                    else
                        accuracyLabel.style.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 음성인식 결과 표시 (VoiceRecognitionManager에서 호출)
        /// </summary>
        public void UpdateVoiceRecognitionDisplay(string message)
        {
            if (voiceRecognitionText != null)
            {
                voiceRecognitionText.text = message;
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

        /// <summary>
        /// Additive 씬이 언로드될 때 호출 (Options/Store에서 돌아올 때)
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            // Options나 Store 씬이 언로드되면 사이드바 다시 표시하고 게임 재개
            if (scene.name == "Options" || scene.name == "Store")
            {
                ShowSidebars();
                ResumeGame(); // 게임 재개

                // Options에서 언어가 변경되었을 수 있으므로 UI 업데이트
                if (scene.name == "Options")
                {
                    // LocalizationManager 이벤트 재구독
                    LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;
                    // 현재 언어로 UI 업데이트
                    UpdateLocalization();
                }

                // Store에서 화폐가 변경되었을 수 있으므로 UI 업데이트
                if (scene.name == "Store")
                {
                    saveData = SaveManager.Instance.GetCurrentSaveData();
                    UpdateCurrencyDisplay();
                }
            }
        }

        private void OnSettingsButtonClicked()
        {
            // 메뉴 팝업 닫기
            if (menuPopup != null)
                menuPopup.style.display = DisplayStyle.None;

            // 게임 상태 유지를 위해 Additive 모드로 씬 로드
            // Time.timeScale은 일시정지 상태 유지 (설정 화면에서도 일시정지)
            SceneNavigationManager.Instance.SetPreviousScene("InGame");
            SceneManager.LoadScene("Options", LoadSceneMode.Additive);
        }

        private void OnStoreButtonClicked()
        {
            // 메뉴 팝업 닫기
            if (menuPopup != null)
                menuPopup.style.display = DisplayStyle.None;

            // 게임 상태 유지를 위해 Additive 모드로 씬 로드
            // Time.timeScale은 일시정지 상태 유지 (상점 화면에서도 일시정지)
            SceneNavigationManager.Instance.SetPreviousScene("InGame");
            SceneManager.LoadScene("Store", LoadSceneMode.Additive);
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

            // 스킬 카테고리 버튼 텍스트 업데이트
            if (allSkillButton != null)
                allSkillButton.text = loc.GetText("skill_category_all");

            if (attackSkillButton != null)
                attackSkillButton.text = loc.GetText("skill_category_attack");

            if (defenseSkillButton != null)
                defenseSkillButton.text = loc.GetText("skill_category_defense");

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
                gameOverPopup.style.display = DisplayStyle.Flex;
                Time.timeScale = 0; // 게임 일시정지
            }
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

                Debug.Log("부활 완료! 남은 부활석: " + SaveManager.Instance.GetCurrentSaveData().reviveStones);
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

            // 게임오버 팝업으로 복귀
            if (gameOverPopup != null)
                gameOverPopup.style.display = DisplayStyle.Flex;

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
            // 다이아몬드 상품 (테스트용 - 실제로는 다이아로 부활석 구매)
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
                    Debug.Log($"부활석 {quantity}개 구매 완료!");
                }
                else
                {
                    Debug.Log("다이아몬드가 부족합니다!");
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
    }
}
