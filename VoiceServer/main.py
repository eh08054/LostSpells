from fastapi import FastAPI, File, UploadFile, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from typing import List, Optional
import os
import tempfile
import json
from pathlib import Path

try:
    from whisper_handler import WhisperHandler
    WHISPER_AVAILABLE = True
except ImportError as e:
    print(f"Warning: Whisper not available: {e}")
    print("Server will run in TEST mode without speech recognition")
    WHISPER_AVAILABLE = False

from skill_matcher import SkillMatcher

# FastAPI 앱 생성
app = FastAPI(title="Voice Recognition Skill Matcher")

# CORS 설정 (Unity에서 접근 가능하도록)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # 실제 배포시에는 특정 도메인만 허용
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Whisper 핸들러 (서버 시작 시 한 번만 로드)
whisper_handler = None
skill_matcher = None
current_skills = []

# 다운로드 진행 상태 추적
download_progress = {}  # {model_size: {"status": "downloading", "progress": 0-100}}

@app.on_event("startup")
async def startup_event():
    """서버 시작 시 Whisper 모델 로드"""
    global whisper_handler
    if WHISPER_AVAILABLE:
        print("Initializing Whisper model...")
        whisper_handler = WhisperHandler(model_size="base")  # tiny, base, small, medium, large
        print("Server ready with Whisper!")
    else:
        print("Server ready in TEST mode (Whisper disabled)")

@app.get("/")
async def root():
    """서버 상태 확인"""
    return {
        "status": "running",
        "message": "Voice Recognition Server",
        "current_skills": current_skills
    }

