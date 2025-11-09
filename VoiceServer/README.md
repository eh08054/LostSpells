# LostSpells 음성인식 서버

음성 인식 기능을 위한 파이썬 FastAPI 서버입니다.

## 빠른 시작

### 방법 1: 배치 파일 실행 (추천)
`start_server.bat` 파일을 더블클릭하면 자동으로 설치 및 서버가 시작됩니다.

### 방법 2: 수동 실행

#### 1. 가상환경 생성 (최초 1회만)
```bash
python -m venv venv
```

#### 2. 가상환경 활성화
```bash
venv\Scripts\activate
```

#### 3. 패키지 설치 (최초 1회만)
```bash
pip install -r requirements.txt
```

#### 4. 서버 실행
```bash
python main.py
```

## 서버 정보
- 주소: `http://localhost:8000`
- 포트: `8000`

## API 엔드포인트

### 1. 서버 상태 확인
```
GET http://localhost:8000/
```

### 2. 스킬 목록 설정
```
POST http://localhost:8000/set-skills
Form Data: skills="파이어볼,힐,아이스,라이트닝,실드"
```

### 3. 음성 인식
```
POST http://localhost:8000/recognize
Form Data:
  - audio: WAV 파일 (바이너리)
  - skills: (선택사항) 쉼표로 구분된 스킬 목록
```

## 필요 패키지
- fastapi
- uvicorn
- faster-whisper
- python-Levenshtein

## 문제 해결

### "No module named 'faster_whisper'" 오류
```bash
pip install faster-whisper
```

### 서버가 시작되지 않음
1. Python 3.8 이상이 설치되어 있는지 확인
2. 포트 8000이 이미 사용 중인지 확인
3. 가상환경이 활성화되어 있는지 확인

### Whisper 모델 다운로드가 느림
최초 실행 시 Whisper 모델(약 140MB)이 자동으로 다운로드됩니다.
네트워크 상태에 따라 시간이 걸릴 수 있습니다.

## Unity 연결

Unity에서 VoiceRecognitionManager 컴포넌트의 Server URL이 `http://localhost:8000`으로 설정되어 있어야 합니다.

1. InGame 씬 실행
2. 서버가 실행 중인지 확인
3. 스페이스바를 눌러 음성 인식 시작
4. 스페이스바를 떼면 인식 결과 표시
