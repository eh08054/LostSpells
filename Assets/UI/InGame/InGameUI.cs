using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using LostSpells.Data;
using LostSpells.Systems;
using LostSpells.Data.Save;

namespace LostSpells.UI
{
    /// <summary>
    /// 인게임 UI 컨트롤러
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InGameUI : MonoBehaviour
    {
        [Header("배경 이미지")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite middlegroundSprite;

        private UIDocument uiDocument;

        // HUD 요소들
        private Button menuButton;
        private Label gameModeInfo;     // 게임 모드 정보 (Story/Chapter/Endless)
        private Label chapterInfo;
        private Label waveInfo;
        private Label diamondCount;
        private Label reviveStoneCount;

        // 사이드바 관련
        private VisualElement leftSidebar;
        private bool isSidebarVisible = true;

        // 플레이어 정보 요소들
        private Label playerLevel;
        private VisualElement expBar;
        private Label hpText;
        private Label mpText;
        private VisualElement hpBar;
        private VisualElement mpBar;

        // 스킬 카테고리 버튼들
        private Button attackSkillButton;
        private Button defenseSkillButton;
        private Button supportSkillButton;

        // 스킬 목록들
        private VisualElement attackSkillList;
        private VisualElement defenseSkillList;
        private VisualElement supportSkillList;

        // 메뉴 팝업
        private VisualElement menuPopup;
        private Button resumeButton;
        private Button settingsButton;
        private Button storeButton;
        private Button mainMenuButton;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();

            // InGame 오브젝트 자동 생성 (Ground, Player, Enemy)
            CreateGameObjects();
        }

        /// <summary>
        /// InGame 씬 시작 시 배경, Ground, Player, Enemy 오브젝트 자동 생성 (씬에 없는 것만)
        /// </summary>
        private void CreateGameObjects()
        {
            // 배경 생성 (씬에 없으면 생성)
            if (GameObject.Find("Background") == null || GameObject.Find("Middleground") == null)
            {
                CreateBackground();
            }

            // 땅 생성 (씬에 없으면 생성)
            if (GameObject.Find("Ground") == null)
            {
                CreateGround();
            }

            // 플레이어 생성
            if (GameObject.Find("Player") == null)
            {
                GameObject player = new GameObject("Player");
                player.transform.position = new Vector3(-5, -3, 0);

                // Player 태그는 Unity 기본 태그이므로 안전하게 설정 가능
                try
                {
                    player.tag = "Player";
                }
                catch (UnityException)
                {
                    Debug.LogWarning("[InGameUI] Player 태그가 정의되지 않았습니다.");
                }

                SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSquareSprite();
                sr.color = new Color(0.2f, 0.5f, 1f); // 파란색
                sr.sortingOrder = 20; // Ground보다 앞에 표시

                player.transform.localScale = new Vector3(1f, 1.5f, 1f);

                BoxCollider2D collider = player.AddComponent<BoxCollider2D>();

                Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 3f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                Debug.Log("[InGameUI] Player 생성 완료");
            }

            // 적 생성
            if (GameObject.Find("Enemy") == null)
            {
                GameObject enemy = new GameObject("Enemy");
                enemy.transform.position = new Vector3(5, -3, 0);

                // Enemy 태그는 사용자 정의 태그이므로 안전하게 처리
                // 태그 설정 없이도 오브젝트 이름으로 구분 가능

                SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSquareSprite();
                sr.color = new Color(1f, 0.2f, 0.2f); // 빨간색
                sr.sortingOrder = 20; // Ground보다 앞에 표시

                enemy.transform.localScale = new Vector3(1f, 1.5f, 1f);

                BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();

                Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 3f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                // 체력바 생성
                CreateHealthBar(enemy);

                // Debug.Log("[InGameUI] Enemy 생성 완료");
            }

            // Debug.Log("[InGameUI] InGame 오브젝트 생성 완료!");
        }

        /// <summary>
        /// 배경 오브젝트 생성 (스프라이트 기반)
        /// </summary>
        private void CreateBackground()
        {
            // Background (하늘) 생성
            if (GameObject.Find("Background") == null)
            {
                GameObject background = new GameObject("Background");
                background.transform.position = new Vector3(0, 0, 0);

                SpriteRenderer sr = background.AddComponent<SpriteRenderer>();

                // Resources에서 배경 스프라이트 로드
                Sprite bgSprite = backgroundSprite;
                if (bgSprite == null)
                {
                    bgSprite = Resources.Load<Sprite>("Gothicvania-Town/Art/Environment/Background/background");
                }

                if (bgSprite != null)
                {
                    sr.sprite = bgSprite;

                    // 카메라 크기에 맞게 자동 스케일 조정
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        float screenHeight = cam.orthographicSize * 2f; // 높이 = 10
                        float screenWidth = screenHeight * cam.aspect; // 16:9 = 17.78

                        // 스프라이트 실제 크기 (Unity units)
                        float spriteHeight = bgSprite.bounds.size.y;
                        float spriteWidth = bgSprite.bounds.size.x;

                        // 화면을 채우기 위한 스케일 계산
                        float scaleX = screenWidth / spriteWidth;
                        float scaleY = screenHeight / spriteHeight;
                        float scale = Mathf.Max(scaleX, scaleY); // 화면을 완전히 채우도록

                        background.transform.localScale = new Vector3(scale, scale, 1f);

                        Debug.Log($"[InGameUI] Background 스케일: {scale}, 화면: {screenWidth}x{screenHeight}, 스프라이트: {spriteWidth}x{spriteHeight}");
                    }
                }
                else
                {
                    // 배경 스프라이트가 없으면 하늘색 사각형 생성
                    sr.sprite = CreateSquareSprite();
                    sr.color = new Color(0.53f, 0.81f, 0.92f); // 하늘색
                    background.transform.localScale = new Vector3(20f, 12f, 1f);
                }

                sr.sortingOrder = 0; // 가장 뒤에 표시

                Debug.Log("[InGameUI] Background 생성 완료");
            }

            // Middleground (건물) 생성
            if (GameObject.Find("Middleground") == null)
            {
                GameObject middleground = new GameObject("Middleground");
                middleground.transform.position = new Vector3(0, 0, 0);

                SpriteRenderer sr = middleground.AddComponent<SpriteRenderer>();

                // Resources에서 중경 스프라이트 로드
                Sprite mgSprite = middlegroundSprite;
                if (mgSprite == null)
                {
                    mgSprite = Resources.Load<Sprite>("Gothicvania-Town/Art/Environment/Background/middleground");
                }

                if (mgSprite != null)
                {
                    sr.sprite = mgSprite;

                    // 카메라 크기에 맞게 자동 스케일 조정
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        float screenHeight = cam.orthographicSize * 2f;
                        float screenWidth = screenHeight * cam.aspect;

                        float spriteHeight = mgSprite.bounds.size.y;
                        float spriteWidth = mgSprite.bounds.size.x;

                        float scaleX = screenWidth / spriteWidth;
                        float scaleY = screenHeight / spriteHeight;
                        float scale = Mathf.Max(scaleX, scaleY);

                        middleground.transform.localScale = new Vector3(scale, scale, 1f);

                        Debug.Log($"[InGameUI] Middleground 스케일: {scale}, 화면: {screenWidth}x{screenHeight}, 스프라이트: {spriteWidth}x{spriteHeight}");
                    }
                }
                else
                {
                    // 중경 스프라이트가 없으면 어두운 회색 사각형 생성
                    sr.sprite = CreateSquareSprite();
                    sr.color = new Color(0.3f, 0.3f, 0.35f, 0.7f); // 반투명 어두운 회색
                    middleground.transform.localScale = new Vector3(20f, 12f, 1f);
                }

                sr.sortingOrder = 5; // Background와 Ground 사이

                Debug.Log("[InGameUI] Middleground 생성 완료");
            }
        }

        /// <summary>
        /// 땅 오브젝트 생성 (투명)
        /// </summary>
        private void CreateGround()
        {
            GameObject ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0, -4, 0);

            SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.4f, 0.25f, 0.1f, 0f); // 완전 투명
            sr.sortingOrder = 10; // Background와 Middleground 뒤, Player/Enemy 앞

            ground.transform.localScale = new Vector3(20f, 1f, 1f);

            // BoxCollider2D 추가 (충돌 감지)
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();

            // Rigidbody2D 추가 (Static - 움직이지 않는 지형)
            Rigidbody2D rb = ground.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            Debug.Log("[InGameUI] Ground 생성 완료 - Position: (0, -4, 0), Scale: (20, 1, 1)");
        }

        /// <summary>
        /// 적 오브젝트 위에 체력바 생성
        /// </summary>
        private void CreateHealthBar(GameObject parent)
        {
            // 체력바 컨테이너 (부모의 자식으로)
            GameObject healthBarContainer = new GameObject("HealthBar");
            healthBarContainer.transform.SetParent(parent.transform);
            healthBarContainer.transform.localPosition = new Vector3(0, 0.85f, 0); // 적 위쪽에 배치 (높이 낮춤)
            healthBarContainer.transform.localScale = Vector3.one;

            // 체력바 배경/테두리 (검은색)
            GameObject background = new GameObject("Background");
            background.transform.SetParent(healthBarContainer.transform);
            background.transform.localPosition = Vector3.zero;

            SpriteRenderer bgSr = background.AddComponent<SpriteRenderer>();
            bgSr.sprite = CreateSquareSprite();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 1f); // 진한 검은색 (테두리)
            bgSr.sortingOrder = 21; // 적보다 앞에 표시

            background.transform.localScale = new Vector3(1.2f, 0.15f, 1f);

            // 체력바 전경 (녹색) - 왼쪽 정렬
            GameObject foreground = new GameObject("Foreground");
            foreground.transform.SetParent(healthBarContainer.transform);
            foreground.transform.localPosition = new Vector3(-0.08f, 0, 0); // 왼쪽 정렬

            SpriteRenderer fgSr = foreground.AddComponent<SpriteRenderer>();
            fgSr.sprite = CreateSquareSprite();
            fgSr.color = new Color(0.2f, 0.8f, 0.2f, 1f); // 녹색
            fgSr.sortingOrder = 22; // 배경보다 앞에 표시

            foreground.transform.localScale = new Vector3(1.04f, 0.11f, 1f); // 테두리가 보이도록 살짝 작게
        }

        /// <summary>
        /// 정사각형 Sprite 생성
        /// </summary>
        private Sprite CreateSquareSprite()
        {
            // 흰색 정사각형 텍스처 생성 (16x16 픽셀)
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];

            // 모든 픽셀을 흰색으로 채움
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point; // 픽셀 아트 스타일

            // Sprite 생성
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0.5f), // Pivot을 중앙으로
                16f // Pixels per unit
            );

            return sprite;
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // UI 요소 찾기
            menuButton = root.Q<Button>("MenuButton");
            gameModeInfo = root.Q<Label>("GameModeInfo");
            chapterInfo = root.Q<Label>("ChapterInfo");
            waveInfo = root.Q<Label>("WaveInfo");
            diamondCount = root.Q<Label>("DiamondCount");
            reviveStoneCount = root.Q<Label>("ReviveStoneCount");

            // 사이드바 찾기
            leftSidebar = root.Q<VisualElement>("LeftSidebar");

            // 플레이어 정보 요소 찾기
            playerLevel = root.Q<Label>("PlayerLevel");
            expBar = root.Q<VisualElement>("ExpBar");
            hpText = root.Q<Label>("HPText");
            mpText = root.Q<Label>("MPText");
            hpBar = root.Q<VisualElement>("HPBar");
            mpBar = root.Q<VisualElement>("MPBar");

            // 스킬 카테고리 버튼 찾기
            attackSkillButton = root.Q<Button>("AttackSkillButton");
            defenseSkillButton = root.Q<Button>("DefenseSkillButton");
            supportSkillButton = root.Q<Button>("SupportSkillButton");

            // 스킬 목록 찾기
            attackSkillList = root.Q<VisualElement>("AttackSkillList");
            defenseSkillList = root.Q<VisualElement>("DefenseSkillList");
            supportSkillList = root.Q<VisualElement>("SupportSkillList");

            // 이벤트 등록
            if (menuButton != null)
                menuButton.clicked += OnMenuButtonClicked;

            if (attackSkillButton != null)
                attackSkillButton.clicked += () => ShowSkillList(attackSkillList);

            if (defenseSkillButton != null)
                defenseSkillButton.clicked += () => ShowSkillList(defenseSkillList);

            if (supportSkillButton != null)
                supportSkillButton.clicked += () => ShowSkillList(supportSkillList);

            // 메뉴 팝업 찾기
            menuPopup = root.Q<VisualElement>("MenuPopup");
            resumeButton = root.Q<Button>("ResumeButton");
            settingsButton = root.Q<Button>("SettingsButton");
            storeButton = root.Q<Button>("StoreButton");
            mainMenuButton = root.Q<Button>("MainMenuButton");

            // 메뉴 팝업 버튼 이벤트 등록
            if (resumeButton != null)
                resumeButton.clicked += OnResumeButtonClicked;

            if (settingsButton != null)
                settingsButton.clicked += OnSettingsButtonClicked;

            if (storeButton != null)
                storeButton.clicked += OnStoreButtonClicked;

            if (mainMenuButton != null)
                mainMenuButton.clicked += OnMainMenuButtonClicked;

            // 초기 UI 업데이트 - GameManager에서 데이터 가져오기
            UpdateGameModeInfo();
            UpdateCurrencyFromGameManager();
            UpdatePlayerInfo();
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (menuButton != null)
                menuButton.clicked -= OnMenuButtonClicked;

            if (attackSkillButton != null)
                attackSkillButton.clicked -= () => ShowSkillList(attackSkillList);

            if (defenseSkillButton != null)
                defenseSkillButton.clicked -= () => ShowSkillList(defenseSkillList);

            if (supportSkillButton != null)
                supportSkillButton.clicked -= () => ShowSkillList(supportSkillList);

            // 메뉴 팝업 이벤트 해제
            if (resumeButton != null)
                resumeButton.clicked -= OnResumeButtonClicked;

            if (settingsButton != null)
                settingsButton.clicked -= OnSettingsButtonClicked;

            if (storeButton != null)
                storeButton.clicked -= OnStoreButtonClicked;

            if (mainMenuButton != null)
                mainMenuButton.clicked -= OnMainMenuButtonClicked;
        }

        private void Update()
        {
            // TODO: 키 바인딩 기능 재구현 필요
            // 임시로 Tab 키로 사이드바 토글
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleSidebar();
            }

            // 테스트용: G 키로 게임오버 (EndlessMode 랭킹 테스트)
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                Debug.Log("[InGameUI] 게임오버 테스트 (G 키)");
                OnGameOver();
            }
        }

        #region Public Methods

        /// <summary>
        /// 게임 모드에 따른 정보 업데이트
        /// </summary>
        public void UpdateGameModeInfo()
        {
            var playerData = GameStateManager.GetCurrentSlotData();
            var currentMode = GameStateManager.CurrentGameMode;

            switch (currentMode)
            {
                case GameMode.StoryMode:
                    UpdateStoryModeInfo(playerData);
                    break;

                case GameMode.ChapterSelect:
                    UpdateChapterModeInfo(playerData);
                    break;

                case GameMode.EndlessMode:
                    UpdateEndlessModeInfo(playerData);
                    break;

                default:
                    if (gameModeInfo != null)
                        gameModeInfo.text = "TEST MODE";
                    break;
            }

            SetWave(playerData.currentWave);
        }

        /// <summary>
        /// 스토리 모드 정보 표시
        /// </summary>
        private void UpdateStoryModeInfo(PlayerData playerData)
        {
            if (gameModeInfo != null)
                gameModeInfo.text = "STORY MODE";

            if (chapterInfo != null)
                chapterInfo.text = $"Chapter {playerData.currentChapter}: {playerData.chapterName}";
        }

        /// <summary>
        /// 챕터 모드 정보 표시 (진행도 포함)
        /// </summary>
        private void UpdateChapterModeInfo(PlayerData playerData)
        {
            if (gameModeInfo != null)
                gameModeInfo.text = "CHAPTER MODE";

            if (chapterInfo != null)
            {
                // 챕터 진행도 가져오기
                int currentSlot = GameStateManager.CurrentSlot;
                int currentChapter = GameStateManager.SelectedChapter;
                var chapterProgress = SaveSystem.GetChapterProgress(currentSlot, currentChapter);

                int percentage = chapterProgress.totalStages > 0
                    ? (chapterProgress.currentStage * 100 / chapterProgress.totalStages)
                    : 0;

                chapterInfo.text = $"Ch.{currentChapter}: {ChapterProgressSystem.GetChapterName(currentChapter)} ({percentage}%)";
            }
        }

        /// <summary>
        /// 무한 모드 정보 표시 (난이도, 최고 기록)
        /// </summary>
        private void UpdateEndlessModeInfo(PlayerData playerData)
        {
            // 선택된 난이도 가져오기
            DifficultyLevel difficulty = GameStateManager.SelectedDifficulty;
            string difficultyText = difficulty.ToString().ToUpper();

            if (gameModeInfo != null)
                gameModeInfo.text = $"ENDLESS MODE - {difficultyText}";

            // TODO: 난이도 기능 제거됨 - 추후 단순화된 통계 시스템으로 교체 예정
            if (chapterInfo != null)
                chapterInfo.text = $"Record: Wave 0 | Lv.0";
        }

        /// <summary>
        /// 챕터 정보 업데이트 (레거시 호환)
        /// </summary>
        public void SetChapterInfo(string chapter)
        {
            if (chapterInfo != null)
                chapterInfo.text = chapter;
        }

        /// <summary>
        /// 웨이브 설정 (UI 표시만)
        /// </summary>
        public void SetWave(int wave)
        {
            if (waveInfo != null)
                waveInfo.text = $"Wave {wave}";
        }

        /// <summary>
        /// 다음 웨이브로 진행
        /// </summary>
        public void NextWave()
        {
            var playerData = GameStateManager.GetCurrentSlotData();
            playerData.currentWave++;
            GameStateManager.SaveCurrentSlotData(playerData);
            SetWave(playerData.currentWave);
            Debug.Log($"[InGame] Wave {playerData.currentWave} 시작");
        }

        /// <summary>
        /// 화폐 정보 가져와서 UI 업데이트
        /// </summary>
        public void UpdateCurrencyFromGameManager()
        {
            var playerData = GameStateManager.GetCurrentSlotData();

            if (diamondCount != null)
                diamondCount.text = playerData.diamonds.ToString();

            if (reviveStoneCount != null)
                reviveStoneCount.text = playerData.reviveStones.ToString();
        }

        /// <summary>
        /// 플레이어 정보 업데이트 (레벨, 경험치, HP, MP)
        /// </summary>
        public void UpdatePlayerInfo()
        {
            var playerData = GameStateManager.GetCurrentSlotData();

            // 레벨
            if (playerLevel != null)
                playerLevel.text = playerData.level.ToString();

            // 경험치 바 (레벨 박스 안에서 아래에서부터 채워짐)
            if (expBar != null)
            {
                float expPercent = playerData.maxExp > 0 ? (float)playerData.currentExp / playerData.maxExp : 0f;
                expBar.style.height = Length.Percent(expPercent * 100f);
            }

            // HP 바 및 텍스트
            if (hpText != null)
                hpText.text = $"{playerData.currentHP} / {playerData.maxHP}";

            if (hpBar != null)
            {
                float hpPercent = playerData.maxHP > 0 ? (float)playerData.currentHP / playerData.maxHP : 0f;
                hpBar.style.width = Length.Percent(hpPercent * 100f);
            }

            // MP 바 및 텍스트
            if (mpText != null)
                mpText.text = $"{playerData.currentMP} / {playerData.maxMP}";

            if (mpBar != null)
            {
                float mpPercent = playerData.maxMP > 0 ? (float)playerData.currentMP / playerData.maxMP : 0f;
                mpBar.style.width = Length.Percent(mpPercent * 100f);
            }
        }

        /// <summary>
        /// 다이아몬드 추가
        /// </summary>
        public void AddDiamonds(int amount)
        {
            var playerData = GameStateManager.GetCurrentSlotData();
            playerData.diamonds += amount;
            GameStateManager.SaveCurrentSlotData(playerData);
            UpdateCurrencyFromGameManager();
        }

        /// <summary>
        /// 부활석 추가
        /// </summary>
        public void AddReviveStones(int amount)
        {
            var playerData = GameStateManager.GetCurrentSlotData();
            playerData.reviveStones += amount;
            GameStateManager.SaveCurrentSlotData(playerData);
            UpdateCurrencyFromGameManager();
        }

        /// <summary>
        /// 모든 데이터를 UI에 반영 (에디터에서 데이터 변경 시 호출)
        /// </summary>
        public void RefreshAllData()
        {
            var playerData = GameStateManager.GetCurrentSlotData();

            UpdateGameModeInfo();
            UpdateCurrencyFromGameManager();
            UpdatePlayerInfo();

            Debug.Log($"[InGameUI] 전체 데이터 새로고침 - Level {playerData.level}, Exp {playerData.currentExp}/{playerData.maxExp}, Chapter {playerData.currentChapter}, Wave {playerData.currentWave}, HP {playerData.currentHP}/{playerData.maxHP}, Diamonds {playerData.diamonds}");
        }

        /// <summary>
        /// 게임 종료 (EndlessMode인 경우 랭킹 저장)
        /// </summary>
        public void OnGameOver()
        {
            var currentMode = GameStateManager.CurrentGameMode;

            // EndlessMode인 경우에만 랭킹 저장
            if (currentMode == GameMode.EndlessMode)
            {
                var playerData = GameStateManager.GetCurrentSlotData();
                string currentDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                // 랭킹 저장
                SaveSystem.SaveEndlessModeRanking(playerData.currentWave, playerData.level, currentDate);

                Debug.Log($"[InGameUI] EndlessMode 종료 - Wave {playerData.currentWave}, Level {playerData.level} 기록 저장됨");
            }

            // 게임 오버 처리 (메인 메뉴로 이동)
            Time.timeScale = 1f; // 타임스케일 복구
            SceneManager.LoadScene("MainMenu");
        }

        #endregion

        #region Event Handlers

        private void OnMenuButtonClicked()
        {
            Debug.Log("[InGame] 메뉴 버튼 클릭 - 메뉴 팝업 열기");
            ShowMenuPopup();
        }

        private void ShowMenuPopup()
        {
            if (menuPopup != null)
            {
                menuPopup.style.display = DisplayStyle.Flex;
                Time.timeScale = 0f; // 게임 일시정지
                Debug.Log("[InGame] 메뉴 팝업 표시 및 게임 일시정지");
            }
        }

        private void HideMenuPopup()
        {
            if (menuPopup != null)
            {
                menuPopup.style.display = DisplayStyle.None;
                Time.timeScale = 1f; // 게임 재개
                Debug.Log("[InGame] 메뉴 팝업 숨김 및 게임 재개");
            }
        }

        private void OnResumeButtonClicked()
        {
            Debug.Log("[InGame] 계속하기 버튼 클릭");
            HideMenuPopup();
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("[InGame] 설정 버튼 클릭 - 설정 화면으로 이동");
            Time.timeScale = 1f; // 씬 전환 전 타임스케일 복구
            SceneManager.LoadScene("Options");
        }

        private void OnStoreButtonClicked()
        {
            Debug.Log("[InGame] 상점 버튼 클릭 - 상점 화면으로 이동");
            Time.timeScale = 1f; // 씬 전환 전 타임스케일 복구
            SceneManager.LoadScene("Store");
        }

        private void OnMainMenuButtonClicked()
        {
            Debug.Log("[InGame] 메인 메뉴 버튼 클릭 - 메인 메뉴로 이동");
            Time.timeScale = 1f; // 씬 전환 전 타임스케일 복구
            SceneManager.LoadScene("MainMenu");
        }

        private void ToggleSidebar()
        {
            if (leftSidebar == null)
                return;

            isSidebarVisible = !isSidebarVisible;

            if (isSidebarVisible)
            {
                leftSidebar.RemoveFromClassList("sidebar-hidden");
                Debug.Log("[InGame] 사이드바 표시");
            }
            else
            {
                leftSidebar.AddToClassList("sidebar-hidden");
                Debug.Log("[InGame] 사이드바 숨김");
            }
        }

        /// <summary>
        /// 스킬 목록 표시 (다른 목록들은 숨김)
        /// </summary>
        private void ShowSkillList(VisualElement listToShow)
        {
            if (attackSkillList != null)
                attackSkillList.AddToClassList("hidden");

            if (defenseSkillList != null)
                defenseSkillList.AddToClassList("hidden");

            if (supportSkillList != null)
                supportSkillList.AddToClassList("hidden");

            if (listToShow != null)
                listToShow.RemoveFromClassList("hidden");

            Debug.Log($"[InGame] 스킬 목록 전환: {listToShow?.name}");
        }

        #endregion
    }
}
