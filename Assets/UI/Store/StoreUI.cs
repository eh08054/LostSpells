using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace LostSpells.UI
{
    /// <summary>
    /// 상점 UI 컨트롤러
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoreUI : MonoBehaviour
    {
        private UIDocument uiDocument;

        // 버튼들
        private Button backButton;
        private Button diamondButton;
        private Button reviveStoneButton;

        // 패널들
        private ScrollView rightPanel;
        private VisualElement diamondPanel;
        private VisualElement reviveStonePanel;

        // 화폐 표시 (헤더)
        private Label headerDiamondsLabel;
        private Label headerReviveStonesLabel;

        // 다이아 구매 버튼들
        private Button buyDiamond100;
        private Button buyDiamond500;
        private Button buyDiamond1000;
        private Button buyDiamond5000;
        private Button buyDiamond10000;
        private Button buyDiamond50000;

        // 부활석 구매 버튼들
        private Button buyReviveStone1;
        private Button buyReviveStone5;
        private Button buyReviveStone10;
        private Button buyReviveStone50;
        private Button buyReviveStone100;
        private Button buyReviveStone500;

        // 임시 데이터 (실제로는 게임 매니저에서 관리)
        private int currentDiamonds = 0;
        private int currentReviveStones = 0;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // UI 요소 찾기
            FindUIElements(root);

            // 이벤트 등록
            RegisterEvents();

            // 초기화
            UpdateCurrencyDisplay();
            ShowPanel(diamondPanel);
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        #region UI Element Finding

        private void FindUIElements(VisualElement root)
        {
            // 버튼들
            backButton = root.Q<Button>("BackButton");
            diamondButton = root.Q<Button>("DiamondButton");
            reviveStoneButton = root.Q<Button>("ReviveStoneButton");

            // 패널들
            rightPanel = root.Q<ScrollView>("RightPanel");
            diamondPanel = root.Q<VisualElement>("DiamondPanel");
            reviveStonePanel = root.Q<VisualElement>("ReviveStonePanel");

            // 화폐 표시 (헤더)
            headerDiamondsLabel = root.Q<Label>("HeaderDiamonds");
            headerReviveStonesLabel = root.Q<Label>("HeaderReviveStones");

            // 다이아 구매 버튼들
            buyDiamond100 = root.Q<Button>("BuyDiamond100");
            buyDiamond500 = root.Q<Button>("BuyDiamond500");
            buyDiamond1000 = root.Q<Button>("BuyDiamond1000");
            buyDiamond5000 = root.Q<Button>("BuyDiamond5000");
            buyDiamond10000 = root.Q<Button>("BuyDiamond10000");
            buyDiamond50000 = root.Q<Button>("BuyDiamond50000");

            // 부활석 구매 버튼들
            buyReviveStone1 = root.Q<Button>("BuyReviveStone1");
            buyReviveStone5 = root.Q<Button>("BuyReviveStone5");
            buyReviveStone10 = root.Q<Button>("BuyReviveStone10");
            buyReviveStone50 = root.Q<Button>("BuyReviveStone50");
            buyReviveStone100 = root.Q<Button>("BuyReviveStone100");
            buyReviveStone500 = root.Q<Button>("BuyReviveStone500");
        }

        #endregion

        #region Event Registration

        private void RegisterEvents()
        {
            // 네비게이션 버튼
            if (backButton != null) backButton.clicked += OnBackButtonClicked;
            if (diamondButton != null) diamondButton.clicked += OnDiamondButtonClicked;
            if (reviveStoneButton != null) reviveStoneButton.clicked += OnReviveStoneButtonClicked;

            // 다이아 구매 버튼
            if (buyDiamond100 != null) buyDiamond100.clicked += () => OnBuyDiamond(100, 1000);
            if (buyDiamond500 != null) buyDiamond500.clicked += () => OnBuyDiamond(500, 4500);
            if (buyDiamond1000 != null) buyDiamond1000.clicked += () => OnBuyDiamond(1000, 8000);
            if (buyDiamond5000 != null) buyDiamond5000.clicked += () => OnBuyDiamond(5000, 35000);
            if (buyDiamond10000 != null) buyDiamond10000.clicked += () => OnBuyDiamond(10000, 65000);
            if (buyDiamond50000 != null) buyDiamond50000.clicked += () => OnBuyDiamond(50000, 300000);

            // 부활석 구매 버튼
            if (buyReviveStone1 != null) buyReviveStone1.clicked += () => OnBuyReviveStone(1, 100);
            if (buyReviveStone5 != null) buyReviveStone5.clicked += () => OnBuyReviveStone(5, 450);
            if (buyReviveStone10 != null) buyReviveStone10.clicked += () => OnBuyReviveStone(10, 800);
            if (buyReviveStone50 != null) buyReviveStone50.clicked += () => OnBuyReviveStone(50, 3500);
            if (buyReviveStone100 != null) buyReviveStone100.clicked += () => OnBuyReviveStone(100, 6500);
            if (buyReviveStone500 != null) buyReviveStone500.clicked += () => OnBuyReviveStone(500, 30000);
        }

        private void UnregisterEvents()
        {
            if (backButton != null) backButton.clicked -= OnBackButtonClicked;
            if (diamondButton != null) diamondButton.clicked -= OnDiamondButtonClicked;
            if (reviveStoneButton != null) reviveStoneButton.clicked -= OnReviveStoneButtonClicked;

            // 람다식으로 등록된 이벤트는 자동으로 해제됨
        }

        #endregion

        #region Panel Management

        private void ShowPanel(VisualElement panelToShow)
        {
            // 모든 패널 숨기기
            if (diamondPanel != null)
                diamondPanel.style.display = DisplayStyle.None;
            if (reviveStonePanel != null)
                reviveStonePanel.style.display = DisplayStyle.None;

            // 선택된 패널 표시
            if (panelToShow != null)
                panelToShow.style.display = DisplayStyle.Flex;

            // 스크롤 위치 초기화
            if (rightPanel != null)
                rightPanel.scrollOffset = Vector2.zero;
        }

        #endregion

        #region Purchase Logic

        private void OnBuyDiamond(int amount, int price)
        {
            Debug.Log($"다이아 구매 시도: {amount}개, 가격: ₩{price:N0}");

            // TODO: 실제 결제 로직 구현
            // 결제가 성공하면:
            currentDiamonds += amount;
            UpdateCurrencyDisplay();

            Debug.Log($"다이아 구매 완료! 현재 보유: {currentDiamonds}개");
        }

        private void OnBuyReviveStone(int amount, int cost)
        {
            if (currentDiamonds >= cost)
            {
                currentDiamonds -= cost;
                currentReviveStones += amount;
                UpdateCurrencyDisplay();

                Debug.Log($"부활석 구매 완료! {amount}개 구매, 다이아 {cost}개 소모");
            }
            else
            {
                Debug.Log($"다이아가 부족합니다! 필요: {cost}개, 보유: {currentDiamonds}개");
                // TODO: 다이아 부족 메시지 표시
            }
        }

        private void UpdateCurrencyDisplay()
        {
            if (headerDiamondsLabel != null)
                headerDiamondsLabel.text = currentDiamonds.ToString("N0");

            if (headerReviveStonesLabel != null)
                headerReviveStonesLabel.text = currentReviveStones.ToString("N0");
        }

        #endregion

        #region Button Click Handlers

        private void OnBackButtonClicked() => SceneManager.LoadScene("MainMenu");
        private void OnDiamondButtonClicked() => ShowPanel(diamondPanel);
        private void OnReviveStoneButtonClicked() => ShowPanel(reviveStonePanel);

        #endregion
    }
}
