import re
from rapidfuzz import process, fuzz

class ChooseActions:
    def FindActions(self, text, skill_list):
        
        normalize_text = text.strip().lower() 
        clean_text = re.sub(r"[.,!?]", "", normalize_text)
        clean_text_nospace = clean_text.replace(" ", "")
        
        action_patterns = {
            "attack": ["공격", "발사"],
            "move": ["이동", "움직"],
            "settings": ["설정", "옵션", "환경설정"],
            "go_back": ["뒤로가기", "뒤로가"],
            "pause": ["메뉴", "정지", "일시정지", "퍼즈", "포즈"],
            "restart": ["재도전", "재시작", "다시도전"], 
            "resume":["계속하기", "이어하기"],
            "revival": ["부활"],
            "title": ["타이틀 화면", "타이틀로"],
            "shop": ["상점"]
        }
        
        order_patterns = {
            "open_ui": ["열기", "열어", "켜줘", "해줘"],
            "close_ui": ["닫기", "닫아", "꺼줘"]
        }
        
        result_dict = {
            "action": "none",
            "order": "none",
            "skill": False
        }
        
        skill_list_nospace = {}
        for skill in skill_list:
            skill_list_nospace[skill.replace(" ", "")] = skill
            best_match_skill = process.extractOne(clean_text_nospace, skill_list_nospace.keys(), scorer=fuzz.partial_ratio)

        if best_match_skill:
            matched_skill, score, _ = best_match_skill
            if score >= 80: # 기준 점수 
                result_dict["action"] = "attack"
                result_dict["skill"] = True
                print(f"Fuzzy Match Skill: '{clean_text}' -> '{matched_skill}' ({score}점)")



        best_score_skill = 75
        # 2. 액션 패턴 체크 
        if not result_dict["skill"]:
            action_map = {}
            for action, keywords in action_patterns.items():
                for k in keywords:
                    action_map[k] = action
                    best_match = process.extractOne(clean_text_nospace, action_map.keys(), scorer=fuzz.partial_ratio)
                    if best_match:
                        matched_word, score, _ = best_match
                        if score >= best_score_skill: # 기준 점수 
                            best_score_skill = score
                            result_dict["action"] = action_map[matched_word]
                            print(f"Fuzzy Match: '{clean_text_nospace}' -> '{matched_word}' ({score}점)")
        
        order_map = {}
        # 3. 명령 체크 
        for order, keywords in order_patterns.items():
                for k in keywords:
                    order_map[k] = order
                    best_match = process.extractOne(clean_text_nospace, order_map.keys(), scorer=fuzz.partial_ratio)
                    if best_match:
                        matched_word, score, _ = best_match
                        if score >= best_score_skill: # 기준 점수 
                            best_score_skill = score
                            result_dict["order"] = order_map[matched_word]
                            print(f"Fuzzy Match: '{clean_text_nospace}' -> '{matched_word}' ({score}점)")
        

        return result_dict

    def AnalyzeNLU(self, text):
        attached_text = text.replace(" ", "").strip()

        result = {
            "direction": "none",
            "location": 0,
        }

        loc_map = {
            "왼쪽": "left", "좌측": "left", "왼편": "left",
            "오른쪽": "right", "우측": "right", "오른편": "right"
        }

        loc_pattern = "|".join(loc_map.keys())
        
        match = re.search(loc_pattern, attached_text)

        if match:
            found_word = match.group(0)
            result["direction"] = loc_map[found_word]

        # 숫자 파싱
        num_match = re.search(r"(\d+)(?:번|번째)?", attached_text)
        
        kor_num_map = {"첫": 1, "한": 1, "두": 2, "세": 3, "네": 4, "다섯": 5}
        kor_num_pattern = "|".join(kor_num_map.keys())
        kor_match = re.search(kor_num_pattern, attached_text)
        
        if num_match:
            result["location"] = int(num_match.group(1))
        elif kor_match:
            result["location"] = kor_num_map[kor_match.group(0)]
        
        return result
    def GetWords(self):
        return ["공격", 
                "이동", "움직", 
                "설정", "옵션", "환경설정",
                "메뉴", "정지", "일시정지", "퍼즈",
                "뒤로가기", "뒤로가", 
                "재도전", "재시작",
                "계속하기", "이어하기",
                "부활", 
                "타이틀 화면", "타이틀로", 
                "상점",
                "왼쪽", "좌측", "왼편",
                "오른쪽", "우측", "오른편",
                "켜줘", "열기", "열어",
                "꺼줘", "닫기", "닫아",
                "한", "두", "세", "네", "다섯",]