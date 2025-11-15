# Lost Spells - Data Management Structure

음성 인식 기반 스킬 매칭 게임의 데이터 관리 시스템 문서

## 📁 폴더 구조

```
Assets/Data/
├── Config/                  # 설정 파일 (ScriptableObjects)
│   └── ServerConfig.cs     # 서버 연결 설정
├── Managers/               # 데이터 관리자 (Singletons)
│   ├── DataManager.cs      # 게임 정적 데이터 로드/관리
│   └── SaveManager.cs      # 플레이어 데이터 저장/로드
├── Models/                 # 데이터 모델 클래스
│   ├── ChapterData.cs      # 챕터 정보
│   ├── MonsterData.cs      # 몬스터 정보
│   ├── SkillData.cs        # 스킬 정보
│   ├── WaveConfig.cs       # 웨이브 설정
│   ├── StoreItemData.cs    # 상점 아이템
│   ├── PlayerSaveData.cs   # 플레이어 저장 데이터
│   └── Server/             # 서버 통신 모델
│       └── ServerModels.cs # API 요청/응답 모델
└── Resources/              # Unity Resources 폴더
    ├── Config/             # 설정 리소스
    │   └── ServerConfig.asset
    └── GameData/           # JSON 게임 데이터
        ├── Chapters.json
        ├── Monsters.json
        └── Skills.json
```

---

## 🎯 핵심 컴포넌트

### 1. DataManager (게임 데이터 관리자)

**역할**: JSON 파일에서 정적 게임 데이터를 로드하고 관리

**데이터 소스**: `Assets/Data/Resources/GameData/*.json`

**관리 데이터**:
- 챕터 (ChapterData) - 8개 챕터 정의
- 몬스터 (MonsterData) - 적 유형 및 스탯
- 스킬 (SkillData) - 5개 스킬 + 음성 키워드

**사용 예시**:
```csharp
// 챕터 데이터 가져오기
ChapterData chapter = DataManager.Instance.GetChapterData("chapter_1");

// 모든 스킬 가져오기
List<SkillData> skills = DataManager.Instance.GetAllSkillData();

// 스킬 ID로 검색
SkillData fireball = DataManager.Instance.GetSkillData("skill_fireball");
```

---

### 2. SaveManager (플레이어 데이터 관리자)

**역할**: 플레이어 진행도와 설정을 JSON 파일로 저장/로드

**저장 위치**: `Application.persistentDataPath/PlayerSaveData.json`

**관리 데이터**:
- 플레이어 정보 (이름, 레벨, 경험치, 골드)
- 화폐 (다이아몬드, 부활석)
- 진행도 (현재 챕터, 스테이지, 잠금 해제 챕터)
- 무한 모드 (최고 점수, 현재 웨이브)
- 스킬 & 아이템 (잠금 해제 리스트)
- 설정 (전체화면, 마이크, 언어)
- 키 바인딩 (액션 → 키 매핑)
- 통계 (플레이 시간, 처치 수, 사망 수)

**자동 저장 시점**:
- 앱 종료 시 (`OnApplicationQuit`)
- 앱 백그라운드 전환 시 (`OnApplicationPause`) - 모바일
- UI에서 수동 호출

**사용 예시**:
```csharp
// 현재 저장 데이터 가져오기
PlayerSaveData saveData = SaveManager.Instance.GetCurrentSaveData();

// 골드 추가
SaveManager.Instance.AddGold(100);

// 다이아몬드 사용
bool success = SaveManager.Instance.SpendDiamonds(50);

// 수동 저장
SaveManager.Instance.SaveGame();
```

---

### 3. ServerConfig (서버 설정)

**역할**: 음성 인식 서버 연결 설정을 중앙화

**타입**: ScriptableObject (Unity Inspector에서 설정 가능)

**설정 항목**:
- 서버 URL (`http://localhost:8000`)
- 연결 타임아웃 (30초)
- 요청 타임아웃 (60초)
- 기본 언어 (`ko`)
- 기본 모델 크기 (`base`)
- 최소 신뢰도 점수 (0.7)
- API 엔드포인트 경로들

**사용 예시**:
```csharp
// Singleton 접근
string serverUrl = ServerConfig.Instance.serverUrl;

// 엔드포인트 URL 생성
string recognizeUrl = ServerConfig.Instance.GetUrl(ServerConfig.Instance.recognizeEndpoint);

// 파라미터가 있는 URL
string statusUrl = ServerConfig.Instance.GetUrl(ServerConfig.Instance.modelStatusEndpoint, "base");
```

---

## 🔄 데이터 흐름

### 게임 시작 시

```
1. DataManager 초기화
   ↓
2. Resources/GameData/*.json 로드
   ↓
3. SaveManager 초기화
   ↓
4. PlayerSaveData.json 로드 (없으면 기본값 생성)
   ↓
5. 게임 준비 완료
```

