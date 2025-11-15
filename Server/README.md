# Voice Recognition Server

Unity용 음성 인식 서버 - Faster Whisper 기반

## 🚀 빠른 시작

### Windows에서 실행

```cmd
start.bat
```

더블클릭으로 실행하거나 터미널에서 실행하면 서버가 `http://localhost:8000`에서 시작됩니다.

---

## 📦 설치 (최초 1회)

### 1. Python 설치
- Python 3.9 이상 필요
- [Python 다운로드](https://www.python.org/downloads/)

### 2. 의존성 설치 (선택사항 - 가상환경 사용시)

**가상환경 생성:**
```cmd
python -m venv venv
```

**활성화:**
```cmd
venv\Scripts\activate
```

**의존성 설치:**
```cmd
pip install -r requirements.txt
```

---

## 🎯 기능

- ✅ **음성 인식**: Faster Whisper 기반 고속 음성 인식
- ✅ **스킬 매칭**: Levenshtein 거리 기반 유사도 매칭
- ✅ **다국어 지원**: 한국어, 영어, 일본어, 중국어
- ✅ **모델 관리**: 5가지 모델 크기 (tiny ~ large-v3)
- ✅ **REST API**: FastAPI 기반 REST API

---

## 🌐 API 엔드포인트

서버가 실행되면 `http://localhost:8000`에서 접근 가능

### 주요 엔드포인트

- `GET /` - 서버 상태 확인
- `POST /recognize` - 음성 인식 (파일 업로드)
- `POST /set-skills` - 스킬 목록 설정
- `GET /skills` - 현재 스킬 목록 조회
- `GET /models` - 모델 목록 및 상태
- `POST /models/select` - 모델 변경
- `POST /models/download` - 모델 다운로드
- `DELETE /models/{model_size}` - 모델 삭제

### API 문서
- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`

---

## 🤖 사용 가능한 모델

| 모델 | 크기 | 속도 | 정확도 |
|------|------|------|--------|
| tiny | ~75MB | ⚡⚡⚡⚡⚡ | ⭐⭐ |
| base | ~145MB | ⚡⚡⚡⚡ | ⭐⭐⭐ |
| small | ~466MB | ⚡⚡⚡ | ⭐⭐⭐⭐ |
| medium | ~1.5GB | ⚡⚡ | ⭐⭐⭐⭐⭐ |
| large-v3 | ~2.9GB | ⚡ | ⭐⭐⭐⭐⭐⭐ |

---

## 🔧 구조

```
Server/
├── main.py              # FastAPI 서버 메인 코드
├── whisper_handler.py   # Whisper 음성 인식 핸들러
├── skill_matcher.py     # 스킬 매칭 로직
├── start.bat            # Windows 실행 스크립트
└── requirements.txt     # Python 의존성
```

---

## 🛠️ 트러블슈팅

### 포트 8000이 이미 사용 중
```cmd
# 포트 사용 중인 프로세스 찾기
netstat -ano | findstr :8000
```

### 모델 다운로드 실패
- 인터넷 연결 확인
- 디스크 공간 확인 (large-v3는 ~3GB 필요)
- Hugging Face 허브 접근 가능 여부 확인

---

## 📄 라이센스

MIT License
