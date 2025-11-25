import base64
import whisper
from faster_whisper import WhisperModel
import tempfile
import math
import re
import torch
from pydantic import BaseModel
from fastapi import FastAPI
from typing import List

class AudioRequest(BaseModel):
    audioData: str

class TextPrompt(BaseModel):
    skillData: List[str]

app = FastAPI()
model = whisper.load_model("base", device="cuda")
model2 = WhisperModel("base", device="cuda", compute_type="float16")
app.state.skillSet = ["fire ball", "metal shield", "small heal"]
app.state.skillSetKorean = ["파이어 볼", "메탈 쉴드", "스몰 힐"]   
app.state.initialPrompt = "The word could be one of: " + ", ".join(app.state.skillSetKorean)
print(torch.cuda.is_available())

@app.get("/")
async def root():
    return {"message": "whisper server is running. Use POST /whisper_stt with JSON {'text': '...'}"}
@app.post("/whisper_stt2")
async def whisper_stt2(request: AudioRequest):
    audio_bytes = base64.b64decode(request.audioData)

    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        tmp.write(audio_bytes)
        tmp.flush()
        tmp_path = tmp.name

    result = model2.transcribe(
        tmp_path,
        initial_prompt=app.state.initialPrompt,
        language="ko"
    )
    
    segments, info = result

    segments = list(segments)

    result_text = " ".join([segment.text for segment in segments])
    normalize_text = result_text.strip().lower()
    clean_text = re.sub(r"[.,!?]", "", normalize_text)

    if len(segments) > 0:
        avg_logprob = segments[0].avg_logprob
        confidence = math.exp(avg_logprob)
    else:
        confidence = 0.0
    confidence_to_string = str(confidence)

    nlu_result = FindActionByNLU(clean_text)
    if(nlu_result["action"] == "none"):
        print("Checking Levenshtein distance..." )
        for i in app.state.skillSetKorean:
            x = getLevenshtein(clean_text, i.lower())
            print("Levenshtein distance between '{}' and '{}' is {}".format(clean_text, i.lower(), x))
            if(x <= 2):
                  skill_evidence = i
                  confidence_to_string = str(max(confidence - x * 5, 0))
                  nlu_result["action"] = "attack"
                  nlu_result["skill"] = True
                  nlu_result["skill_name"] = skill_evidence
                  break   
    
    analyze_result = AnalyzeNLU(clean_text)

    return {
        "type" : nlu_result["action"],
        "audio": request.audioData,
        "text": clean_text,
        "expectation": confidence_to_string,
        "skill_evidence": nlu_result["skill_name"],
        "location": analyze_result["location"],
        "index": analyze_result["index"]
    }
def getLevenshtein(s1, s2):
    if len(s1) < len(s2):
         return getLevenshtein(s2, s1)

    if len(s2) == 0:
         return len(s1)

    previous_row = range(len(s2) + 1)
    for i, c1 in enumerate(s1):
        current_row = [i + 1]
        for j, c2 in enumerate(s2):
            insertions = previous_row[j + 1] + 1
            deletions = current_row[j] + 1
            substitutions = previous_row[j] + (c1 != c2)
            current_row.append(min(insertions, deletions, substitutions))
        previous_row = current_row

    return previous_row[-1]
def FindActionByNLU(clean_text):
    skill_evidence = "No Skill"

    result = {
        "action": "none",
        "skill": False,
        "skill_name": "none"
    }

    if("공격" in clean_text):
        result["action"] = "attack"
    if("이동" in clean_text):
        result["action"] = "move"
    if hasattr(app.state, "skillSet"):
        for i in app.state.skillSetKorean:
            print(clean_text, i)
            if i in clean_text:
                print("EXIST SKILL:", i)
                skill_evidence = i
                result["action"] = "attack"
                result["skill"] = True
                result["skill_name"] = skill_evidence
                break

    return result
def AnalyzeNLU(text):
    attached_text = text.replace(" ", "").strip()

    result = {
        "location": "none",
        "index": 0,
    }

    loc_map = {
        "왼쪽": "left", "좌측": "left", "왼편": "left",
        "오른쪽": "right", "우측": "right", "오른편": "right"
    }

    loc_pattern = "|".join(loc_map.keys())
    match = re.search(loc_pattern, attached_text)

    if match:
        found_word = match.group(0)
        result["location"] = loc_map[found_word]

    num_match = re.search(r"(\d+)(?:번|번째)?", attached_text)
    kor_num_map = {"첫": 1, "두": 2, "세": 3, "네": 4, "다섯": 5}
    kor_num_pattern = "|".join(kor_num_map.keys())
    kor_match = re.search(kor_num_pattern, attached_text)
    if num_match:
        result["index"] = int(num_match.group(1))
    elif kor_match:
        result["index"] = kor_num_map[kor_match.group(0)]
    
    return result
@app.post("/register_skill")
async def register_skill(request: TextPrompt):
    app.state.skillSet = request.skillData
    app.state.initialPrompt = "The word could be one of: " + ", ".join(app.state.skillSet)
    return {"message": "skill registered"}