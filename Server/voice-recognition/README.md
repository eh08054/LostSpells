# Lost Spells - Voice Recognition Server

Lost Spells 게임의 음성 인식 서버입니다. Unity 클라이언트에서 녹음한 음성을 받아 텍스트로 변환하고 스킬을 매칭합니다.

## 🚀 빠른 시작

### 로컬 개발

1. **Python 환경 설정**
```bash
# 가상환경 생성 (권장)
python -m venv venv

# 가상환경 활성화
# Windows
venv\Scripts\activate
# Mac/Linux
source venv/bin/activate

# 의존성 설치
pip install -r requirements.txt
```

2. **서버 실행**
```bash
python app.py
```

서버가 `http://localhost:5000`에서 실행됩니다.

### Docker 사용

```bash
cd ../docker
docker-compose up -d
```

## 📡 API 엔드포인트

### 1. 서버 상태 확인
```http
GET /api/health
```

**응답:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-15T12:00:00",
  "version": "1.0.0"
}
```

### 2. 음성 인식
```http
POST /api/recognize
Content-Type: multipart/form-data

Parameters:
  - audio: 오디오 파일 (WAV, MP3)
  - language: 언어 코드 (ko-KR, en-US)
```

**응답:**
```json
{
  "text": "파이어볼",
  "confidence": 0.95,
  "skill_matched": "fireball",
  "language": "ko-KR",
  "timestamp": "2025-11-15T12:00:00"
}
```

### 3. 스킬 목록 조회
```http
GET /api/skills
```

**응답:**
```json
{
  "skills": {
    "fireball": ["파이어볼", "fire ball"],
    "ice_spear": ["아이스 스피어", "ice spear"]
  },
  "count": 5
}
```

### 4. 스킬 키워드 업데이트
```http
POST /api/update_skills
Content-Type: application/json

Body: { "skills": {...} }
```

## 🔧 설정

`config.json` 파일에서 서버 설정을 변경할 수 있습니다:

```json
{
  "server": {
    "host": "0.0.0.0",
    "port": 5000
  },
  "recognition": {
    "language": "ko-KR",
    "confidence_threshold": 0.7
  }
}
```

## 📂 프로젝트 구조

```
voice-recognition/
├── app.py              # Flask 서버 메인
├── config.json         # 서버 설정
├── requirements.txt    # Python 의존성
├── models/             # 음성 인식 모델 (옵션)
├── utils/              # 유틸리티 함수
└── uploads/            # 업로드된 음성 파일 저장
```

## 🧪 테스트

### cURL로 테스트
```bash
# 상태 확인
curl http://localhost:5000/api/health

# 음성 인식 (테스트 파일 필요)
curl -X POST http://localhost:5000/api/recognize \
  -F "audio=@test_audio.wav" \
  -F "language=ko-KR"
```

### Python으로 테스트
```python
import requests

# 음성 파일 업로드
with open('test_audio.wav', 'rb') as f:
    files = {'audio': f}
    data = {'language': 'ko-KR'}
    response = requests.post('http://localhost:5000/api/recognize',
                           files=files, data=data)
    print(response.json())
```

## 🔐 보안 고려사항

### 프로덕션 배포 시

1. **HTTPS 사용**
   - Nginx 리버스 프록시 사용
   - Let's Encrypt SSL 인증서 적용

2. **API 인증**
   - API 키 기반 인증 추가
   - Rate limiting 구현

3. **파일 업로드 검증**
   - 파일 크기 제한
   - 파일 형식 검증
   - 바이러스 스캔

## 🚢 배포

### AWS EC2 배포 예시

```bash
# 1. EC2 인스턴스 접속
ssh -i key.pem ubuntu@your-server.com

# 2. Docker 설치 (Ubuntu)
sudo apt update
sudo apt install docker.io docker-compose

# 3. 프로젝트 클론
git clone https://github.com/your-repo/LostSpells.git
cd LostSpells/Server/docker

# 4. Docker 실행
sudo docker-compose up -d

# 5. 상태 확인
curl http://localhost:5000/api/health
```

## 🛠️ 개발 가이드

### 새로운 음성 인식 엔진 추가

`app.py`의 `recognize_audio()` 함수를 수정:

```python
def recognize_audio(filepath, language):
    # OpenAI Whisper 사용 예시
    import whisper
    model = whisper.load_model("base")
    result = model.transcribe(filepath, language=language)
    return result["text"]
```

### 스킬 키워드 관리

Unity의 `Skills.json`과 동기화 필요:
- Unity에서 스킬 추가/수정 시
- `/api/update_skills` 엔드포인트로 서버에 동기화

## 📝 TODO

- [ ] OpenAI Whisper 모델 통합
- [ ] 데이터베이스 연동 (스킬 키워드 저장)
- [ ] 음성 인식 정확도 로깅
- [ ] 사용자별 음성 프로필 학습
- [ ] WebSocket 지원 (실시간 스트리밍)

## 📄 라이센스

Lost Spells Project
