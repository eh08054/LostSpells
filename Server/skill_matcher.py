import Levenshtein
import re
from typing import List, Dict

class SkillMatcher:
    def __init__(self, skills: List[str]):
        """
        스킬 매칭 시스템 초기화

        Args:
            skills: 스킬명 리스트 (예: ["가", "나", "다", "라", "마"])
        """
        self.skills = skills

    def calculate_similarity(self, recognized_text: str, skill: str) -> float:
        """
        인식된 텍스트와 스킬의 유사도 계산

        Args:
            recognized_text: Whisper로 인식된 텍스트
            skill: 비교할 스킬명

        Returns:
            0.0 ~ 1.0 사이의 유사도 점수
        """
        # 공백 제거 및 소문자 변환
        text = recognized_text.strip()
        skill = skill.strip()

        if not text:
            return 0.0

        # 1. 정확히 일치하는 경우
        if text == skill:
            return 1.0

        # 2. 부분 문자열로 포함되는 경우
        if skill in text:
            # 스킬이 텍스트에 포함되면 높은 점수
            return 0.95

        if text in skill:
            # 텍스트가 스킬에 포함되면 중간 점수
            return 0.85

        # 3. Levenshtein 거리 기반 유사도
        distance = Levenshtein.distance(text, skill)
        max_len = max(len(text), len(skill))

        if max_len == 0:
            return 0.0

        similarity = 1.0 - (distance / max_len)

        # 4. 자음/모음 분리 비교로 추가 점수
        jamo_similarity = self._calculate_jamo_similarity(text, skill)

        # 두 점수의 가중 평균
        final_score = (similarity * 0.7) + (jamo_similarity * 0.3)

        return max(0.0, min(1.0, final_score))

    def _calculate_jamo_similarity(self, text1: str, text2: str) -> float:
        """
        한글 자음/모음 분리 비교
        "가" vs "갸" → 자음은 같으므로 어느 정도 유사
        """
        try:
            # 한글 자음/모음 분리
            jamo1 = self._decompose_hangul(text1)
            jamo2 = self._decompose_hangul(text2)

            if not jamo1 or not jamo2:
                return 0.0

            # Levenshtein 거리로 자모 비교
            distance = Levenshtein.distance(jamo1, jamo2)
            max_len = max(len(jamo1), len(jamo2))

            if max_len == 0:
                return 0.0

            return 1.0 - (distance / max_len)
        except:
            return 0.0

    def _decompose_hangul(self, text: str) -> str:
        """
        한글을 자음/모음으로 분리
        예: "가" → "ㄱㅏ"
        """
        CHOSUNG = [
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ',
            'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        ]
        JUNGSUNG = [
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
        ]
        JONGSUNG = [
            '', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ',
            'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        ]

        result = []
        for char in text:
            if '가' <= char <= '힣':
                # 유니코드 한글 분해
                code = ord(char) - 0xAC00
                cho = code // (21 * 28)
                jung = (code % (21 * 28)) // 28
                jong = code % 28

                result.append(CHOSUNG[cho])
                result.append(JUNGSUNG[jung])
                if jong > 0:
                    result.append(JONGSUNG[jong])
            else:
                result.append(char)

        return ''.join(result)

    def match_skills(self, recognized_text: str) -> Dict[str, float]:
        """
        모든 스킬과 비교하여 유사도 점수 반환

        Args:
            recognized_text: 인식된 텍스트

        Returns:
            {"가": 0.95, "나": 0.10, ...} 형태의 딕셔너리
        """
        scores = {}

        for skill in self.skills:
            score = self.calculate_similarity(recognized_text, skill)
            scores[skill] = round(score, 2)

        return scores

    def get_best_match(self, recognized_text: str) -> tuple:
        """
        가장 높은 점수의 스킬 반환

        Returns:
            (skill_name, score) 튜플
        """
        scores = self.match_skills(recognized_text)

        if not scores:
            return (None, 0.0)

        best_skill = max(scores.items(), key=lambda x: x[1])
        return best_skill
