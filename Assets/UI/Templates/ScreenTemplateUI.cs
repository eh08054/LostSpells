using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace LostSpells.UI
{
    /*
    ========================================
    공통 화면 UI 컨트롤러 템플릿
    ========================================

    사용 방법:
    1. 이 파일을 복사해서 새 화면 폴더에 붙여넣기
    2. 클래스 이름을 [화면이름]UI로 변경
    3. 필요한 버튼/요소 변수 추가
    4. 이벤트 핸들러 구현

    예시:
    - public class GameModeSelectionUI : MonoBehaviour
    - public class StoryModeUI : MonoBehaviour
    - public class ChapterSelectUI : MonoBehaviour
    - public class OptionsUI : MonoBehaviour
    */

    /// <summary>
    /// [화면이름] UI 컨트롤러
    /// </summary>
    public class ScreenTemplateUI : MonoBehaviour
    {
        private UIDocument uiDocument;

        // 공통 요소
        private Button backButton;

        // ========== 여기에 화면별 UI 요소 변수 추가 ==========
        // 예시:
        // private Button myButton;
        // private Label myLabel;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;

            // 공통 요소 찾기
            backButton = root.Q<Button>("BackButton");

            // ========== 여기에 화면별 UI 요소 찾기 ==========
            // 예시:
            // myButton = root.Q<Button>("MyButton");

            // 이벤트 등록
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            // ========== 여기에 화면별 이벤트 등록 ==========
            // 예시:
            // if (myButton != null)
            //     myButton.clicked += OnMyButtonClicked;
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (backButton != null)
                backButton.clicked -= OnBackButtonClicked;

            // ========== 여기에 화면별 이벤트 해제 ==========
            // 예시:
            // if (myButton != null)
            //     myButton.clicked -= OnMyButtonClicked;
        }

        #region Button Click Handlers

        private void OnBackButtonClicked()
        {
            // TODO: SceneManager.LoadScene("PreviousScene");
        }

        // ========== 여기에 화면별 이벤트 핸들러 추가 ==========
        // 예시:
        // private void OnMyButtonClicked()
        // {
        //     Debug.Log("내 버튼 클릭");
        // }

        #endregion
    }
}
