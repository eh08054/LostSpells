# InGame UI

인게임 화면 UI입니다.

## 구성 요소

### 상단 HUD 바
- **왼쪽**: 메뉴 버튼 + 챕터 정보
  - 메뉴 버튼: 일시정지 메뉴 열기
  - 챕터 번호 (CHAPTER 1)
  - 챕터 이름 (Pride)

- **중앙**: 웨이브 진행상황 (완전 중앙 고정)
  - WAVE 라벨
  - 현재/총 웨이브 (1 / 10)

- **오른쪽**: 화폐 정보
  - 다이아몬드 개수
  - 부활석 개수

### 게임플레이 영역
메인 게임 플레이가 이루어지는 영역

### 하단 스킬 바
- 4개의 스킬 슬롯 (Q, W, E, R)
- 각 슬롯은 스킬 아이콘, 쿨다운 등을 표시 (TODO)

## 주요 기능

### Public 메서드
```csharp
// 챕터 정보 업데이트
UpdateChapterInfo(string number, string name)

// 웨이브 진행상황 업데이트
UpdateWaveProgress(int current, int total)

// 다음 웨이브로 진행
NextWave()

// 화폐 업데이트
UpdateCurrency(int diamonds, int reviveStones)

// 다이아몬드/부활석 추가
AddDiamonds(int amount)
AddReviveStones(int amount)
```

## 레이아웃 특징

상단 HUD는 absolute positioning을 사용하여:
- 왼쪽/오른쪽 요소가 중앙 요소를 밀지 않음
- 중앙 요소가 항상 정확히 화면 중앙에 위치
- 왼쪽/오른쪽 콘텐츠 길이와 무관하게 중앙 위치 고정

## TODO

- [ ] 일시정지 메뉴 팝업 구현
- [ ] 스테이지 완료 UI 구현
- [ ] 스킬 시스템 (아이콘, 쿨다운, 활성화/비활성화)
- [ ] 체력바/마나바 추가
- [ ] 미니맵 추가 (옵션)
- [ ] 보스전 UI (보스 체력바 등)
