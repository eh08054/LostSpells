# UI Toolkit 사용 가이드

## 📂 파일 구조 (화면별 관리)

```
Assets/
├── UI/
│   ├── MainMenu/                  # 메인메뉴 (화면별로 모든 파일 묶음)
│   │   ├── MainMenu.unity        # 씬
│   │   ├── MainMenu.uxml         # UI 레이아웃
│   │   ├── MainMenu.uss          # 화면별 스타일
│   │   └── MainMenuUI.cs         # 스크립트
│   │
│   ├── Common/                    # 공통 에셋
│   │   └── Common.uss            # 모든 화면에서 사용하는 공통 스타일
│   │
│   └── DefaultPanelSettings.asset
│
└── Documents/                     # 기획 문서
```

**핵심**: 각 화면(Scene + UI + Script)이 **한 폴더에 모여있습니다!**

---

## 🎨 메인메뉴 사용 방법

### 1. 씬 열기
- `UI/MainMenu/MainMenu.unity` 더블클릭

### 2. UI 확인
- Hierarchy에서 `UI Document` 선택
- Inspector에서 확인:
  - **Source Asset**: `MainMenu.uxml` (자동 설정됨)
  - **Panel Settings**: `DefaultPanelSettings` (자동 설정됨)

### 3. 완료!
USS는 UXML 안에 경로가 포함되어 **자동 적용**됩니다.

---

## 🎯 스타일 시스템

### Common.uss (공통 스타일)
- **위치**: `UI/Common/Common.uss`
- **용도**: 모든 화면에서 공유하는 스타일
- **내용**:
  - 색상 변수 (보라색 테마)
  - `.menu-button` - 기본 버튼 스타일
  - `.panel`, `.title-text` 등 재사용 클래스

### MainMenu.uss (화면별 스타일)
- **위치**: `UI/MainMenu/MainMenu.uss`
- **용도**: 메인메뉴만의 특별한 스타일
- **내용**:
  - `.main-menu` - 전체 배경
  - `.game-title` - "Lost Spells" 제목

**적용 순서**: Common.uss → MainMenu.uss (나중 것이 우선)

---

## ➕ 새 화면 추가하기 (예: 옵션 화면)

### 1. 폴더 생성
```
UI/Options/ 폴더 생성
```

### 2. 파일 생성

#### Options.uxml
```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="/Assets/UI/Common/Common.uss"/>
    <Style src="/Assets/UI/Options/Options.uss"/>

    <ui:VisualElement name="Options" class="options-screen">
        <!-- UI 요소들 -->
    </ui:VisualElement>
</ui:UXML>
```

#### Options.uss
```css
.options-screen {
    width: 100%;
    height: 100%;
    /* 옵션 화면만의 스타일 */
}
```

#### OptionsUI.cs
```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace LostSpell.UI
{
    public class OptionsUI : MonoBehaviour
    {
        private UIDocument uiDocument;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;
            // UI 요소 찾기 및 이벤트 등록
        }
    }
}
```

### 3. 씬 생성
1. File → New Scene
2. UI Document 추가
3. 씬을 `UI/Options/Options.unity`로 저장

### 4. 완료!
모든 관련 파일이 `UI/Options/` 폴더에 정리됩니다.

---

## 📋 화면별 폴더 구조 예시

```
UI/
├── MainMenu/          ✅ 메인메뉴
│   ├── MainMenu.unity
│   ├── MainMenu.uxml
│   ├── MainMenu.uss
│   └── MainMenuUI.cs
│
├── GameModeSelection/ 🔜 게임모드 선택
│   ├── GameModeSelection.unity
│   ├── GameModeSelection.uxml
│   ├── GameModeSelection.uss
│   └── GameModeSelectionUI.cs
│
├── Options/           🔜 옵션
│   ├── Options.unity
│   ├── Options.uxml
│   ├── Options.uss
│   └── OptionsUI.cs
│
└── Common/            📦 공통 에셋
    └── Common.uss
```

---

## ✨ 장점

✅ **한눈에 파악**: 메인메뉴 관련 모든 것이 한 폴더에
✅ **쉬운 관리**: 화면 추가/삭제가 간단
✅ **재사용성**: Common.uss로 일관된 디자인 유지
✅ **협업 친화적**: 각 화면을 독립적으로 작업 가능
✅ **Unity 표준**: 기본 UI Toolkit 방식 사용

---

## 🔧 문제 해결

### UI가 안 보일 때
1. Panel Settings가 설정되었는지 확인
2. UXML의 USS 경로가 맞는지 확인
3. UI Document의 Source Asset이 설정되었는지 확인

### 스타일이 적용 안 될 때
1. UXML 파일 상단의 `<Style src="..."/>` 경로 확인
2. Unity 에디터 새로고침 (Ctrl+R)
3. UI Builder에서 직접 USS 추가

**간단하고 깔끔한 구조입니다!** 🎉
