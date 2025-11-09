"""
Whisper 모델 다운로드 및 관리 서비스
"""
from pathlib import Path
from typing import Dict, Optional, Tuple
import os
import sys

try:
    from huggingface_hub import snapshot_download, hf_hub_download
    from huggingface_hub.utils import tqdm as hf_tqdm
    HF_AVAILABLE = True
except ImportError:
    HF_AVAILABLE = False
    print("Warning: huggingface_hub not available. Install with: pip install huggingface-hub")

from config.settings import AVAILABLE_MODELS


class ModelManager:
    """모델 다운로드 및 상태 관리"""

    def __init__(self):
        self.cache_dir = Path.home() / ".cache" / "huggingface" / "hub"
        self.download_progress = {}  # 다운로드 진행 상태 저장

    def get_model_cache_path(self, model_size: str) -> Optional[Path]:
        """모델 캐시 경로 조회"""
        model_pattern = f"models--Systran--faster-whisper-{model_size}"

        if not self.cache_dir.exists():
            return None

        for item in self.cache_dir.iterdir():
            if model_pattern in item.name:
                return item
        return None

    def is_model_downloaded(self, model_size: str) -> bool:
        """모델이 다운로드되어 있는지 확인"""
        return self.get_model_cache_path(model_size) is not None

    def get_model_status(self, model_size: str) -> Dict:
        """모델 상태 정보 반환"""
        is_downloaded = self.is_model_downloaded(model_size)
        download_progress = self.download_progress.get(model_size, 0)

        status = {
            "model": model_size,
            "downloaded": is_downloaded,
            "download_progress": download_progress,
            "status": "ready" if is_downloaded else ("downloading" if download_progress > 0 else "not_downloaded")
        }

        # 모델 정보 추가
        if model_size in AVAILABLE_MODELS:
            status.update(AVAILABLE_MODELS[model_size])

        return status

    def get_all_models_status(self) -> Dict[str, Dict]:
        """모든 모델의 상태 반환"""
        models_status = {}
        for model_id in AVAILABLE_MODELS.keys():
            models_status[model_id] = self.get_model_status(model_id)
        return models_status

    def download_model(self, model_size: str, progress_callback=None) -> Tuple[bool, str]:
        """
        모델 다운로드

        Args:
            model_size: 모델 크기 (tiny, base, small, medium, large-v3)
            progress_callback: 진행률 콜백 함수 (percentage: float)

        Returns:
            (성공여부, 메시지)
        """
        if not HF_AVAILABLE:
            return False, "huggingface_hub not installed"

        if model_size not in AVAILABLE_MODELS:
            return False, f"Invalid model: {model_size}"

        # 이미 다운로드되어 있는지 확인
        if self.is_model_downloaded(model_size):
            return True, f"Model {model_size} already downloaded"

        try:
            repo_id = f"Systran/faster-whisper-{model_size}"

            print(f"Downloading model: {repo_id}")
            self.download_progress[model_size] = 0

            # huggingface_hub를 사용하여 다운로드
            # 실제로는 WhisperModel 초기화 시 자동으로 다운로드됨
            # 하지만 명시적으로 다운로드하려면:
            try:
                from faster_whisper import WhisperModel

                # 임시로 모델 로드 (이 과정에서 다운로드됨)
                temp_model = WhisperModel(model_size, device="cpu", compute_type="int8")
                del temp_model

                self.download_progress[model_size] = 100

                if progress_callback:
                    progress_callback(100)

                return True, f"Model {model_size} downloaded successfully"

            except Exception as e:
                return False, f"Download failed: {str(e)}"

        except Exception as e:
            self.download_progress[model_size] = 0
            return False, f"Download error: {str(e)}"

    def get_download_progress(self, model_size: str) -> float:
        """다운로드 진행률 조회 (0-100)"""
        return self.download_progress.get(model_size, 0)

    def delete_model(self, model_size: str) -> Tuple[bool, str]:
        """모델 삭제"""
        cache_path = self.get_model_cache_path(model_size)

        if cache_path is None:
            return False, f"Model {model_size} not found"

        try:
            import shutil
            shutil.rmtree(cache_path)
            return True, f"Model {model_size} deleted successfully"
        except Exception as e:
            return False, f"Failed to delete model: {str(e)}"

    def get_model_size_on_disk(self, model_size: str) -> Optional[int]:
        """디스크에서 모델이 차지하는 크기 (바이트)"""
        cache_path = self.get_model_cache_path(model_size)

        if cache_path is None:
            return None

        total_size = 0
        for dirpath, dirnames, filenames in os.walk(cache_path):
            for filename in filenames:
                filepath = os.path.join(dirpath, filename)
                if os.path.exists(filepath):
                    total_size += os.path.getsize(filepath)

        return total_size
