"""
언어 설정 관리 서비스
"""
from typing import Dict, Optional
from config.settings import SUPPORTED_LANGUAGES, DEFAULT_LANGUAGE


class LanguageManager:
    """언어 설정 관리"""

    def __init__(self):
        self.current_language = DEFAULT_LANGUAGE

    def set_language(self, language_code: str) -> bool:
        """
        언어 설정

        Args:
            language_code: 언어 코드 (ko, en, ja, zh)

        Returns:
            성공 여부
        """
        if language_code not in SUPPORTED_LANGUAGES:
            print(f"Warning: Unsupported language '{language_code}'. Using default: {DEFAULT_LANGUAGE}")
            return False

        self.current_language = language_code
        print(f"Language set to: {language_code} ({SUPPORTED_LANGUAGES[language_code]})")
        return True

    def get_language(self) -> str:
        """현재 언어 코드 반환"""
        return self.current_language

    def get_language_name(self) -> str:
        """현재 언어 이름 반환"""
        return SUPPORTED_LANGUAGES.get(self.current_language, "Unknown")

    def get_supported_languages(self) -> Dict[str, str]:
        """지원하는 언어 목록 반환"""
        return SUPPORTED_LANGUAGES.copy()

    def is_language_supported(self, language_code: str) -> bool:
        """언어가 지원되는지 확인"""
        return language_code in SUPPORTED_LANGUAGES
