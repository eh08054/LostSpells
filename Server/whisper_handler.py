from faster_whisper import WhisperModel
import time
from pathlib import Path
import os

class WhisperHandler:
    # 사용 가능한 모델 목록
    AVAILABLE_MODELS = {
        "tiny": {"name": "Tiny", "description": "가장 빠름, 정확도 낮음", "size": "~75MB"},
        "base": {"name": "Base", "description": "빠르고 적당한 정확도 (추천)", "size": "~145MB"},
        "small": {"name": "Small", "description": "균형잡힌 선택", "size": "~466MB"},
        "medium": {"name": "Medium", "description": "느리지만 높은 정확도", "size": "~1.5GB"},
        "large-v3": {"name": "Large-v3", "description": "최고 정확도, 매우 느림", "size": "~2.9GB"}
    }

    def __init__(self, model_size="base"):
        """
        Faster Whisper 모델 초기화
        model_size: tiny, base, small, medium, large-v3
        """
        print(f"Loading Faster-Whisper model: {model_size}...")
        # device: "cpu" 또는 "cuda"
        # compute_type: "int8", "int8_float16", "float16", "float32"
        self.current_model_size = model_size
        self.model = WhisperModel(model_size, device="cpu", compute_type="int8")
        print(f"Faster-Whisper model loaded successfully!")

    @classmethod
    def get_available_models(cls):
        """사용 가능한 모델 목록 반환"""
        return cls.AVAILABLE_MODELS

    @classmethod
    def check_model_downloaded(cls, model_size):
        """
        모델이 다운로드되어 있는지 확인
        faster-whisper는 모델을 캐시에 자동 다운로드하므로,
        캐시 디렉토리에 모델이 있는지 확인
        """
        try:
            # faster-whisper의 기본 캐시 디렉토리
            cache_dir = Path.home() / ".cache" / "huggingface" / "hub"

            # 모델 이름 패턴 (예: models--Systran--faster-whisper-base)
            model_pattern = f"models--Systran--faster-whisper-{model_size}"

            if cache_dir.exists():
                for item in cache_dir.iterdir():
                    if model_pattern in item.name:
                        return True
            return False
        except Exception as e:
            print(f"Error checking model: {e}")
            return False

    def change_model(self, new_model_size):
        """모델 변경"""
        print(f"Changing model from {self.current_model_size} to {new_model_size}...")
        self.current_model_size = new_model_size
        self.model = WhisperModel(new_model_size, device="cpu", compute_type="int8")
        print(f"Model changed to {new_model_size} successfully!")

    def transcribe_audio(self, audio_path: str, language: str = "ko") -> dict:
        """
        음성 파일을 텍스트로 변환

        Args:
            audio_path: 음성 파일 경로
            language: 언어 코드 (ko=한국어)

        Returns:
            {
                "text": 인식된 텍스트,
                "processing_time": 처리 시간(초)
            }
        """
        start_time = time.time()

        # Faster-Whisper로 음성 인식
        segments, info = self.model.transcribe(
            audio_path,
            language=language,
            beam_size=5
        )

        # 세그먼트를 하나의 텍스트로 결합
        text = " ".join([segment.text for segment in segments])

        processing_time = time.time() - start_time

        return {
            "text": text.strip(),
            "processing_time": processing_time
        }

    def transcribe_with_segments(self, audio_path: str, language: str = "ko") -> dict:
        """
        음성 파일을 세그먼트별로 변환 (더 상세한 정보)
        """
        start_time = time.time()

        segments, info = self.model.transcribe(
            audio_path,
            language=language,
            beam_size=5
        )

        # 세그먼트 정보 수집
        segment_list = []
        full_text = []

        for segment in segments:
            segment_list.append({
                "start": segment.start,
                "end": segment.end,
                "text": segment.text
            })
            full_text.append(segment.text)

        processing_time = time.time() - start_time

        return {
            "text": " ".join(full_text).strip(),
            "segments": segment_list,
            "language": info.language,
            "processing_time": processing_time
        }
