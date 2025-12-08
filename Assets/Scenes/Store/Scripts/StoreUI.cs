using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using LostSpells.Data;
using LostSpells.Systems;

namespace LostSpells.UI
{
    /// <summary>
    /// 상점 UI 컨트롤러 - 아이템 구매 및 화폐 표시
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoreUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Button backButton;

        // 헤더 화폐 표시
        private Label headerDiamondsLabel;
        private Label headerReviveStonesLabel;

        // 카테고리 버튼
        private Button diamondButton;
        private Button reviveStoneButton;

        // 상품 패널
        private VisualElement diamondPanel;
        private VisualElement reviveStonePanel;

        // 상품 그리드
        private VisualElement diamondProductGrid;
        private VisualElement reviveStoneProductGrid;

        private PlayerSaveData saveData;

        // 상품 데이터
        private List<StoreItemData> diamondItems;
        private List<StoreItemData> reviveStoneItems;

        // 배경 레이어 (패럴랙스 스크롤용)
        private VisualElement skyLayer;
        private VisualElement mountainLayer;
        private VisualElement groundLayer;

        // 배경 스크롤 오프셋
        private float skyOffset = 0f;
        private float mountainOffset = 0f;
        private float groundOffset = 0f;

        // 다이아 부족 팝업
        private VisualElement insufficientDiamondPopup;
        private Button confirmInsufficientDiamondButton;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Update()
        {
            // 배경 스크롤 업데이트 (패럴랙스 효과)
            UpdateBackgroundScroll();
        }

