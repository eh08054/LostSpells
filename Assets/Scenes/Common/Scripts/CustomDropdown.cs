using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace LostSpells.UI
{
    /// <summary>
    /// 재사용 가능한 커스텀 드롭다운 컴포넌트
    /// UXML에 정의된 드롭다운 구조를 제어
    /// </summary>
    public class CustomDropdown
    {
        private VisualElement container;
        private Button button;
        private Label label;
        private VisualElement list;

        private List<string> items = new List<string>();
        private string selectedValue;
        private System.Action<string> onValueChanged;

        /// <summary>
        /// 드롭다운 초기화
        /// </summary>
        /// <param name="root">루트 VisualElement</param>
        /// <param name="containerName">드롭다운 컨테이너 이름</param>
        /// <param name="buttonName">드롭다운 버튼 이름</param>
        /// <param name="labelName">드롭다운 라벨 이름</param>
        /// <param name="listName">드롭다운 리스트 이름</param>
        public CustomDropdown(VisualElement root, string containerName, string buttonName, string labelName, string listName)
        {
            container = root.Q<VisualElement>(containerName);
            button = root.Q<Button>(buttonName);
            label = root.Q<Label>(labelName);
            list = root.Q<VisualElement>(listName);

            if (button != null)
            {
                button.clicked += Toggle;
            }
        }

        /// <summary>
        /// 드롭다운 항목 설정
        /// </summary>
        public void SetItems(List<string> newItems, string defaultValue, System.Action<string> callback)
        {
            items = newItems;
            selectedValue = defaultValue;
            onValueChanged = callback;

            // 라벨 업데이트
            if (label != null)
            {
                label.text = selectedValue;
            }

            // 리스트 채우기
            PopulateList();
        }

        /// <summary>
        /// 현재 선택된 값 가져오기
        /// </summary>
        public string GetValue()
        {
            return selectedValue;
        }

        /// <summary>
        /// 값 설정 (프로그래밍 방식)
        /// </summary>
        public void SetValue(string value)
        {
            if (items.Contains(value))
            {
                selectedValue = value;
                if (label != null)
                {
                    label.text = value;
                }
            }
        }

        /// <summary>
        /// 드롭다운 토글
        /// </summary>
        public void Toggle()
        {
            if (list == null) return;

            bool isExpanded = list.ClassListContains("expanded");

            if (isExpanded)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// 드롭다운 열기
        /// </summary>
        public void Open()
        {
            if (list != null && button != null)
            {
                list.AddToClassList("expanded");
                button.AddToClassList("expanded");
            }
        }

        /// <summary>
        /// 드롭다운 닫기
        /// </summary>
        public void Close()
        {
            if (list != null && button != null)
            {
                list.RemoveFromClassList("expanded");
                button.RemoveFromClassList("expanded");
            }
        }

        /// <summary>
        /// 드롭다운이 열려있는지 확인
        /// </summary>
        public bool IsOpen()
        {
            return list != null && list.ClassListContains("expanded");
        }

        /// <summary>
        /// 리스트 항목 채우기
        /// </summary>
        private void PopulateList()
        {
            if (list == null) return;

            list.Clear();

            foreach (var item in items)
            {
                // 항목 컨테이너 생성
                VisualElement itemContainer = new VisualElement();
                itemContainer.AddToClassList("custom-dropdown-item-container");

                // 체크 표시 영역
                VisualElement checkmark = new VisualElement();
                checkmark.AddToClassList("custom-dropdown-checkmark");

                // 선택된 항목에만 체크 표시
                if (item == selectedValue)
                {
                    checkmark.AddToClassList("checked");
                }

                // 항목 버튼
                Button itemButton = new Button();
                itemButton.text = item;
                itemButton.AddToClassList("custom-dropdown-item");

                string currentItem = item; // 클로저 문제 방지
                itemButton.clicked += () =>
                {
                    selectedValue = currentItem;

                    if (label != null)
                    {
                        label.text = currentItem;
                    }

                    onValueChanged?.Invoke(currentItem);

                    // 모든 항목의 체크 표시 업데이트
                    UpdateCheckmarks();

                    Close();
                };

                // 컨테이너에 추가
                itemContainer.Add(checkmark);
                itemContainer.Add(itemButton);

                list.Add(itemContainer);
            }
        }

        /// <summary>
        /// 체크 표시 업데이트
        /// </summary>
        private void UpdateCheckmarks()
        {
            if (list == null) return;

            // 모든 항목 컨테이너를 순회
            for (int i = 0; i < list.childCount; i++)
            {
                var itemContainer = list[i];
                var checkmark = itemContainer.Q<VisualElement>(className: "custom-dropdown-checkmark");
                var itemButton = itemContainer.Q<Button>(className: "custom-dropdown-item");

                if (checkmark != null && itemButton != null)
                {
                    // 선택된 항목에만 체크 표시
                    if (itemButton.text == selectedValue)
                    {
                        checkmark.AddToClassList("checked");
                    }
                    else
                    {
                        checkmark.RemoveFromClassList("checked");
                    }
                }
            }
        }

        /// <summary>
        /// 이벤트 정리
        /// </summary>
        public void Dispose()
        {
            if (button != null)
            {
                button.clicked -= Toggle;
            }
        }
    }
}
