# InGame 씬 설정 가이드

## 1. 바닥 (Ground) 설정

### 방법 1: Sprite 사용
1. **Hierarchy**에서 **Create > 2D Object > Sprite > Square**
2. 이름을 `Ground`로 변경
3. **Transform** 설정:
   - Position: (0, -4, 0)
   - Scale: (20, 1, 1)
4. **Sprite Renderer** 색상:
   - Color: 갈색 또는 원하는 색 (예: #8B4513)

### 방법 2: 이미지 사용
1. 바닥 이미지를 `Assets/Sprites/` 폴더에 임포트
2. **Hierarchy**에서 **Create > 2D Object > Sprite**
3. Sprite Renderer의 Sprite 항목에 임포트한 이미지 드래그

---

## 2. 플레이어 캐릭터 설정

1. **Hierarchy**에서 **Create > 2D Object > Sprite > Square**
2. 이름을 `Player`로 변경
3. **Tag**를 `Player`로 설정 (Inspector 상단)
   - Tag가 없으면: Tags > Add Tag > `Player` 생성
4. **Transform** 설정:
   - Position: (-7, 0, 0) - 화면 왼쪽
   - Scale: (1, 1, 1)
5. **Add Component**: `PlayerComponent` 스크립트 추가
6. **Sprite Renderer** 색상:
   - Color: 파란색 (또는 원하는 색)

---

## 3. 적 Prefab 만들기

### 3-1. 적 GameObject 생성
1. **Hierarchy**에서 **Create > 2D Object > Sprite > Square**
2. 이름을 `Enemy`로 변경
3. **Transform** 설정:
   - Scale: (0.8, 0.8, 1)

### 3-2. 적 이름 텍스트 추가
1. `Enemy` GameObject를 선택한 상태에서 **우클릭 > 3D Object > 3D Text**
2. 이름을 `NameText`로 변경
3. **Transform** 설정:
   - Position: (0, 0.6, 0) - 적 위쪽
   - Scale: (0.1, 0.1, 0.1)
4. **Text Mesh** 설정:
   - Text: "Enemy"
   - Font Size: 50
   - Anchor: Middle Center
   - Alignment: Center
   - Color: White

### 3-3. 체력바 배경 추가
1. `Enemy` GameObject를 선택한 상태에서 **우클릭 > 2D Object > Sprite > Square**
2. 이름을 `HealthBarBackground`로 변경
3. **Transform** 설정:
   - Position: (0, -0.5, 0) - 적 아래쪽
   - Scale: (1, 0.1, 1)
4. **Sprite Renderer**:
   - Color: Dark Gray (#333333)

### 3-4. 체력바 Fill 추가
1. `HealthBarBackground`를 선택한 상태에서 **우클릭 > 2D Object > Sprite > Square**
2. 이름을 `HealthBarFill`로 변경
3. **Transform** 설정:
   - Position: (0, 0, -0.1)
   - Scale: (1, 1, 1)
4. **Sprite Renderer**:
   - Color: Green (#00FF00)

### 3-5. Enemy Component 추가
1. `Enemy` GameObject를 선택
2. **Add Component**: `EnemyComponent` 스크립트 추가
3. **Inspector**에서 다음 항목 연결:
   - Name Text: `NameText` GameObject 드래그
   - Health Bar Background: `HealthBarBackground` GameObject 드래그
   - Health Bar Fill: `HealthBarFill` GameObject 드래그

### 3-6. Prefab 저장
1. `Assets` 폴더에 `Prefabs` 폴더 생성 (없다면)
2. Hierarchy의 `Enemy` GameObject를 `Assets/Prefabs/` 폴더로 드래그
3. Hierarchy에서 `Enemy` GameObject 삭제

---

## 4. Enemy Spawner 설정

1. **Hierarchy**에서 **Create > Create Empty**
2. 이름을 `EnemySpawner`로 변경
3. **Transform** 설정:
   - Position: (0, 0, 0)
4. **Add Component**: `EnemySpawner` 스크립트 추가
5. **Inspector**에서 설정:
   - Enemy Prefab: `Assets/Prefabs/Enemy` Prefab 드래그
   - Spawn Interval: 2
   - Enemies Per Wave: 5

---

## 5. 웨이브 시스템 연동

### InGameUI 스크립트 수정
다음 웨이브 시작 시 EnemySpawner 호출:

```csharp
private EnemySpawner enemySpawner;

private void Start()
{
    enemySpawner = FindObjectOfType<EnemySpawner>();

    // 첫 웨이브 시작
    if (enemySpawner != null)
    {
        enemySpawner.StartWave(1);
    }
}
```

---

## 6. 카메라 설정

1. **Main Camera** 선택
2. **Camera** Component:
   - Projection: Orthographic
   - Size: 5
3. **Transform**:
   - Position: (0, 0, -10)

---

## 7. 테스트

1. **Play** 버튼 클릭
2. 확인 사항:
   - 플레이어가 화면 왼쪽에 있는지
   - 적이 화면 오른쪽에서 생성되는지
   - 적이 왼쪽으로 이동하는지
   - 적 위에 이름이 표시되는지
   - 적 아래에 체력바가 표시되는지

---

## 8. 선택사항: Sprite 이미지 사용

캐릭터와 적에 실제 이미지를 사용하려면:

1. 이미지를 `Assets/Sprites/` 폴더에 임포트
2. 이미지 선택 후 **Inspector**:
   - Texture Type: Sprite (2D and UI)
   - Apply 클릭
3. Player/Enemy GameObject의 **Sprite Renderer**:
   - Sprite 항목에 이미지 드래그

---

## 완료!

이제 인게임 화면에서:
- 바닥이 보입니다
- 플레이어가 왼쪽에 배치됩니다
- 웨이브마다 적들이 오른쪽에서 생성되어 왼쪽으로 이동합니다
- 적마다 이름과 체력바가 표시됩니다