### 플레이 중

```
플레이어 액션
    ↓
UI Component (InGameUI, StoreUI 등)
    ↓
SaveManager.AddGold() / SpendDiamonds() 등
    ↓
PlayerSaveData 업데이트
    ↓
자동 저장 (특정 시점)
```

### 음성 인식

```
플레이어 음성 입력
    ↓
VoiceRecognitionManager (Unity)
    ↓
VoiceServerClient (HTTP 통신)
    ↓
Voice Recognition Server (FastAPI + Whisper)
    ↓
RecognitionResponse (인식된 스킬)
    ↓
InGameUI (스킬 발동)
```

---

## 📊 JSON 데이터 형식

### Chapters.json
```json
{
  "chapters": [
    {
      "chapterId": "chapter_1",
      "chapterNumber": 1,
      "chapterName": "Pride",
      "waves": [
        {
          "waveNumber": 1,
          "totalEnemies": 5,
          "enemyTypes": ["goblin"],
          "spawnInterval": 2.0
        }
      ],
      "rewards": {
        "gold": 100,
        "experience": 50
      }
    }
  ]
}
```

### Skills.json
```json
{
  "skills": [
    {
      "skillId": "skill_fireball",
      "skillName": "Fireball",
      "skillType": "Instant",
      "damage": 50,
      "cooldown": 3.0,
      "manaCost": 20,
      "voiceKeywords": ["파이어볼", "fireball"]
    }
  ]
}
```

---

## 🔧 설정 방법

### ServerConfig 생성

1. Unity Editor에서: `Assets → Create → LostSpells → Config → Server Config`
2. 파일명: `ServerConfig`
3. 저장 위치: `Assets/Data/Resources/Config/ServerConfig.asset`
4. Inspector에서 서버 URL 및 설정 조정

### 새로운 챕터 추가

1. `Assets/Data/Resources/GameData/Chapters.json` 편집
2. `chapters` 배열에 새 챕터 객체 추가
3. Unity 재생 시 자동 로드

### 새로운 스킬 추가

1. `Assets/Data/Resources/GameData/Skills.json` 편집
2. `skills` 배열에 새 스킬 객체 추가
3. `voiceKeywords`에 음성 인식 키워드 설정

---

## 🐛 문제 해결

### 데이터 로드 실패

**증상**: `DataManager.Instance.GetChapterData()` 반환값이 null

**원인**:
- JSON 파일이 `Resources/GameData/` 폴더에 없음
- JSON 구문 오류
- chapterId가 JSON에 정의되지 않음

**해결**:
1. JSON 파일 위치 확인
2. JSON 유효성 검증 (온라인 validator 사용)
3. Unity Console에서 에러 로그 확인

### 저장 데이터 손실

**증상**: 게임 재시작 시 진행도 초기화

**원인**:
- `Application.persistentDataPath` 경로 문제
- 파일 쓰기 권한 부족
- JSON 직렬화 오류

**해결**:
1. 저장 경로 확인:
   ```csharp
   Debug.Log($"Save path: {SaveManager.Instance.saveFilePath}");
   ```
2. 파일 존재 여부 확인
3. SaveManager 로그 확인

### 서버 연결 실패

**증상**: 음성 인식이 작동하지 않음

**원인**:
- 서버가 실행되지 않음
- 잘못된 서버 URL
- 방화벽 차단

**해결**:
1. 서버 실행 확인: `Server/start.bat`
2. 브라우저에서 `http://localhost:8000` 접속 테스트
3. ServerConfig의 URL 확인

---

## 📝 Best Practices

1. **JSON 직접 편집 주의**
   - Unity가 실행 중일 때 JSON 편집 시 자동 리로드 안 됨
   - Editor 재시작 또는 Play Mode 재진입 필요

2. **저장 데이터 백업**
   - 중요한 변경 전 `PlayerSaveData.json` 백업
   - Git에는 `.gitignore`로 제외됨

3. **서버 URL 하드코딩 금지**
   - 항상 `ServerConfig.Instance.serverUrl` 사용
   - 환경별 설정 변경 용이

4. **Null 체크 필수**
   - DataManager에서 데이터 가져올 때 항상 null 체크
   - 존재하지 않는 ID 요청 시 null 반환

---

## 🚀 향후 개선 사항

- [ ] Dictionary 직렬화 개선 (키 바인딩)
- [ ] 경험치 시스템 구현
- [ ] 챕터 클리어 추적 시스템
- [ ] IAP (In-App Purchase) 시스템 통합
- [ ] 데이터 유효성 검증 추가
- [ ] 서버 연결 상태 모니터링
- [ ] 오프라인 모드 지원
