"""
Pydantic models for API request/response validation
"""
from pydantic import BaseModel
from typing import List, Dict, Optional


class RecognitionRequest(BaseModel):
    """음성 인식 요청"""
    language: Optional[str] = "ko"


class RecognitionResponse(BaseModel):
    """음성 인식 응답"""
    status: str
    text: str
    processing_time: float
    matched_skill: Optional[str] = None
    similarity_score: Optional[float] = None


class ModelSelectRequest(BaseModel):
    """모델 선택 요청"""
    model_size: str


class ModelDownloadRequest(BaseModel):
    """모델 다운로드 요청"""
    model_size: str


class ModelStatusResponse(BaseModel):
    """모델 상태 응답"""
    model: str
    downloaded: bool
    download_progress: float
    status: str


class ModelsInfoResponse(BaseModel):
    """전체 모델 정보 응답"""
    status: str
    current_model: str
    available_models: Dict[str, Dict]


class SkillSetRequest(BaseModel):
    """스킬 설정 요청"""
    skills: List[str]


class LanguageSetRequest(BaseModel):
    """언어 설정 요청"""
    language: str


class LanguageInfoResponse(BaseModel):
    """언어 정보 응답"""
    status: str
    current_language: str
    supported_languages: Dict[str, str]


class HealthCheckResponse(BaseModel):
    """서버 상태 확인 응답"""
    status: str
    message: str
    current_model: str
    current_language: str