        /// <summary>
        /// 배경 스크롤 업데이트 (패럴랙스 효과)
        /// </summary>
        private void UpdateBackgroundScroll()
        {
            if (skyLayer == null || mountainLayer == null || groundLayer == null)
                return;

            // Time.unscaledDeltaTime을 사용하여 일시정지 상태에서도 스크롤
            float deltaTime = Time.unscaledDeltaTime;

            // 각 레이어별 스크롤 속도 (퍼센트/초) - 메인메뉴와 동일한 시각적 속도를 위해 절반으로 설정 (200% 너비 보정)
            float skySpeed = 1f;
            float mountainSpeed = 2.5f;
            float groundSpeed = 5f;

            // 오프셋 업데이트 (0~100% 사이를 순환)
            skyOffset += skySpeed * deltaTime;
            mountainOffset += mountainSpeed * deltaTime;
            groundOffset += groundSpeed * deltaTime;

            // 100%를 넘으면 0으로 리셋 (레이어 너비가 200%이므로 100%가 한 사이클)
            if (skyOffset >= 100f) skyOffset = 0f;
            if (mountainOffset >= 100f) mountainOffset = 0f;
            if (groundOffset >= 100f) groundOffset = 0f;

            // translate로 X축 이동 (퍼센트 단위)
            skyLayer.style.translate = new Translate(new Length(-skyOffset, LengthUnit.Percent), 0);
            mountainLayer.style.translate = new Translate(new Length(-mountainOffset, LengthUnit.Percent), 0);
            groundLayer.style.translate = new Translate(new Length(-groundOffset, LengthUnit.Percent), 0);
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // 배경 레이어 찾기
            skyLayer = root.Q<VisualElement>("Sky");
            mountainLayer = root.Q<VisualElement>("Mountain");
            groundLayer = root.Q<VisualElement>("Ground");

            // UI 요소 찾기
            backButton = root.Q<Button>("BackButton");
            headerDiamondsLabel = root.Q<Label>("HeaderDiamonds");
            headerReviveStonesLabel = root.Q<Label>("HeaderReviveStones");

            diamondButton = root.Q<Button>("DiamondButton");
            reviveStoneButton = root.Q<Button>("ReviveStoneButton");

            diamondPanel = root.Q<VisualElement>("DiamondPanel");
            reviveStonePanel = root.Q<VisualElement>("ReviveStonePanel");

            // 상품 그리드 찾기
            if (diamondPanel != null)
                diamondProductGrid = diamondPanel.Q<VisualElement>("ProductGrid");
            if (reviveStonePanel != null)
                reviveStoneProductGrid = reviveStonePanel.Q<VisualElement>("ProductGrid");

            // 다이아 부족 팝업 찾기
            insufficientDiamondPopup = root.Q<VisualElement>("InsufficientDiamondPopup");
            confirmInsufficientDiamondButton = root.Q<Button>("ConfirmInsufficientDiamondButton");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            if (diamondButton != null)
                diamondButton.clicked += OnDiamondButtonClicked;

            if (reviveStoneButton != null)
                reviveStoneButton.clicked += OnReviveStoneButtonClicked;

            if (confirmInsufficientDiamondButton != null)
                confirmInsufficientDiamondButton.clicked += OnConfirmInsufficientDiamondButtonClicked;

            // 상품 데이터 초기화
            InitializeStoreItems();

            // 저장 데이터 가져오기 및 UI 업데이트
            saveData = SaveManager.Instance.GetCurrentSaveData();
            UpdateCurrencyDisplay();

            // 상품 카드 생성
            PopulateProductGrid();

            // 기본적으로 다이아몬드 패널 표시
            ShowDiamondPanel();

            // Localization 이벤트 등록
            LocalizationManager.Instance.OnLanguageChanged += UpdateLocalization;

            // 현재 언어로 UI 업데이트
            UpdateLocalization();
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            if (diamondButton != null)
                diamondButton.clicked -= OnDiamondButtonClicked;

            if (reviveStoneButton != null)
                reviveStoneButton.clicked -= OnReviveStoneButtonClicked;

            if (confirmInsufficientDiamondButton != null)
                confirmInsufficientDiamondButton.clicked -= OnConfirmInsufficientDiamondButtonClicked;

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

        /// <summary>
        /// 로컬라이제이션 업데이트
        /// </summary>
        private void UpdateLocalization()
        {
            var loc = LocalizationManager.Instance;

            // Title/Header 업데이트
            var root = uiDocument.rootVisualElement;
            var headerTitle = root.Q<Label>("HeaderTitle");
            if (headerTitle != null)
                headerTitle.text = loc.GetText("store_title");

            // Category buttons 업데이트
            if (diamondButton != null)
                diamondButton.text = loc.GetText("store_diamond");

            if (reviveStoneButton != null)
                reviveStoneButton.text = loc.GetText("store_revive_stone");

            // 다이아 부족 팝업 텍스트 업데이트
            if (insufficientDiamondPopup != null)
            {
                var insufficientTitle = insufficientDiamondPopup.Q<Label>("InsufficientDiamondTitle");
                if (insufficientTitle != null)
                    insufficientTitle.text = loc.GetText("insufficient_diamond_title");

                var insufficientMessage = insufficientDiamondPopup.Q<Label>("InsufficientDiamondMessage");
                if (insufficientMessage != null)
                    insufficientMessage.text = loc.GetText("insufficient_diamond_message");

                if (confirmInsufficientDiamondButton != null)
                    confirmInsufficientDiamondButton.text = loc.GetText("confirm");
            }

            // BackButton은 이미지만 사용하므로 텍스트 설정 안함
        }

        /// <summary>
        /// 화폐 표시 업데이트
        /// </summary>
        private void UpdateCurrencyDisplay()
        {
            if (saveData != null)
            {
                if (headerDiamondsLabel != null)
                    headerDiamondsLabel.text = saveData.diamonds.ToString();

                if (headerReviveStonesLabel != null)
                    headerReviveStonesLabel.text = saveData.reviveStones.ToString();
            }
        }

        /// <summary>
        /// 다이아몬드 패널 표시
        /// </summary>
        private void ShowDiamondPanel()
        {
            if (diamondPanel != null)
                diamondPanel.style.display = DisplayStyle.Flex;

            if (reviveStonePanel != null)
                reviveStonePanel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 부활석 패널 표시
        /// </summary>
        private void ShowReviveStonePanel()
        {
            if (diamondPanel != null)
                diamondPanel.style.display = DisplayStyle.None;

            if (reviveStonePanel != null)
                reviveStonePanel.style.display = DisplayStyle.Flex;
        }

        private void OnDiamondButtonClicked()
        {
            ShowDiamondPanel();
        }

        private void OnReviveStoneButtonClicked()
        {
            ShowReviveStonePanel();
        }

        private void OnBackButtonClicked()
        {
            // Store 씬이 Additive로 로드되었는지 확인
            bool isLoadedAdditively = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == "Store" && SceneManager.sceneCount > 1)
                {
                    isLoadedAdditively = true;
                    break;
                }
            }

            if (isLoadedAdditively)
            {
                // Additive로 로드된 경우 - 자신을 언로드
                // Time.timeScale은 InGame에서 이미 0으로 설정되어 있음 (일시정지 상태 유지)
                SceneManager.UnloadSceneAsync("Store");
            }
            else
            {
                // 단독으로 로드된 경우 - 이전 씬으로 이동
                string previousScene = SceneNavigationManager.Instance.GetPreviousScene();
                SceneManager.LoadScene(previousScene);
            }
        }

