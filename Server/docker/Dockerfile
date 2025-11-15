# Lost Spells - Voice Recognition Server Dockerfile

FROM python:3.10-slim

# 작업 디렉토리 설정
WORKDIR /app

# 시스템 패키지 설치 (오디오 처리용)
RUN apt-get update && apt-get install -y \
    ffmpeg \
    portaudio19-dev \
    && rm -rf /var/lib/apt/lists/*

# Python 의존성 복사 및 설치
COPY ../voice-recognition/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# 애플리케이션 코드 복사
COPY ../voice-recognition/ .

# 업로드 폴더 생성
RUN mkdir -p uploads models

# 포트 노출
EXPOSE 5000

# 환경변수 설정
ENV FLASK_APP=app.py
ENV FLASK_ENV=production

# 서버 실행
CMD ["python", "app.py"]
