# Lost Spells - 프로젝트 구조 재편성 가이드

이 문서는 Lost Spells 프로젝트의 파일 구조를 재편성하는 전체 과정을 안내합니다.

## 📋 목차

1. [사전 준비](#사전-준비)
2. [마이그레이션 실행](#마이그레이션-실행)
3. [검증 및 테스트](#검증-및-테스트)
4. [서버 설정](#서버-설정)
5. [트러블슈팅](#트러블슈팅)

---

## 🎯 재편성 목표

### Before (현재)
```
Assets/
├── Scenes/           # UI 파일만
├── Scripts/          # 모든 스크립트
├── Data/             # 데이터 혼재
└── (에셋들 흩어짐)
```

### After (목표)
```
Assets/
├── Scenes/           # 씬별 UI + Scripts 통합
├── Data/             # 순수 데이터 레이어
├── Core/             # 전역 시스템
└── Templates/        # 에셋 스토어 패키지

Server/               # Unity 밖, 독립 실행
└── voice-recognition/
```

---

## 사전 준비

### 1. 백업 생성

**Git 사용자:**
```bash
# 현재 상태 커밋
git add .
git commit -m "Backup before project restructure"

# 백업 태그 생성
git tag -a v0.1-before-restructure -m "Pre-restructure snapshot"
```

**Git 미사용자:**
```bash
# 프로젝트 전체 복사
# Windows: 탐색기에서 LostSpells 폴더 전체 복사
# Mac/Linux: cp -r LostSpells LostSpells_backup
```

### 2. Unity 에디터 준비

1. Unity에서 프로젝트 열기
2. 모든 씬 저장 (`Ctrl+S` / `Cmd+S`)
3. 진행 중인 작업 모두 저장
4. Prefab 편집 모드 종료

### 3. 체크리스트

- [ ] 프로젝트 백업 완료
- [ ] Unity 에디터 열림
- [ ] 모든 변경사항 저장
- [ ] 컴파일 에러 없음 확인

---

## 마이그레이션 실행

### Step 1: 마이그레이션 스크립트 확인

Unity 에디터에서:
1. 메뉴: `Tools > Restructure Project`
2. 마이그레이션 창 열림
3. "마이그레이션 계획 보기" 펼쳐서 확인

### Step 2: 폴더 구조 생성

1. **"1. 폴더 구조 생성" 버튼 클릭**
2. 생성되는 폴더들:
   ```
   Assets/
   ├── Scenes/*/UI/
   ├── Scenes/*/Scripts/
   ├── Scenes/InGame/Editor/
   ├── Data/Models/
   ├── Data/Resources/
   ├── Core/
   ├── Core/Voice/
   └── Templates/
   ```
3. Console 로그 확인
4. 완료 메시지 대기

### Step 3: 파일 이동 실행

1. **"2. 파일 이동 실행" 버튼 클릭**
2. 확인 다이얼로그에서 "실행" 클릭
3. Console에서 진행 상황 모니터링:
   ```
   ✓ 이동 완료: MainMenu.uxml → ...
   ✓ 이동 완료: PlayerComponent.cs → ...
   ○ 파일 없음: ... (무시 가능)
   ```
4. **중요: 에러 메시지 확인**
   - `✗ 이동 실패` 로그가 있으면 기록

### Step 4: Unity 에디터 재시작

**매우 중요!**
1. Unity 에디터 완전 종료
2. 잠시 대기 (5초)
3. Unity 에디터 다시 열기
4. 프로젝트 리컴파일 대기

### Step 5: 컴파일 에러 확인

재시작 후:
1. Console 창 확인 (`Ctrl+Shift+C`)
2. 에러가 있다면 → [트러블슈팅](#트러블슈팅) 참고
3. 에러 없다면 다음 단계 진행

### Step 6: 빈 폴더 정리

1. `Tools > Restructure Project` 다시 열기
2. **"3. 빈 폴더 정리" 버튼 클릭**
3. 삭제될 폴더들:
   ```
   Assets/Scripts/Components/     (빈 폴더)
   Assets/Scripts/Editor/         (빈 폴더)
   Assets/Scripts/Systems/        (빈 폴더)
   Assets/Data/GameConfig/        (빈 폴더)
   ```

---

## 검증 및 테스트

### 1. 파일 위치 확인

**Unity Project 창에서 확인:**

- [ ] `Scenes/MainMenu/UI/` - MainMenu.uxml, MainMenu.uss 존재
- [ ] `Scenes/MainMenu/Scripts/` - MainMenuUI.cs 존재
- [ ] `Scenes/InGame/Scripts/` - PlayerComponent.cs, EnemyComponent.cs 존재
- [ ] `Scenes/InGame/Scripts/Skills/` - SkillBehavior.cs 등 존재
- [ ] `Data/Models/` - ChapterData.cs 등 데이터 클래스 존재
- [ ] `Core/` - GameStateManager.cs 등 존재
- [ ] `Core/Voice/` - VoiceRecognitionManager.cs 등 존재
- [ ] `Templates/` - 2D Casual UI 등 에셋 존재

### 2. 참조 링크 확인

1. **씬 파일 열기**
   - MainMenu 씬 열기
   - Hierarchy에서 Canvas 선택
   - Inspector에서 UI Document 확인
   - Source Asset이 올바른 경로 가리키는지 확인

2. **프리팹 확인**
   - Prefabs 폴더의 프리팹 하나 열기
   - 컴포넌트 스크립트 누락 없는지 확인

3. **씬 전환 테스트**
   - Play 모드 실행
   - 씬 전환 동작 확인
   - UI 표시 확인

### 3. 빌드 테스트 (선택사항)

```
File > Build Settings
- Platform 선택
- "Build" 클릭
- 빌드 성공 확인
```

---

## 서버 설정

### 1. 서버 폴더 확인

프로젝트 루트에서:
```
LostSpells/
├── Assets/        (Unity)
└── Server/        (새로 생성됨)
    └── voice-recognition/
        ├── app.py
        ├── config.json
        └── requirements.txt
```

### 2. 로컬 서버 실행

```bash
# 1. 서버 폴더로 이동
cd Server/voice-recognition

# 2. Python 가상환경 생성 (권장)
python -m venv venv

# 3. 가상환경 활성화
# Windows:
venv\Scripts\activate
# Mac/Linux:
source venv/bin/activate

# 4. 의존성 설치
pip install -r requirements.txt

# 5. 서버 실행
python app.py
```

서버가 `http://localhost:5000`에서 실행됩니다.

### 3. Unity-Server 연동 테스트

1. Unity 에디터에서 Play 모드
2. 음성 인식 기능 테스트
3. Console에서 서버 통신 로그 확인

### 4. Docker 실행 (선택사항)

```bash
cd Server/docker
docker-compose up -d

# 상태 확인
curl http://localhost:5000/api/health
```

---

## 트러블슈팅

### 문제 1: 파일 이동 실패

**증상:**
```
✗ 이동 실패: Assets/Scripts/... → Cannot move asset
```

**해결:**
1. 해당 파일이 다른 곳에서 참조되는지 확인
2. Unity 에디터 재시작
3. 수동으로 파일 이동:
   - Project 창에서 파일 드래그 앤 드롭
   - 메타 파일(.meta)도 함께 이동됨

### 문제 2: Missing Script 에러

**증상:**
```
The referenced script on this Behaviour is missing!
```

**해결:**
1. `Edit > Preferences > External Tools`
2. "Regenerate project files" 클릭
3. Unity 재시작
4. 여전히 문제 시:
   - 해당 오브젝트 선택
   - Inspector에서 Missing Script 제거
   - 올바른 스크립트 다시 추가

### 문제 3: UI Document 경로 에러

**증상:**
```
Source Asset: None (UXML Document)
```

**해결:**
1. 씬 열기
2. UI Document 컴포넌트 선택
3. Source Asset 필드 클릭
4. 새 위치의 UXML 파일 선택
   - 예: `Scenes/MainMenu/UI/MainMenu.uxml`

### 문제 4: 네임스페이스 에러

**증상:**
```
error CS0246: The type or namespace 'XXX' could not be found
```

**해결:**
1. 스크립트 파일 열기
2. 상단에 `using` 문 확인
3. 필요한 네임스페이스 추가
4. 예:
   ```csharp
   using UnityEngine.UIElements;  // UI Toolkit용
   ```

### 문제 5: DataManager Resources 로드 실패

**증상:**
```
NullReferenceException: DataManager could not load Monsters.json
```

**해결:**
1. `DataManager.cs` 열기
2. Resources 경로 수정:
   ```csharp
   // Before
   Resources.Load<TextAsset>("GameData/Monsters");

   // After (Data 폴더가 Resources 내부면)
   Resources.Load<TextAsset>("Monsters");
   ```

### 문제 6: 서버 실행 안됨

**증상:**
```bash
ModuleNotFoundError: No module named 'flask'
```

**해결:**
```bash
# 의존성 다시 설치
pip install -r requirements.txt

# 특정 패키지 설치
pip install flask flask-cors
```

---

## 수동 롤백 방법

문제 발생 시 백업으로 복원:

### Git 사용자:
```bash
git reset --hard v0.1-before-restructure
```

### 파일 복사 백업 사용자:
1. Unity 에디터 종료
2. 현재 `LostSpells/` 폴더 삭제
3. `LostSpells_backup/` → `LostSpells/`로 복사
4. Unity 재시작

---

## 추가 작업 (선택사항)

### 1. 네임스페이스 추가

코드 정리를 위해 네임스페이스 추가:

```csharp
// Before
public class GameStateManager : MonoBehaviour
{
    ...
}

// After
namespace LostSpells.Core
{
    public class GameStateManager : MonoBehaviour
    {
        ...
    }
}
```

각 폴더별 권장 네임스페이스:
- `Scenes/InGame/Scripts/` → `LostSpells.Scenes.InGame`
- `Data/Models/` → `LostSpells.Data`
- `Core/` → `LostSpells.Core`

### 2. .gitignore 업데이트

`.gitignore`에 추가:
```
# Server
Server/voice-recognition/__pycache__/
Server/voice-recognition/*.pyc
Server/voice-recognition/venv/
Server/voice-recognition/uploads/
Server/voice-recognition/models/*.pth
```

---

## 완료 체크리스트

- [ ] 모든 파일 이동 완료
- [ ] 컴파일 에러 없음
- [ ] 씬 로드 정상 작동
- [ ] UI 표시 정상
- [ ] Play 모드 실행 가능
- [ ] 서버 실행 가능
- [ ] Unity-Server 통신 테스트
- [ ] 빈 폴더 정리 완료
- [ ] Git 커밋 (또는 백업 보관)

---

## 지원

문제가 계속되면:
1. Unity Console의 전체 에러 로그 복사
2. 실행한 단계 기록
3. 개발팀에 문의

---

**축하합니다!** 🎉 프로젝트 구조 재편성이 완료되었습니다.

이제 더 체계적이고 관리하기 쉬운 프로젝트 구조를 갖추었습니다.