@app.post("/set-skills")
async def set_skills(skills: str = Form(...)):
    """
    스킬 목록 설정

    Args:
        skills: 쉼표로 구분된 스킬 목록 (예: "가,나,다,라,마")
    """
    global skill_matcher, current_skills

    try:
        # 쉼표로 구분된 스킬 파싱
        skill_list = [s.strip() for s in skills.split(",") if s.strip()]

        if not skill_list:
            raise HTTPException(status_code=400, detail="No skills provided")

        current_skills = skill_list
        skill_matcher = SkillMatcher(skill_list)

        return {
            "status": "success",
            "message": f"Skills set successfully",
            "skills": current_skills
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error setting skills: {str(e)}")

@app.post("/recognize")
async def recognize_skill(
    audio: UploadFile = File(...),
    skills: Optional[str] = Form(None),
    language: Optional[str] = Form("ko")
):
    """
    음성 파일에서 스킬 인식

    Args:
        audio: 음성 파일 (WAV, MP3 등)
        skills: (선택) 쉼표로 구분된 스킬 목록
        language: (선택) 언어 코드 (ko, en 등)

    Returns:
        {
            "recognized_text": "인식된 텍스트",
            "processing_time": 0.5,
            "skill_scores": {"가": 0.95, "나": 0.10, ...}
        }
    """
    global whisper_handler, skill_matcher, current_skills

    if not WHISPER_AVAILABLE:
        # TEST MODE: Whisper 없이 더미 응답 반환
        if not current_skills:
            current_skills = ["가", "나", "다", "라", "마"]
            skill_matcher = SkillMatcher(current_skills)

        return {
            "status": "success",
            "recognized_text": "테스트",
            "processing_time": 0.1,
            "skill_scores": {"가": 0.85, "나": 0.12, "다": 0.08, "라": 0.15, "마": 0.05},
            "best_match": {"skill": "가", "score": 0.85},
            "note": "TEST MODE - Whisper not available. Install Python 3.13 and faster-whisper for real recognition."
        }

    if whisper_handler is None:
        raise HTTPException(status_code=500, detail="Whisper model not loaded")

    # 스킬이 제공되면 업데이트
    if skills:
        skill_list = [s.strip() for s in skills.split(",") if s.strip()]
        current_skills = skill_list
        skill_matcher = SkillMatcher(skill_list)

    if not current_skills:
        raise HTTPException(
            status_code=400,
            detail="No skills configured. Use /set-skills first or provide skills parameter"
        )

    # 임시 파일로 저장
    temp_file = None
    debug_file = None
    try:
        content = await audio.read()

        # 디버깅용: 받은 오디오를 Server 폴더에 저장
        debug_file = Path(__file__).parent / "last_recording.wav"
        with open(debug_file, "wb") as f:
            f.write(content)

        # 임시 파일 생성
        suffix = Path(audio.filename).suffix if audio.filename else ".wav"
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as temp:
            temp.write(content)
            temp_file = temp.name

        print(f"[Audio] Received {len(content)} bytes")
        print(f"[Audio] Debug file saved to: {debug_file}")

        # Whisper로 음성 인식 (언어 파라미터 사용)
        transcription = whisper_handler.transcribe_audio(temp_file, language=language)

        recognized_text = transcription["text"]
        processing_time = transcription["processing_time"]

        print(f"[Whisper] Recognized ({language}): '{recognized_text}' (took {processing_time:.2f}s)")

        # 스킬 매칭
        skill_scores = skill_matcher.match_skills(recognized_text)
        best_match = skill_matcher.get_best_match(recognized_text)

        print(f"[Matching] Best match: '{best_match[0]}' (score: {best_match[1]:.2f})")
        print(f"[Matching] All scores: {skill_scores}")

        return {
            "status": "success",
            "recognized_text": recognized_text,
            "processing_time": round(processing_time, 2),
            "skill_scores": skill_scores,
            "best_match": {
                "skill": best_match[0],
                "score": best_match[1]
            }
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error processing audio: {str(e)}")

    finally:
        # 임시 파일 삭제
        if temp_file and os.path.exists(temp_file):
            os.remove(temp_file)

@app.get("/skills")
async def get_skills():
    """현재 설정된 스킬 목록 조회"""
    return {
        "skills": current_skills
    }

@app.get("/models")
async def get_models():
    """
    사용 가능한 모델 목록 및 상태 조회

    Returns:
        {
            "current_model": "base",
            "available_models": {
                "tiny": {"name": "Tiny", "description": "...", "size": "~75MB", "downloaded": true},
                ...
            }
        }
    """
    if not WHISPER_AVAILABLE:
        return {
            "status": "error",
            "message": "Whisper not available"
        }

    available_models = WhisperHandler.get_available_models()

    # 각 모델의 다운로드 상태 확인
    models_with_status = {}
    for model_id, model_info in available_models.items():
        models_with_status[model_id] = {
            **model_info,
            "downloaded": WhisperHandler.check_model_downloaded(model_id)
        }

    current_model = whisper_handler.current_model_size if whisper_handler else "none"

    return {
        "status": "success",
        "current_model": current_model,
        "available_models": models_with_status
    }

@app.post("/models/select")
async def select_model(model_size: str = Form(...)):
    """
    모델 선택 및 로드

    Args:
        model_size: 모델 크기 (tiny, base, small, medium, large-v3)

    Returns:
        성공 또는 실패 메시지
    """
    global whisper_handler

    if not WHISPER_AVAILABLE:
        raise HTTPException(status_code=500, detail="Whisper not available")

    available_models = WhisperHandler.get_available_models()
    if model_size not in available_models:
        raise HTTPException(
            status_code=400,
            detail=f"Invalid model size. Available: {list(available_models.keys())}"
        )

    try:
        if whisper_handler is None:
            whisper_handler = WhisperHandler(model_size=model_size)
        else:
            whisper_handler.change_model(model_size)

        return {
            "status": "success",
            "message": f"Model changed to {model_size}",
            "current_model": model_size
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error loading model: {str(e)}")

@app.get("/models/{model_size}/status")
async def get_model_status(model_size: str):
    """
    특정 모델의 상태 확인 (다운로드 여부, 다운로드 진행률)

    Args:
        model_size: 모델 크기 (tiny, base, small, medium, large-v3)

    Returns:
        {
            "downloaded": true/false,
            "download_progress": 0-100,
            "status": "downloaded"/"not_downloaded"/"downloading"
        }
    """
    global download_progress

    if not WHISPER_AVAILABLE:
        raise HTTPException(status_code=500, detail="Whisper not available")

    available_models = WhisperHandler.get_available_models()
    if model_size not in available_models:
        raise HTTPException(
            status_code=400,
            detail=f"Invalid model size. Available: {list(available_models.keys())}"
        )

    # 다운로드 중인지 확인
    if model_size in download_progress:
        return {
            "downloaded": False,
            "download_progress": download_progress[model_size].get("progress", 0),
            "status": "downloading"
        }

    # 모델 다운로드 상태 확인
    is_downloaded = WhisperHandler.check_model_downloaded(model_size)

    if is_downloaded:
        return {
            "downloaded": True,
            "download_progress": 100,
            "status": "downloaded"
        }
    else:
        # 다운로드 시작 전이거나 실패한 경우
        return {
            "downloaded": False,
            "download_progress": 0,
            "status": "not_downloaded"
        }

def get_model_download_size(model_size: str) -> int:
    """모델의 예상 다운로드 크기(바이트) 반환"""
    # 각 모델의 대략적인 크기 (바이트)
    model_sizes = {
        "tiny": 75 * 1024 * 1024,      # 75MB
        "base": 145 * 1024 * 1024,     # 145MB
        "small": 466 * 1024 * 1024,    # 466MB
        "medium": 1536 * 1024 * 1024,  # 1.5GB
        "large-v3": 2969 * 1024 * 1024 # 2.9GB
    }
    return model_sizes.get(model_size, 100 * 1024 * 1024)

def get_current_model_size_on_disk(model_size: str) -> int:
    """현재 디스크에 다운로드된 모델 크기(바이트) 반환"""
    from pathlib import Path

    cache_dir = Path.home() / ".cache" / "huggingface" / "hub"
    model_pattern = f"models--Systran--faster-whisper-{model_size}"

    total_size = 0
    if cache_dir.exists():
        for item in cache_dir.iterdir():
            if model_pattern in item.name:
                # 디렉토리의 모든 파일 크기 합산
                if item.is_dir():
                    for file in item.rglob("*"):
                        if file.is_file():
                            total_size += file.stat().st_size
                elif item.is_file():
                    total_size += item.stat().st_size

    return total_size

async def download_model_background(model_size: str):
    """백그라운드에서 모델 다운로드 및 진행률 업데이트"""
    global download_progress
    import asyncio
    import threading

    # 다운로드 시작 전 현재 크기 기록
    initial_size = get_current_model_size_on_disk(model_size)
    expected_size = get_model_download_size(model_size)

    print(f"[Download] Initial size for {model_size}: {initial_size} bytes / {expected_size} bytes")

    def download_thread():
        try:
            # 다운로드 상태 초기화
            download_progress[model_size] = {
                "status": "downloading",
                "progress": 0,
                "initial_size": initial_size,
                "completed": False
            }

            # 모델 로드 시작 (백그라운드 스레드에서 실행)
            print(f"[Download] Starting download for model: {model_size}")
            temp_handler = WhisperHandler(model_size=model_size)

            # 다운로드 완료
            if model_size in download_progress:
                download_progress[model_size]["progress"] = 100
                download_progress[model_size]["completed"] = True
                print(f"[Download] Model {model_size} downloaded successfully")

            # 다운로드 상태 제거 (완료됨)
            import time
            time.sleep(2)  # 2초 후 상태 제거 (Unity가 100% 표시할 시간 제공)
            if model_size in download_progress:
                del download_progress[model_size]

        except Exception as e:
            print(f"[Download] Error downloading model {model_size}: {e}")
            if model_size in download_progress:
                del download_progress[model_size]

    # 진행률 추적 스레드
    def progress_thread():
        import time

        while model_size in download_progress:
            # 다운로드가 완료되었으면 진행률 업데이트 중지
            if download_progress.get(model_size, {}).get("completed", False):
                time.sleep(0.5)
                continue

            current_size = get_current_model_size_on_disk(model_size)

            # 다운로드된 양 계산 (현재 크기 - 초기 크기)
            downloaded = current_size - initial_size
            remaining = expected_size - initial_size

            if remaining > 0:
                # 진행률 = (다운로드된 양 / 남은 양) * 100
                progress = min(int((downloaded / remaining) * 100), 99)  # 최대 99%까지만
            else:
                # 이미 다운로드 완료된 경우
                progress = 99

            if model_size in download_progress and not download_progress[model_size].get("completed", False):
                download_progress[model_size]["progress"] = progress
                print(f"[Download] Progress for {model_size}: {progress}% (downloaded: {downloaded}/{remaining} bytes)")

            time.sleep(0.5)  # 0.5초마다 업데이트

    # 두 스레드 시작
    download_t = threading.Thread(target=download_thread, daemon=True)
    progress_t = threading.Thread(target=progress_thread, daemon=True)

    download_t.start()
    progress_t.start()

@app.post("/models/download")
async def download_model(model_size: str = Form(...)):
    """
    모델 다운로드 (첫 로드 시 자동 다운로드됨)

    Args:
        model_size: 모델 크기 (tiny, base, small, medium, large-v3)

    Returns:
        다운로드 시작 메시지
    """
    global download_progress

    if not WHISPER_AVAILABLE:
        raise HTTPException(status_code=500, detail="Whisper not available")

    available_models = WhisperHandler.get_available_models()
    if model_size not in available_models:
        raise HTTPException(
            status_code=400,
            detail=f"Invalid model size. Available: {list(available_models.keys())}"
        )

    # 이미 다운로드 중인지 확인
    if model_size in download_progress:
        return {
            "status": "already_downloading",
            "message": f"Model {model_size} is already being downloaded",
            "model_size": model_size
        }

    try:
        # 백그라운드에서 다운로드 시작
        await download_model_background(model_size)

        return {
            "status": "started",
            "message": f"Model {model_size} download started",
            "model_size": model_size
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error starting download: {str(e)}")

@app.delete("/models/{model_size}")
async def delete_model(model_size: str):
    """
    모델 삭제

    Args:
        model_size: 모델 크기 (tiny, base, small, medium, large-v3)

    Returns:
        삭제 성공/실패 메시지
    """
    if not WHISPER_AVAILABLE:
        raise HTTPException(status_code=500, detail="Whisper not available")

    available_models = WhisperHandler.get_available_models()
    if model_size not in available_models:
        raise HTTPException(
            status_code=400,
            detail=f"Invalid model size. Available: {list(available_models.keys())}"
        )

    try:
        import shutil
        from pathlib import Path

        # Hugging Face cache 경로
        cache_dir = Path.home() / ".cache" / "huggingface" / "hub"

        # faster-whisper 모델 이름 형식: models--Systran--faster-whisper-{model_size}
        model_dir_name = f"models--Systran--faster-whisper-{model_size}"
        model_path = cache_dir / model_dir_name

        if model_path.exists():
            shutil.rmtree(model_path)
            print(f"Deleted model directory: {model_path}")

            return {
                "status": "success",
                "message": f"Model {model_size} deleted successfully",
                "model_size": model_size
            }
        else:
            return {
                "status": "success",
                "message": f"Model {model_size} was not downloaded",
                "model_size": model_size
            }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error deleting model: {str(e)}")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
