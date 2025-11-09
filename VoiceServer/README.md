# Voice Recognition Server

Unity용 음성 인식 서버 - Faster Whisper 기반

## 🚀 빠른 시작

### 🎯 권장 방법 (모든 OS)

```bash
python start.py
```

### OS별 실행 방법

#### Windows
```cmd
start_gui.bat
```
또는
```cmd
python start.py
```

#### macOS / Linux
```bash
./start_gui.sh
```
또는
```bash
python3 start.py
```

---

## 📦 설치

### 1. Python 설치
- Python 3.9 이상 필요
- [Python 다운로드](https://www.python.org/downloads/)

### 2. 가상환경 생성

**Windows:**
```cmd
python -m venv venv
venv\Scripts\activate
```

**macOS/Linux:**
```bash
python3 -m venv venv
source venv/bin/activate
```

### 3. 의존성 설치
```bash
pip install -r requirements.txt
```

---

## 🎯 기능

- ✅ **음성 인식**: Faster Whisper 기반 고속 음성 인식
- ✅ **스킬 매칭**: Levenshtein 거리 기반 유사도 매칭
- ✅ **다국어 지원**: 한국어, 영어, 일본어, 중국어
- ✅ **모델 관리**: 5가지 모델 크기 (tiny ~ large-v3)
- ✅ **GUI 관리자**: 모델 다운로드/삭제, 서버 제어
- ✅ **REST API**: FastAPI 기반 REST API

---

## 🌐 API 엔드포인트

서버가 실행되면 `http://localhost:8000`에서 접근 가능

### 주요 엔드포인트

- `GET /` - 서버 상태 확인
- `POST /recognize` - 음성 인식 (파일 업로드)
- `POST /set-skills` - 스킬 목록 설정
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

## 🔧 설정

서버 설정은 `main.py`에서 수정 가능:

```python
# 서버 주소 및 포트
host = "0.0.0.0"
port = 8000

# 기본 모델
default_model = "base"

# 기본 언어
default_language = "ko"
```

---

## 📝 Unity 연동 예시

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class VoiceRecognitionClient : MonoBehaviour
{
    private const string SERVER_URL = "http://localhost:8000";

    IEnumerator RecognizeAudio(byte[] audioData)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, "recording.wav", "audio/wav");
        form.AddField("language", "ko");

        using (UnityWebRequest www = UnityWebRequest.Post($"{SERVER_URL}/recognize", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"Recognition result: {jsonResponse}");
            }
            else
            {
                Debug.LogError($"Error: {www.error}");
            }
        }
    }
}
```

---

## 🛠️ 트러블슈팅

### 가상환경을 찾을 수 없음
```bash
# 가상환경 재생성
python -m venv venv
source venv/bin/activate  # macOS/Linux
# 또는
venv\Scripts\activate  # Windows
pip install -r requirements.txt
```

### 포트 8000이 이미 사용 중
```bash
# Windows: 포트 사용 중인 프로세스 찾기
netstat -ano | findstr :8000

# macOS/Linux: 포트 사용 중인 프로세스 찾기
lsof -i :8000
```

### 모델 다운로드 실패
- 인터넷 연결 확인
- 디스크 공간 확인 (large-v3는 ~3GB 필요)
- Hugging Face 허브 접근 가능 여부 확인

---

## 📄 라이센스

MIT License

---

## 🤝 기여

버그 리포트 및 기능 제안은 Issues에 등록해주세요.
