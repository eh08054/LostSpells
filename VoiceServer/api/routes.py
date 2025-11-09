"""
FastAPI routes for voice recognition server
"""
from fastapi import APIRouter, UploadFile, File, Form, HTTPException
from fastapi.responses import JSONResponse
from typing import Optional
import os
import tempfile

from .models import (
    RecognitionResponse,
    ModelStatusResponse,
    ModelsInfoResponse,
    LanguageInfoResponse,
    HealthCheckResponse
)
from config.settings import AVAILABLE_MODELS
from core import WhisperHandler, SkillMatcher
from services import ModelManager, LanguageManager

# Router 생성
router = APIRouter()

# Global instances (will be initialized by server.py)
whisper_handler: Optional[WhisperHandler] = None
skill_matcher: Optional[SkillMatcher] = None
model_manager: Optional[ModelManager] = None
language_manager: Optional[LanguageManager] = None


def init_services(whisper: WhisperHandler, matcher: SkillMatcher,
                  model_mgr: ModelManager, lang_mgr: LanguageManager):
    """Initialize service instances"""
    global whisper_handler, skill_matcher, model_manager, language_manager
    whisper_handler = whisper
    skill_matcher = matcher
    model_manager = model_mgr
    language_manager = lang_mgr


@router.get("/", response_model=HealthCheckResponse)
async def health_check():
    """서버 상태 확인"""
    return {
        "status": "success",
        "message": "Voice Recognition Server is running",
        "current_model": whisper_handler.current_model_size if whisper_handler else "unknown",
        "current_language": language_manager.get_language() if language_manager else "unknown"
    }


@router.get("/models", response_model=ModelsInfoResponse)
async def get_models():
    """
    사용 가능한 모델 목록과 다운로드 상태 반환
    """
    models_with_status = {}

    for model_id, model_info in AVAILABLE_MODELS.items():
        status = model_manager.get_model_status(model_id)
        models_with_status[model_id] = {
            **model_info,
            "downloaded": status["downloaded"],
            "download_progress": status["download_progress"],
            "status": status["status"]
        }

    return {
        "status": "success",
        "current_model": whisper_handler.current_model_size,
        "available_models": models_with_status
    }


@router.get("/models/{model_size}/status", response_model=ModelStatusResponse)
async def get_model_status(model_size: str):
    """
    특정 모델의 상태 확인 (다운로드 여부, 다운로드 진행률)
    """
    if model_size not in AVAILABLE_MODELS:
        raise HTTPException(status_code=400, detail=f"Invalid model size: {model_size}")

    status = model_manager.get_model_status(model_size)
    return status


@router.post("/models/select")
async def select_model(model_size: str = Form(...)):
    """
    사용할 모델 변경
    """
    if model_size not in AVAILABLE_MODELS:
        raise HTTPException(status_code=400, detail=f"Invalid model size: {model_size}")

    # 모델이 다운로드되어 있는지 확인
    if not model_manager.is_model_downloaded(model_size):
        raise HTTPException(
            status_code=400,
            detail=f"Model {model_size} is not downloaded. Please download it first."
        )

    try:
        whisper_handler.change_model(model_size)
        return {
            "status": "success",
            "message": f"Model changed to {model_size}",
            "current_model": model_size
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to change model: {str(e)}")


@router.post("/models/download")
async def download_model(model_size: str = Form(...)):
    """
    모델 다운로드
    다운로드 진행 상태는 /models/{model_size}/status 엔드포인트로 확인 가능
    """
    if model_size not in AVAILABLE_MODELS:
        raise HTTPException(status_code=400, detail=f"Invalid model size: {model_size}")

    # 이미 다운로드되어 있는지 확인
    if model_manager.is_model_downloaded(model_size):
        return {
            "status": "success",
            "message": f"Model {model_size} is already downloaded",
            "downloaded": True
        }

    try:
        # 백그라운드에서 다운로드 시작
        success, message = model_manager.download_model(model_size)

        if success:
            return {
                "status": "success",
                "message": message,
                "downloaded": True
            }
        else:
            raise HTTPException(status_code=500, detail=message)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Download failed: {str(e)}")


@router.post("/recognize", response_model=RecognitionResponse)
async def recognize_audio(
    audio: UploadFile = File(...),
    language: Optional[str] = Form(None)
):
    """
    음성 파일을 받아서 텍스트로 변환하고 스킬 매칭 수행
    """
    # 언어가 지정되지 않으면 현재 설정된 언어 사용
    if language is None:
        language = language_manager.get_language()

    # 임시 파일로 저장
    temp_file = None
    try:
        # 임시 파일 생성
        with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as temp_file:
            content = await audio.read()
            temp_file.write(content)
            temp_file_path = temp_file.name

        # 음성 인식
        result = whisper_handler.transcribe_audio(temp_file_path, language=language)
        recognized_text = result["text"]
        processing_time = result["processing_time"]

        # 스킬 매칭 (스킬이 설정되어 있는 경우)
        matched_skill = None
        similarity_score = None

        if skill_matcher and skill_matcher.skills:
            matched_skill, similarity_score = skill_matcher.get_best_match(recognized_text)

        return {
            "status": "success",
            "text": recognized_text,
            "processing_time": processing_time,
            "matched_skill": matched_skill,
            "similarity_score": similarity_score
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Recognition failed: {str(e)}")

    finally:
        # 임시 파일 삭제
        if temp_file and os.path.exists(temp_file_path):
            os.unlink(temp_file_path)


@router.post("/set-skills")
async def set_skills(skills: list[str] = Form(...)):
    """
    매칭에 사용할 스킬 목록 설정
    """
    global skill_matcher

    try:
        skill_matcher = SkillMatcher(skills)
        return {
            "status": "success",
            "message": f"Skills updated: {len(skills)} skills registered",
            "skills": skills
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to set skills: {str(e)}")


@router.get("/skills")
async def get_skills():
    """
    현재 설정된 스킬 목록 반환
    """
    if skill_matcher and skill_matcher.skills:
        return {
            "status": "success",
            "skills": skill_matcher.skills
        }
    else:
        return {
            "status": "success",
            "skills": []
        }


@router.post("/language/set")
async def set_language(language: str = Form(...)):
    """
    음성 인식 언어 설정
    """
    success = language_manager.set_language(language)

    if not success:
        supported = language_manager.get_supported_languages()
        raise HTTPException(
            status_code=400,
            detail=f"Invalid language: {language}. Supported languages: {list(supported.keys())}"
        )

    return {
        "status": "success",
        "message": f"Language set to {language}",
        "current_language": language
    }


@router.get("/language", response_model=LanguageInfoResponse)
async def get_language_info():
    """
    현재 언어 설정 및 지원하는 언어 목록 반환
    """
    return {
        "status": "success",
        "current_language": language_manager.get_language(),
        "supported_languages": language_manager.get_supported_languages()
    }