        // ========== 상품 데이터 초기화 및 UI 생성 ==========

        /// <summary>
        /// 상점 상품 데이터 초기화
        /// </summary>
        private void InitializeStoreItems()
        {
            // 다이아몬드 상품 목록 (실제 돈으로 구매)
            diamondItems = new List<StoreItemData>
            {
                new StoreItemData("diamond_1", "Small", StoreItemType.Diamond, 1, 1100, PriceType.RealMoney),
                new StoreItemData("diamond_5", "Medium", StoreItemType.Diamond, 5, 5000, PriceType.RealMoney),
                new StoreItemData("diamond_10", "Large", StoreItemType.Diamond, 10, 9000, PriceType.RealMoney),
                new StoreItemData("diamond_25", "Huge", StoreItemType.Diamond, 25, 20000, PriceType.RealMoney),
                new StoreItemData("diamond_50", "Mountain", StoreItemType.Diamond, 50, 35000, PriceType.RealMoney),
                new StoreItemData("diamond_100", "Treasure", StoreItemType.Diamond, 100, 60000, PriceType.RealMoney)
            };

            // 게임 내 아이템 목록 (다이아몬드로 구매)
            reviveStoneItems = new List<StoreItemData>
            {
                new StoreItemData("revive_1", "Single", StoreItemType.ReviveStone, 1, 1, PriceType.Diamond),
                new StoreItemData("revive_5", "Small Pack", StoreItemType.ReviveStone, 5, 4, PriceType.Diamond),
                new StoreItemData("revive_10", "Medium Pack", StoreItemType.ReviveStone, 10, 8, PriceType.Diamond),
                new StoreItemData("revive_25", "Large Pack", StoreItemType.ReviveStone, 25, 18, PriceType.Diamond),
                new StoreItemData("revive_50", "Huge Pack", StoreItemType.ReviveStone, 50, 32, PriceType.Diamond),
                new StoreItemData("revive_100", "Premium Pack", StoreItemType.ReviveStone, 100, 55, PriceType.Diamond)
            };
        }

        /// <summary>
        /// 상품 그리드 채우기
        /// </summary>
        private void PopulateProductGrid()
        {
            // 다이아몬드 상품 카드 생성
            if (diamondProductGrid != null)
            {
                diamondProductGrid.Clear();
                foreach (var item in diamondItems)
                {
                    var productCard = CreateProductCard(item);
                    diamondProductGrid.Add(productCard);
                }
            }

            // 부활석 상품 카드 생성
            if (reviveStoneProductGrid != null)
            {
                reviveStoneProductGrid.Clear();
                foreach (var item in reviveStoneItems)
                {
                    var productCard = CreateProductCard(item);
                    reviveStoneProductGrid.Add(productCard);
                }
            }
        }

