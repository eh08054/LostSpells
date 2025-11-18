using UnityEngine;
using UnityEngine.UIElements;

namespace LostSpells.UI
{
    /// <summary>
    /// 재사용 가능한 접기/펼치기 섹션 컴포넌트
    /// 헤더를 클릭하면 컨텐츠 영역이 토글됨
    /// </summary>
    public class CollapsibleSection
    {
        private Label headerLabel;
        private Button toggleButton;
        private VisualElement contentArea;
        private bool isExpanded = false;

        /// <summary>
        /// 접기/펼치기 섹션 초기화
        /// </summary>
        /// <param name="root">루트 VisualElement</param>
        /// <param name="headerLabelName">헤더 라벨 이름</param>
        /// <param name="toggleButtonName">토글 버튼 이름</param>
        /// <param name="contentAreaName">컨텐츠 영역 이름</param>
        /// <param name="startExpanded">시작 시 펼쳐진 상태인지 여부</param>
        public CollapsibleSection(VisualElement root, string headerLabelName, string toggleButtonName, string contentAreaName, bool startExpanded = false)
        {
            headerLabel = root.Q<Label>(headerLabelName);
            toggleButton = root.Q<Button>(toggleButtonName);
            contentArea = root.Q<VisualElement>(contentAreaName);

            isExpanded = startExpanded;

            // 초기 상태 설정
            UpdateVisibility();

            // 이벤트 등록
            if (headerLabel != null)
            {
                headerLabel.RegisterCallback<ClickEvent>(evt => Toggle());
            }

            if (toggleButton != null)
            {
                toggleButton.clicked += Toggle;
            }
        }

        /// <summary>
        /// 섹션 토글
        /// </summary>
        public void Toggle()
        {
            isExpanded = !isExpanded;
            UpdateVisibility();
        }

        /// <summary>
        /// 섹션 펼치기
        /// </summary>
        public void Expand()
        {
            if (!isExpanded)
            {
                isExpanded = true;
                UpdateVisibility();
            }
        }

        /// <summary>
        /// 섹션 접기
        /// </summary>
        public void Collapse()
        {
            if (isExpanded)
            {
                isExpanded = false;
                UpdateVisibility();
            }
        }

        /// <summary>
        /// 현재 펼쳐진 상태인지 확인
        /// </summary>
        public bool IsExpanded()
        {
            return isExpanded;
        }

        /// <summary>
        /// 가시성 업데이트
        /// </summary>
        private void UpdateVisibility()
        {
            if (contentArea != null)
            {
                contentArea.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (toggleButton != null)
            {
                if (isExpanded)
                {
                    toggleButton.AddToClassList("expanded");
                }
                else
                {
                    toggleButton.RemoveFromClassList("expanded");
                }
            }
        }

        /// <summary>
        /// 이벤트 정리
        /// </summary>
        public void Dispose()
        {
            if (headerLabel != null)
            {
                headerLabel.UnregisterCallback<ClickEvent>(evt => Toggle());
            }

            if (toggleButton != null)
            {
                toggleButton.clicked -= Toggle;
            }
        }
    }
}
