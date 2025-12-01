import re

class ChooseActions:
    def FindActions(self, text, skill_list):
        
        normalize_text = text.strip().lower() 
        clean_text = re.sub(r"[.,!?]", "", normalize_text)
        clean_text_nospace = clean_text.replace(" ", "")
        
        action_patterns = {
            "attack": ["공격", "발사"],
            "move": ["이동", "움직"],
            "settings": ["설정", "옵션", "환경설정"],
            "go_back": ["뒤로가기", "뒤로가", "뒤로 가"],
            "pause": ["메뉴", "정지", "1시정지", "1시 정지", "일시정지", "1C정지", "1C 정지", "일C정지", "일C 정지", "일시 정지" "퍼즈", "포즈", "1C 종지"],
            "restart": ["재도전", "재시작", "다시 도전"], 
            "resume":["계속하기, 전투 화면", "전투화면"],
            "revival": ["부활"],
            "title": ["타이틀 화면", "타이틀로"],
            "shop": ["상점"]
        }
        
        order_patterns = {
            "open_ui": ["열기", "열어", "켜줘", "해줘"],
            "close_ui": ["닫기", "닫아", "꺼줘", "꺼져"]
        }
        
        result_dict = {
            "action": "none",
            "order": "none",
            "skill": False
        }
        
        # 1. 스킬 체크
        for i in skill_list:
            if i.replace(" ", "").lower() in clean_text_nospace:
                result_dict["action"] = "attack"
                result_dict["skill"] = True
                break

        # 2. 액션 패턴 체크 
        if not result_dict["skill"]:
            for action, keywords in action_patterns.items():
                if any(word in clean_text for word in keywords):
                    result_dict["action"] = action
                    break
        
        # 3. 명령 체크 
        for order, keywords in order_patterns.items():
            if any(word in clean_text for word in keywords):
                result_dict["order"] = order 
                break

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
        
        kor_num_map = {"첫": 1, "두": 2, "세": 3, "네": 4, "다섯": 5}
        kor_num_pattern = "|".join(kor_num_map.keys())
        kor_match = re.search(kor_num_pattern, attached_text)
        
        if num_match:
            result["location"] = int(num_match.group(1))
        elif kor_match:
            result["location"] = kor_num_map[kor_match.group(0)]
        
        return result