        /// <summary>
        /// 상품 카드 생성
        /// </summary>
        private VisualElement CreateProductCard(StoreItemData item)
        {
            // 카드 컨테이너
            var card = new VisualElement();
            card.AddToClassList("product-card");

            // Best Value 배지
            if (item.isBestValue)
            {
                var badge = new Label("BEST VALUE");
                badge.AddToClassList("best-value-badge");
                card.Add(badge);
            }

            // 아이템 아이콘과 수량을 가로로 배치
            var infoRow = new VisualElement();
            infoRow.AddToClassList("product-info-row");

            // 아이템 아이콘
            var icon = new VisualElement();
            icon.AddToClassList("product-icon-large");
            if (item.itemType == StoreItemType.Diamond)
            {
                icon.AddToClassList("diamond-icon-product");
            }
            else
            {
                icon.AddToClassList("revive-stone-icon-product");
            }
            infoRow.Add(icon);

            // X 표시
            var multiplication = new Label("X");
            multiplication.AddToClassList("multiplication-sign");
            infoRow.Add(multiplication);

            // 아이템 수량
            var quantity = new Label($"{item.quantity:N0}");
            quantity.AddToClassList("product-quantity");
            infoRow.Add(quantity);

            card.Add(infoRow);

            // 가격 (PriceType에 따라 다르게 표시)
            if (item.priceType == PriceType.RealMoney)
            {
                // 실제 돈 가격 (텍스트만)
                var price = new Label($"₩{item.price:N0}");
                price.AddToClassList("product-price");
                card.Add(price);
            }
            else
            {
                // 다이아몬드 가격 (아이콘 X 숫자)
                var priceContainer = new VisualElement();
                priceContainer.AddToClassList("product-price-container");

                var priceIcon = new VisualElement();
                priceIcon.AddToClassList("product-price-icon");
                priceContainer.Add(priceIcon);

                var priceMultiplication = new Label("X");
                priceMultiplication.AddToClassList("price-multiplication-sign");
                priceContainer.Add(priceMultiplication);

                var priceText = new Label($"{item.price:N0}");
                priceText.AddToClassList("product-price-text");
                priceContainer.Add(priceText);

                card.Add(priceContainer);
            }

            // 구매 버튼
            var buyButton = new Button();
            buyButton.text = "BUY";
            buyButton.AddToClassList("buy-button");
            buyButton.clicked += () => OnBuyButtonClicked(item);
            card.Add(buyButton);

            return card;
        }

        /// <summary>
        /// 구매 버튼 클릭
        /// </summary>
        private void OnBuyButtonClicked(StoreItemData item)
        {
            if (item.priceType == PriceType.RealMoney)
            {
                // TODO: 실제 결제 로직 구현 (IAP)
                // 현재는 테스트로 바로 다이아몬드 지급
                SaveManager.Instance.AddDiamonds(item.quantity);
            }
            else // PriceType.Diamond
            {
                // 다이아몬드 차감
                if (SaveManager.Instance.SpendDiamonds(item.price))
                {
                    // 구매 성공 - 아이템 지급
                    if (item.itemType == StoreItemType.ReviveStone)
                    {
                        SaveManager.Instance.AddReviveStones(item.quantity);
                    }
                    else if (item.itemType == StoreItemType.Gold)
                    {
                        SaveManager.Instance.AddGold(item.quantity);
                    }
                }
                else
                {
                    // 다이아 부족 팝업 표시
                    ShowInsufficientDiamondPopup();
                }
            }

            // UI 업데이트
            saveData = SaveManager.Instance.GetCurrentSaveData();
            UpdateCurrencyDisplay();
        }

        /// <summary>
        /// 다이아 부족 팝업 표시
        /// </summary>
        private void ShowInsufficientDiamondPopup()
        {
            if (insufficientDiamondPopup != null)
            {
                insufficientDiamondPopup.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// 다이아 부족 팝업 숨김
        /// </summary>
        private void HideInsufficientDiamondPopup()
        {
            if (insufficientDiamondPopup != null)
            {
                insufficientDiamondPopup.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// 다이아 부족 팝업 확인 버튼 클릭
        /// </summary>
        private void OnConfirmInsufficientDiamondButtonClicked()
        {
            HideInsufficientDiamondPopup();
        }

        // ========== 테스트용 메서드 (나중에 실제 구매 로직으로 교체) ==========

        /// <summary>
        /// 다이아몬드 추가 (테스트용)
        /// </summary>
        public void TestAddDiamonds(int amount)
        {
            SaveManager.Instance.AddDiamonds(amount);
            saveData = SaveManager.Instance.GetCurrentSaveData();
            UpdateCurrencyDisplay();
        }

        /// <summary>
        /// 부활석 추가 (테스트용)
        /// </summary>
        public void TestAddReviveStones(int amount)
        {
            SaveManager.Instance.AddReviveStones(amount);
            saveData = SaveManager.Instance.GetCurrentSaveData();
            UpdateCurrencyDisplay();
        }
        public void backOrdered()
        {
            OnBackButtonClicked();
        }
    }
}
