# 공통 스타일시트 (Common.uss)

## 📋 개요

모든 UI 화면에서 사용하는 **공통 스타일시트**입니다.

---

## 📁 파일

- **Common.uss** - 전역 CSS 변수 및 공통 스타일 정의

---

## 🎨 포함된 스타일

### CSS 변수
```css
--color-white: rgb(255, 255, 255)
--color-black: rgb(0, 0, 0)
--color-gray-light: rgb(240, 240, 240)
--color-gray-medium: rgb(220, 220, 220)
--spacing-small: 8px
--spacing-medium: 16px
--spacing-large: 24px
--spacing-xlarge: 32px
--border-width: 2px
--font-size-medium: 18px
--font-size-xlarge: 48px
```

### 레이아웃 클래스
- `.screen-container` - 전체 화면 컨테이너
- `.header-bar` - 상단 헤더 바 (3등분 구조)
- `.header-side` - 헤더 좌우 영역
- `.header-left` - 왼쪽 영역 (왼쪽 정렬)
- `.header-right` - 오른쪽 영역 (오른쪽 정렬)
- `.header-title` - 중앙 제목
- `.content-area` - 컨텐츠 영역

### 버튼 클래스
- `.menu-button` - 일반 메뉴 버튼 (흰 배경, 검은 테두리)
- `.back-button` - 돌아가기 버튼 (< 모양)

---

## 🔧 사용법

모든 UXML 파일의 상단에 추가:

```xml
<Style src="/Assets/UI/Common/Common.uss"/>
```

---

## 💡 스타일 추가 가이드

### 새로운 공통 스타일 추가 시:

1. **CSS 변수**: `:root` 섹션에 추가
2. **버튼 스타일**: `버튼 스타일` 섹션에 추가
3. **레이아웃**: `화면 레이아웃` 섹션에 추가

### 화면별 스타일:

각 화면 폴더의 `.uss` 파일에 추가 (예: `MainMenu.uss`)

---

## ⚠️ 주의사항

- 이 파일은 **모든 UI 화면에 영향**을 줍니다
- 변경 시 모든 화면을 테스트해주세요
- 화면별 스타일은 각 화면의 USS 파일에 작성하세요
