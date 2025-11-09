"""
서버 설정 파일
"""

# 서버 설정
SERVER_HOST = "0.0.0.0"
SERVER_PORT = 8000

# Whisper 모델 설정
DEFAULT_MODEL = "base"
DEFAULT_LANGUAGE = "ko"

# 모델 다운로드 타임아웃 (초)
MODEL_DOWNLOAD_TIMEOUT = 600

# 사용 가능한 모델 목록
AVAILABLE_MODELS = {
    "tiny": {
        "name": "Tiny",
        "description": "가장 빠름, 정확도 낮음",
        "size": "~75MB"
    },
    "base": {
        "name": "Base",
        "description": "빠르고 적당한 정확도 (추천)",
        "size": "~145MB"
    },
    "small": {
        "name": "Small",
        "description": "균형잡힌 선택",
        "size": "~466MB"
    },
    "medium": {
        "name": "Medium",
        "description": "느리지만 높은 정확도",
        "size": "~1.5GB"
    },
    "large-v3": {
        "name": "Large-v3",
        "description": "최고 정확도, 매우 느림",
        "size": "~2.9GB"
    }
}

# 지원하는 언어
SUPPORTED_LANGUAGES = {
    "ko": "Korean",
    "en": "English",
    "ja": "Japanese",
    "zh": "Chinese"
}

# 오디오 설정
AUDIO_SAMPLE_RATE = 16000
AUDIO_CHANNELS = 1

# 디버그 모드
DEBUG_MODE = True
SAVE_DEBUG_AUDIO = True
