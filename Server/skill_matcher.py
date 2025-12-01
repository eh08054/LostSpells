"""
스킬명 매칭 알고리즘
음성 인식 결과에서 가장 유사한 스킬명을 찾음
"""

from difflib import SequenceMatcher
import re


class SkillMatcher:
    def __init__(self):
        """스킬 매처 초기화"""
        self.skills = []

    def set_skills(self, skill_names):
        """
        스킬 목록 설정

        Args:
            skill_names: 스킬명 리스트 (한글 또는 영어)
        """
        self.skills = [name.strip().lower() for name in skill_names]
        print(f"[SkillMatcher] Updated skill list: {len(self.skills)} skills")

    def match(self, recognized_text, threshold=0.6):
        """
        인식된 텍스트에서 가장 유사한 스킬명 찾기

        Args:
            recognized_text: Whisper가 인식한 텍스트
            threshold: 최소 유사도 (0.0 ~ 1.0)

        Returns:
            dict: {
                "matched": 스킬명 (없으면 None),
                "confidence": 유사도 (0.0 ~ 1.0),
                "candidates": 후보 리스트 (상위 3개)
            }
        """
        if not self.skills:
            return {
                "matched": None,
                "confidence": 0.0,
                "candidates": []
            }

        # 텍스트 정규화
        text = self._normalize(recognized_text)

        # 모든 스킬과 유사도 계산
        similarities = []
        for skill in self.skills:
            # 완전 일치 체크
            if text == skill:
                similarities.append((skill, 1.0))
                continue

            # 부분 일치 체크
            if skill in text:
                # 스킬명이 인식 텍스트에 포함되어 있으면 높은 점수
                similarities.append((skill, 0.95))
                continue
            elif text in skill:
                # 인식 텍스트가 스킬명의 일부면 중간 점수
                ratio = len(text) / len(skill)
                similarities.append((skill, ratio * 0.85))
                continue

            # 유사도 계산
            ratio = SequenceMatcher(None, text, skill).ratio()
            similarities.append((skill, ratio))

        # 유사도 순으로 정렬
        similarities.sort(key=lambda x: x[1], reverse=True)

        # 최고 유사도 스킬
        best_skill, best_score = similarities[0] if similarities else (None, 0.0)

        # 상위 3개 후보
        candidates = [
            {"name": skill, "confidence": round(score, 2)}
            for skill, score in similarities[:3]
        ]

        return {
            "action": "attack",
            "matched": best_skill if best_score >= threshold else None,
            "confidence": round(best_score, 2),
            "candidates": candidates
        }

    def _normalize(self, text):
        """텍스트 정규화 (소문자, 공백 제거)"""
        text = text.lower().strip()
        # 공백 제거
        text = re.sub(r'\s+', '', text)
        return text
