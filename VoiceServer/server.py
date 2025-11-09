"""
Voice Recognition Server
음성 인식 서버 메인 엔트리 포인트
"""
import sys
import uvicorn
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

# 의존성 확인 및 설치
from utils import DependencyInstaller

print("Checking dependencies...")
installer = DependencyInstaller()
version_ok, version_msg = installer.check_python_version()

if not version_ok:
    print(f"ERROR: {version_msg}")
    sys.exit(1)

all_installed, missing = installer.check_dependencies()

if not all_installed:
    print(f"Missing dependencies: {', '.join(missing)}")
    print("Installing missing dependencies...")
    success, results = installer.install_all_dependencies()

    for result in results:
        print(f"  {result}")

    if not success:
        print("Failed to install dependencies. Please install manually:")
        print(f"  pip install {' '.join(missing)}")
        sys.exit(1)

print("All dependencies satisfied.\n")

# 모듈 임포트
from config.settings import (
    SERVER_HOST,
    SERVER_PORT,
    DEFAULT_MODEL,
    DEFAULT_LANGUAGE
)
from core import WhisperHandler, SkillMatcher
from services import ModelManager, LanguageManager
from api.routes import router, init_services

# FastAPI 앱 생성
app = FastAPI(
    title="Voice Recognition Server",
    description="음성 인식 및 스킬 매칭 서버",
    version="2.0.0"
)

# CORS 설정
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 서비스 초기화
print("Initializing services...")

# Whisper 핸들러 초기화
print(f"Loading Whisper model: {DEFAULT_MODEL}...")
whisper_handler = WhisperHandler(model_size=DEFAULT_MODEL)

# 스킬 매처 초기화 (빈 스킬 리스트로 시작)
skill_matcher = SkillMatcher(skills=[])

# 모델 매니저 초기화
model_manager = ModelManager()

# 언어 매니저 초기화
language_manager = LanguageManager()

# API 라우트에 서비스 주입
init_services(whisper_handler, skill_matcher, model_manager, language_manager)

# 라우터 등록
app.include_router(router)

print("Services initialized successfully!\n")


@app.on_event("startup")
async def startup_event():
    """서버 시작 시 실행"""
    print("=" * 60)
    print("Voice Recognition Server Started")
    print("=" * 60)
    print(f"Server running at: http://{SERVER_HOST}:{SERVER_PORT}")
    print(f"Current model: {whisper_handler.current_model_size}")
    print(f"Current language: {language_manager.get_language()}")
    print(f"Documentation: http://{SERVER_HOST}:{SERVER_PORT}/docs")
    print("=" * 60)


@app.on_event("shutdown")
async def shutdown_event():
    """서버 종료 시 실행"""
    print("\nShutting down Voice Recognition Server...")


if __name__ == "__main__":
    # 서버 실행
    uvicorn.run(
        app,
        host=SERVER_HOST,
        port=SERVER_PORT,
        log_level="info"
    )
