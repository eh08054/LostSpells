# InGame 씬 자동 설정 가이드

## Unity에서 자동 설정하기 (1분 만에 완료!)

### 1. Unity 에디터에서 메뉴 실행
1. Unity 상단 메뉴: **Tools > Setup InGame Scene**
2. 창이 열리면 **"Setup InGame Scene"** 버튼 클릭
3. 완료!

---

## 설정 내용

자동으로 다음 항목들이 생성됩니다:

### 1. 바닥 (Ground)
- 위치: (0, -4, 0)
- 크기: 20 x 1
- 색상: 갈색

### 2. 플레이어 (Player)
- 위치: (-7, 0, 0) - 화면 왼쪽
- Tag: "Player"
- 컴포넌트: `PlayerComponent`
- 색상: 파란색

### 3. Enemy Prefab
- **Assets/Prefabs/Enemy.prefab** 생성
- 이름 표시 (TextMeshPro)
- 체력바 (배경 + Fill)
- 컴포넌트: `EnemyComponent`
- 색상: 빨간색

### 4. EnemySpawner
- Enemy Prefab 자동 연결
- 웨이브 설정:
  - 스폰 간격: 2초
  - Wave 1: 5마리
  - 이후 웨이브마다 2마리씩 증가

### 5. 카메라 설정
- Projection: Orthographic
- Size: 5
- 위치: (0, 0, -10)

---

## 게임 실행

1. **Play** 버튼 클릭
2. Wave 1이 자동으로 시작됩니다
3. 적들이 오른쪽에서 생성되어 왼쪽으로 이동합니다

---

## 다음 웨이브 시작 방법

현재는 첫 웨이브만 자동으로 시작됩니다. 다음 웨이브를 시작하려면:

### 방법 1: 코드에서 호출
```csharp
InGameUI inGameUI = FindObjectOfType<InGameUI>();
inGameUI.StartNextWave();
```

### 방법 2: 웨이브 완료 시 자동 진행 (추후 구현 예정)
- 모든 적이 처치되면 자동으로 다음 웨이브 시작

---

## Enemy Prefab만 다시 만들기

Enemy Prefab만 다시 생성하려면:

1. **Tools > Setup InGame Scene** 열기
2. **"Create Enemy Prefab Only"** 버튼 클릭

---

## 커스터마이징

### 적 능력치 조정
**EnemySpawner** GameObject 선택 후 Inspector에서:
- Base Health: 기본 체력 (기본값: 50)
- Base Speed: 기본 속도 (기본값: 2)
- Spawn Interval: 스폰 간격 (기본값: 2초)
- Enemies Per Wave: 웨이브당 기본 적 수 (기본값: 5)

### 플레이어/적 색상 변경
각 GameObject 선택 후 **Sprite Renderer > Color** 변경

### 이미지 사용
1. 이미지를 `Assets/Sprites/` 폴더에 임포트
2. 이미지 선택 후 **Inspector**:
   - Texture Type: Sprite (2D and UI)
   - Apply 클릭
3. Player/Enemy의 **Sprite Renderer**:
   - Sprite 항목에 이미지 드래그

---

## 생성된 파일 위치

- **Scripts/Components/PlayerComponent.cs** - 플레이어 컴포넌트
- **Scripts/Components/EnemyComponent.cs** - 적 컴포넌트
- **Scripts/Systems/EnemySpawner.cs** - 웨이브 스폰 시스템
- **Scripts/Editor/InGameSceneSetup.cs** - 자동 설정 에디터 스크립트
- **Prefabs/Enemy.prefab** - 적 Prefab

---

## 문제 해결

### "Enemy Prefab이 설정되지 않았습니다!" 경고가 뜹니다
1. **Tools > Setup InGame Scene** 다시 실행
2. 또는 **Create Enemy Prefab Only** 실행 후
3. Hierarchy에서 **EnemySpawner** 선택
4. Inspector의 **Enemy Prefab** 항목에 `Assets/Prefabs/Enemy.prefab` 드래그

### 적이 생성되지 않습니다
1. Console에서 에러 확인
2. EnemySpawner의 **Start Wave** 메서드가 호출되는지 확인
3. Enemy Prefab이 올바르게 설정되어 있는지 확인

### Player Tag가 없습니다
1. Unity 상단: **Edit > Project Settings > Tags and Layers**
2. Tags 섹션에서 `+` 클릭
3. "Player" 입력 후 Save

---

## 완료!

이제 게임을 실행하면:
- ✅ 바닥이 보입니다
- ✅ 플레이어가 왼쪽에 있습니다
- ✅ 적들이 오른쪽에서 생성됩니다
- ✅ 적들이 플레이어 쪽으로 이동합니다
- ✅ 적 위에 이름이 표시됩니다
- ✅ 적 아래에 체력바가 표시됩니다
