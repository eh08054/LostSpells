# UI 템플릿 가이드

## 📋 개요

새로운 화면을 만들 때 사용하는 **템플릿 파일들**입니다.

---

## 🎨 공통 레이아웃 구조

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  [왼쪽 1/3]   [중앙 1/3]      [오른쪽 1/3]    ┃ ← 헤더 바 (80px)
┃   ┌───┐                                      ┃
┃   │ < │      화면 제목                        ┃
┃   └───┘                                      ┃
┃━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┃
┃                                             ┃
┃                                             ┃
┃            컨텐츠 영역                       ┃
┃        (화면별로 자유롭게 구성)              ┃
┃                                             ┃
┃                                             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

### 헤더 구조:
- **왼쪽 영역** (33.33%): 돌아가기 버튼 (60x60px, `<` 모양)
- **중앙 영역** (33.33%): 화면 제목 (중앙 정렬)
- **오른쪽 영역** (33.33%): 추가 UI (상점 아이템, 코인 등)

---

## 🚀 새 화면 만들기

### 1단계: 폴더 생성
```
UI/[화면이름]/ 폴더 생성
예시: UI/StoryMode/
```

### 2단계: 템플릿 파일 복사

#### UXML 복사
- `Templates/ScreenTemplate.uxml` → `[화면이름]/[화면이름].uxml`
- 파일 내용 수정:
  - `[화면이름]` → 실제 화면 이름으로 변경
  - `"화면 제목"` → 실제 제목으로 변경
  - USS 경로의 주석 해제 및 수정
  - `ContentArea` 안에 화면별 UI 추가

#### 스크립트 복사
- `Templates/ScreenTemplateUI.cs` → `[화면이름]/[화면이름]UI.cs`
- 클래스 이름 변경:
  ```csharp
  public class ScreenTemplateUI : MonoBehaviour
  ↓
  public class StoryModeUI : MonoBehaviour
  ```

#### USS 생성
- `[화면이름]/[화면이름].uss` 파일 생성
- 화면별 스타일 추가

### 3단계: 씬 생성
1. File → New Scene
2. Hierarchy → UI Toolkit → UI Document 추가
3. Inspector:
   - Source Asset: `[화면이름].uxml` 드래그
   - Panel Settings: `PanelSettings.asset` 드래그
4. UIDocument에 `[화면이름]UI.cs` 스크립트 추가
5. 씬 저장: `UI/[화면이름]/[화면이름].unity`

---

## 📝 예시: 게임모드 선택 화면

### GameModeSelection.uxml
```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements"
        editor-extension-mode="False">
    <Style src="/Assets/UI/Common/Common.uss"/>
    <Style src="/Assets/UI/GameModeSelection/GameModeSelection.uss"/>

    <ui:VisualElement name="GameModeSelection" class="screen-container">
        <ui:VisualElement name="HeaderBar" class="header-bar">
            <ui:VisualElement name="HeaderLeft" class="header-side header-left">
                <ui:Button text="&lt;" name="BackButton" class="back-button"/>
            </ui:VisualElement>

            <ui:Label text="게임 모드 선택" name="HeaderTitle" class="header-title"/>

            <ui:VisualElement name="HeaderRight" class="header-side"/>
        </ui:VisualElement>

        <ui:VisualElement name="ContentArea" class="content-area">
            <!-- 여기에 화면별 UI 추가 -->
            <ui:Button text="스토리 모드" name="StoryModeButton" class="menu-button"/>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

---

## 🎯 공통 CSS 클래스 (Common.uss)

### 레이아웃
- `.screen-container` - 전체 화면 컨테이너
- `.header-bar` - 상단 헤더 바 (3등분)
- `.header-side` - 헤더 좌우 영역 (각 33.33%)
- `.header-left` - 헤더 왼쪽 영역 (왼쪽 정렬)
- `.header-right` - 헤더 오른쪽 영역 (오른쪽 정렬)
- `.header-title` - 헤더 중앙 제목
- `.content-area` - 컨텐츠 영역

### 버튼
- `.back-button` - 돌아가기 버튼
- `.menu-button` - 일반 메뉴 버튼

---

## ✅ 만들어야 할 화면 목록

- [x] **MainMenu** - 메인메뉴
- [x] **GameModeSelection** - 게임모드 선택
- [ ] **StoryMode** - 스토리 모드 (저장 슬롯)
- [ ] **ChapterSelect** - 챕터 선택
- [ ] **EndlessMode** - 무한 모드
- [ ] **Options** - 옵션
- [ ] **Store** - 상점

---

## 💡 팁

1. **헤더 3등분**: 왼쪽, 중앙, 오른쪽이 각각 33.33%씩 차지
2. **오른쪽 영역 활용**: 상점에서 코인/아이템 표시 가능
3. **컨텐츠 중앙 정렬**: `content-area`는 기본적으로 중앙 정렬
4. **Common.uss**: 모든 공통 스타일은 여기서 관리

**템플릿으로 일관성 있는 UI를 만드세요!** 🎨
