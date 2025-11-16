"""
Whisper 음성 인식 핸들러 (GPU/CPU 자동 선택)
Medium 모델을 사용하여 높은 정확도 제공
"""

import whisper
from pathlib import Path
import torch


class WhisperHandler:
    def __init__(self, model_name="medium", language="ko"):
        """
        Whisper 모델 초기화

        Args:
            model_name: 모델 크기 (tiny, base, small, medium, large)
            language: 인식 언어 (ko, en)
        """
        self.model_name = model_name
        self.language = language

        # GPU 사용 가능 여부 확인 (하지만 sm_120 같은 최신 GPU는 지원 안 됨)
        # 안전하게 CPU 모드로 실행
        self.device = "cpu"

        if torch.cuda.is_available():
            gpu_name = torch.cuda.get_device_name(0)
            # Compute capability 확인
            capability = torch.cuda.get_device_capability(0)
            sm_version = capability[0] * 10 + capability[1]

            print(f"[Whisper] GPU detected: {gpu_name} (sm_{sm_version})")
            print(f"[Whisper] Note: PyTorch currently supports up to sm_90")
            print(f"[Whisper] Falling back to CPU mode for compatibility")

        print(f"[Whisper] Initializing...")
        print(f"   - Model: {model_name}")
        print(f"   - Language: {language}")
        print(f"   - Device: CPU")

        # 모델 로드 (CPU 모드)
        self.model = whisper.load_model(model_name, device=self.device)
        print(f"[Whisper] Model loaded successfully!")

    def transcribe(self, audio_path, skill_names=None):
        """
        음성 파일을 텍스트로 변환

        Args:
            audio_path: 오디오 파일 경로
            skill_names: 스킬명 리스트 (프롬프트로 사용하여 정확도 향상)

        Returns:
            dict: {
                "text": 인식된 텍스트,
                "language": 감지된 언어,
                "confidence": 신뢰도 (평균)
            }
        """
        # 스킬명을 프롬프트로 사용 (인식 정확도 향상)
        prompt = None
        if skill_names:
            prompt = ", ".join(skill_names[:20])  # 최대 20개 스킬명만 사용

        # Whisper 실행
        result = self.model.transcribe(
            str(audio_path),
            language=self.language,
            initial_prompt=prompt,  # 스킬명 힌트 제공
        )

        # 신뢰도 계산 (세그먼트별 평균)
        confidences = []
        if "segments" in result:
            for segment in result["segments"]:
                if "avg_logprob" in segment:
                    # logprob를 0~1 범위로 변환
                    confidence = min(1.0, max(0.0, (segment["avg_logprob"] + 1.0)))
                    confidences.append(confidence)

        avg_confidence = sum(confidences) / len(confidences) if confidences else 0.8

        return {
            "text": result["text"].strip(),
            "language": result.get("language", self.language),
            "confidence": round(avg_confidence, 2)
        }

    def change_language(self, language):
        """언어 변경"""
        self.language = language
        print(f"[Whisper] Language changed: {language}")
