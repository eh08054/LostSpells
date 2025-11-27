"""
Lost Spells Voice Server (GPU 가속)
Unity에서 음성 파일을 업로드하면 Whisper로 인식 후 스킬 매칭
"""

from fastapi import FastAPI, File, UploadFile, Form
from fastapi.middleware.cors import CORSMiddleware
from pathlib import Path
import uvicorn
import time

from whisper_handler import WhisperHandler
from skill_matcher import SkillMatcher

app = FastAPI(title="Lost Spells Voice Server")

# CORS 설정 (Unity에서 접근 가능하도록)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Whisper 및 Skill Matcher 초기화
whisper_handler = None
skill_matcher = SkillMatcher()

# 임시 파일 저장 디렉토리
TEMP_DIR = Path("temp")
TEMP_DIR.mkdir(exist_ok=True)


@app.on_event("startup")
async def startup_event():
    """서버 시작 시 Whisper 모델 로드"""
    global whisper_handler
    print("=" * 60)
    print("Lost Spells Voice Server (CPU)")
    print("=" * 60)
    whisper_handler = WhisperHandler(model_name="small", language="ko")
    print("=" * 60)
    print("[Server] Ready!")
    print("=" * 60)


@app.get("/")
async def root():
    """서버 상태 확인"""
    device = whisper_handler.device if whisper_handler else "unknown"
    return {
        "status": "online",
        "message": "Lost Spells Voice Server is running",
        "version": "2.0.0",
        "model": "small",
        "device": device.upper()
    }


@app.get("/health")
async def health_check():
    """헬스 체크"""
    return {"status": "healthy"}


@app.post("/recognize")
async def recognize_voice(
    audio: UploadFile = File(...),
    language: str = Form("ko"),
    skills: str = Form("")
):
    """
    음성 인식 및 스킬 매칭

    Args:
        audio: WAV 파일
        language: 언어 (ko, en)
        skills: 스킬명 리스트 (쉼표로 구분, 예: "파이어볼,아이스블래스트,힐")

    Returns:
        {
            "success": true/false,
            "text": "인식된 텍스트",
            "matched_skill": "매칭된 스킬명",
            "confidence": 0.95,
            "candidates": [...],
            "processing_time": 0.23
        }
    """
    start_time = time.time()

    try:
        # 언어 변경 (필요시)
        if whisper_handler.language != language:
            whisper_handler.change_language(language)

        # 오디오 파일 저장
        audio_path = TEMP_DIR / f"recording_{int(time.time() * 1000)}.wav"
        audio_data = await audio.read()
        print(f"[Server] Received audio: {len(audio_data)} bytes")

        with open(audio_path, "wb") as f:
            f.write(audio_data)

        print(f"[Server] Audio saved to: {audio_path}")

        # 스킬 목록 파싱
        skill_list = []
        if skills:
            skill_list = [s.strip() for s in skills.split(",") if s.strip()]
            skill_matcher.set_skills(skill_list)

        # 음성 인식
        print(f"[Server] Starting transcription...")
        result = whisper_handler.transcribe(audio_path, skill_names=skill_list)
        print(f"[Server] Transcription result: {result['text']}")

        # 스킬 매칭
        match_result = skill_matcher.match(result["text"]) if skill_list else {
            "matched": None,
            "confidence": 0.0,
            "candidates": []
        }

        # 임시 파일 삭제
        if audio_path.exists():
            audio_path.unlink()
            print(f"[Server] Cleaned up temporary file")

        processing_time = round(time.time() - start_time, 2)

        return {
            "success": True,
            "text": result["text"],
            "matched_skill": match_result["matched"],
            "confidence": match_result["confidence"],
            "candidates": match_result["candidates"],
            "processing_time": processing_time
        }

    except Exception as e:
        import traceback
        print(f"[ERROR] {e}")
        print(f"[ERROR] Traceback:")
        traceback.print_exc()
        return {
            "success": False,
            "error": str(e),
            "text": "",
            "matched_skill": None,
            "confidence": 0.0,
            "candidates": [],
            "processing_time": round(time.time() - start_time, 2)
        }


@app.post("/set-language")
async def set_language(language: str = Form("ko")):
    """
    음성 인식 언어 변경

    Args:
        language: 언어 코드 (ko, en)
    """
    whisper_handler.change_language(language)
    return {
        "success": True,
        "language": language
    }


if __name__ == "__main__":
    print("=" * 60)
    print("Lost Spells Voice Server")
    print("=" * 60)
    print("Starting server on http://localhost:8000")
    print("Press Ctrl+C to stop")
    print("=" * 60)

    uvicorn.run(app, host="0.0.0.0", port=8000)
