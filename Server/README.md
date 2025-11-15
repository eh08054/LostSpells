# Lost Spells - Server

Lost Spells 게임의 서버 시스템입니다. Unity 게임 클라이언트와 독립적으로 실행됩니다.

## 📂 구조

```
Server/
├── voice-recognition/     # 음성 인식 서버
│   ├── app.py            # Flask 서버
│   ├── config.json       # 설정
│   └── README.md         # 상세 문서
│
└── docker/               # Docker 배포 설정
    ├── Dockerfile
    └── docker-compose.yml
```

## 🚀 서버 실행

### 방법 1: Python 직접 실행

```bash
cd voice-recognition
pip install -r requirements.txt
python app.py
```

### 방법 2: Docker 사용 (권장)

```bash
cd docker
docker-compose up -d
```

## 🔗 Unity 연동

Unity의 `VoiceServerClient.cs`에서 서버 URL 설정:

```csharp
// 개발 환경
private const string SERVER_URL = "http://localhost:5000";

// 프로덕션 환경
private const string SERVER_URL = "https://your-domain.com";
```

## 📚 각 서버 문서

- [음성 인식 서버 상세 문서](voice-recognition/README.md)

## 🚢 배포 가이드

### AWS EC2 배포

1. **인스턴스 생성**
   - Ubuntu Server 22.04 LTS
   - t2.micro 이상
   - 보안 그룹: 포트 5000 오픈

2. **서버 설정**
```bash
# Docker 설치
sudo apt update
sudo apt install docker.io docker-compose

# 프로젝트 클론
git clone https://github.com/your-repo/LostSpells.git
cd LostSpells/Server/docker

# 서버 실행
sudo docker-compose up -d
```

3. **도메인 연결** (옵션)
   - Route 53에서 도메인 설정
   - Nginx로 리버스 프록시
   - Let's Encrypt로 SSL 인증서

### GCP Cloud Run 배포

```bash
# 프로젝트 설정
gcloud config set project YOUR_PROJECT_ID

# 이미지 빌드 및 푸시
gcloud builds submit --tag gcr.io/YOUR_PROJECT_ID/voice-server

# Cloud Run 배포
gcloud run deploy voice-server \
  --image gcr.io/YOUR_PROJECT_ID/voice-server \
  --platform managed \
  --region asia-northeast3 \
  --allow-unauthenticated
```

## 🛡️ 보안 체크리스트

- [ ] HTTPS 적용 (SSL/TLS)
- [ ] API 키 인증 추가
- [ ] Rate limiting 구현
- [ ] 파일 업로드 크기 제한
- [ ] 입력 검증 및 새니타이징
- [ ] CORS 설정 확인
- [ ] 에러 메시지에 민감 정보 노출 방지

## 📊 모니터링

### 로그 확인

```bash
# Docker 로그
docker-compose logs -f voice-server

# Python 로그
tail -f voice-recognition/app.log
```

### 상태 확인

```bash
curl http://localhost:5000/api/health
```

## 🔧 트러블슈팅

### 포트 충돌
```bash
# 5000 포트 사용 중인 프로세스 확인
lsof -i :5000  # Mac/Linux
netstat -ano | findstr :5000  # Windows

# config.json에서 포트 변경
{
  "server": {
    "port": 5001  # 다른 포트로 변경
  }
}
```

### Docker 빌드 실패
```bash
# 캐시 없이 재빌드
docker-compose build --no-cache
```

## 📝 개발 노트

- Unity 빌드와 서버는 완전히 독립적
- 게임 배포 시 서버 코드는 포함되지 않음
- 서버 업데이트는 Unity 재빌드 없이 가능
