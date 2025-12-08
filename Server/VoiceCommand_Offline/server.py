"""
음성 명령 서버 (오프라인 버전)
- 로컬 Whisper 모델: 음성 → 텍스트 변환
- 키워드 기반 분류: 의도 파악 및 명령 분류
- 인터넷 연결 불필요, API 키 불필요
"""

import os
import base64
import tempfile
import json
from fastapi import FastAPI, HTTPException, File, UploadFile, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import time
import warnings

# Whisper 경고 숨기기
warnings.filterwarnings("ignore", category=UserWarning)
warnings.filterwarnings("ignore", category=FutureWarning)

# 로컬 Whisper 모델 로드
print("Loading local Whisper model...")
import whisper

# 모델 크기 선택 (tiny, base, small, medium, large)
# tiny: 가장 빠름, 정확도 낮음 (~39MB)
# base: 빠름, 적당한 정확도 (~74MB)
# small: 균형 (~244MB)
# medium: 느림, 높은 정확도 (~769MB)
# large: 가장 느림, 최고 정확도 (~1.5GB)
MODEL_SIZE = os.getenv("WHISPER_MODEL", "base")

print(f"Loading Whisper model: {MODEL_SIZE}")
whisper_model = whisper.load_model(MODEL_SIZE)
print(f"Whisper model '{MODEL_SIZE}' loaded successfully!")

app = FastAPI(title="Voice Command Server (Offline)", version="1.0.0")

# CORS 설정
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

print("Voice Command Server (Offline) initialized!")


class AudioRequest(BaseModel):
    audioData: str  # Base64 인코딩된 WAV 데이터


class CommandResponse(BaseModel):
    text: str  # 인식된 텍스트
    command: str  # 실행할 명령어
    confidence: float  # 신뢰도


# 사용 가능한 함수 정의
AVAILABLE_FUNCTIONS = [
    {
        "name": "OpenSettings",
        "description": "설정창, 옵션창을 엽니다",
        "keywords": ["설정", "옵션", "세팅", "settings", "options"]
    },
    {
        "name": "CloseSettings",
        "description": "설정창을 닫고 인게임으로 돌아갑니다",
        "keywords": ["설정 닫", "옵션 닫", "설정창 닫", "close settings"]
    },
    {
        "name": "OpenMenu",
        "description": "게임 메뉴를 엽니다",
        "keywords": ["메뉴", "menu"]
    },
    {
        "name": "CloseMenu",
        "description": "게임 메뉴를 닫습니다",
        "keywords": ["메뉴 닫", "close menu"]
    },
    {
        "name": "PauseGame",
        "description": "게임을 일시정지합니다",
        "keywords": ["일시정지", "멈춰", "정지", "퍼즈", "pause", "stop"]
    },
    {
        "name": "ResumeGame",
        "description": "일시정지된 게임을 재개합니다",
        "keywords": ["계속", "재개", "플레이", "resume", "continue"]
    },
    {
        "name": "RestartGame",
        "description": "게임을 재시작합니다",
        "keywords": ["재시작", "다시 시작", "리스타트", "재도전", "다시", "restart"]
    },
    {
        "name": "QuitToMainMenu",
        "description": "메인 메뉴로 나갑니다",
        "keywords": ["나가기", "종료", "quit", "exit"]
    },
    {
        "name": "OpenInventory",
        "description": "인벤토리/가방을 엽니다",
        "keywords": ["인벤토리", "가방", "아이템", "소지품", "inventory"]
    },
    {
        "name": "CloseInventory",
        "description": "인벤토리를 닫습니다",
        "keywords": ["인벤토리 닫", "가방 닫", "close inventory"]
    },
    {
        "name": "OpenMap",
        "description": "지도를 엽니다",
        "keywords": ["지도", "맵", "map"]
    },
    {
        "name": "CloseMap",
        "description": "지도를 닫습니다",
        "keywords": ["지도 닫", "맵 닫", "close map"]
    },
    {
        "name": "ShowHelp",
        "description": "도움말을 표시합니다",
        "keywords": ["도움말", "도와", "help"]
    },
    {
        "name": "StartGame",
        "description": "게임 모드 선택 화면으로 이동합니다",
        "keywords": ["게임 시작", "시작", "플레이", "start", "play"]
    },
    {
        "name": "SelectStoryMode",
        "description": "스토리 모드를 선택합니다",
        "keywords": ["스토리 모드", "스토리", "챕터 모드", "story"]
    },
    {
        "name": "SelectEndlessMode",
        "description": "무한 모드를 선택합니다",
        "keywords": ["무한 모드", "엔드리스", "무한", "endless"]
    },
    {
        "name": "StartEndless",
        "description": "무한 모드 게임을 시작합니다",
        "keywords": ["게임 시작", "시작해줘", "시작"]
    },
    {
        "name": "GoBack",
        "description": "이전 화면으로 돌아갑니다",
        "keywords": ["뒤로", "뒤로가기", "이전", "취소", "back"]
    },
    {
        "name": "GoToMainMenu",
        "description": "메인 메뉴 화면으로 돌아갑니다",
        "keywords": ["메인 메뉴", "메인으로", "main menu"]
    },
    {
        "name": "GoToGameModeSelection",
        "description": "게임 모드 선택 화면으로 돌아갑니다",
        "keywords": ["게임 모드 선택", "모드 선택", "game mode"]
    },
    {
        "name": "OpenStore",
        "description": "상점을 엽니다",
        "keywords": ["상점", "스토어", "store", "shop"]
    },
    {
        "name": "SelectTutorial",
        "description": "튜토리얼을 시작합니다",
        "keywords": ["튜토리얼", "챕터 0", "챕터 영", "tutorial"]
    },
    {
        "name": "SelectChapter1",
        "description": "챕터 1을 선택합니다",
        "keywords": ["챕터 1", "1챕터", "챕터 일", "chapter 1"]
    },
    {
        "name": "SelectChapter2",
        "description": "챕터 2를 선택합니다",
        "keywords": ["챕터 2", "2챕터", "챕터 이", "chapter 2"]
    },
    {
        "name": "SelectChapter3",
        "description": "챕터 3을 선택합니다",
        "keywords": ["챕터 3", "3챕터", "챕터 삼", "chapter 3"]
    },
    {
        "name": "SelectChapter4",
        "description": "챕터 4를 선택합니다",
        "keywords": ["챕터 4", "4챕터", "챕터 사", "chapter 4"]
    },
    {
        "name": "SelectChapter5",
        "description": "챕터 5를 선택합니다",
        "keywords": ["챕터 5", "5챕터", "챕터 오", "chapter 5"]
    },
    {
        "name": "SelectChapter6",
        "description": "챕터 6을 선택합니다",
        "keywords": ["챕터 6", "6챕터", "챕터 육", "chapter 6"]
    },
    {
        "name": "SelectChapter7",
        "description": "챕터 7을 선택합니다",
        "keywords": ["챕터 7", "7챕터", "챕터 칠", "chapter 7"]
    },
    {
        "name": "SelectChapter8",
        "description": "챕터 8을 선택합니다",
        "keywords": ["챕터 8", "8챕터", "챕터 팔", "chapter 8"]
    },
    {
        "name": "SelectChapter9",
        "description": "챕터 9를 선택합니다",
        "keywords": ["챕터 9", "9챕터", "챕터 구", "chapter 9"]
    },
    {
        "name": "SelectChapter10",
        "description": "챕터 10을 선택합니다",
        "keywords": ["챕터 10", "10챕터", "챕터 십", "chapter 10"]
    },
    {
        "name": "SelectChapter11",
        "description": "챕터 11을 선택합니다",
        "keywords": ["챕터 11", "11챕터", "chapter 11"]
    },
    {
        "name": "SelectChapter12",
        "description": "챕터 12를 선택합니다",
        "keywords": ["챕터 12", "12챕터", "chapter 12"]
    },
]


def transcribe_audio(audio_path: str, language: str = "ko") -> str:
    """로컬 Whisper 모델로 음성 인식"""
    try:
        result = whisper_model.transcribe(
            audio_path,
            language=language,
            fp16=False  # CPU에서는 False 권장
        )
        return result["text"].strip()
    except Exception as e:
        print(f"Whisper 로컬 오류: {e}")
        raise e


def classify_intent(text: str) -> dict:
    """키워드 기반 의도 분류"""
    text_lower = text.lower()

    best_match = None
    best_score = 0.0

    for func in AVAILABLE_FUNCTIONS:
        for keyword in func["keywords"]:
            if keyword.lower() in text_lower:
                # 더 긴 키워드가 매칭되면 더 높은 점수
                score = len(keyword) / len(text) if text else 0
                score = min(0.95, 0.5 + score)  # 0.5 ~ 0.95 범위

                if score > best_score:
                    best_score = score
                    best_match = func["name"]

    if best_match:
        return {"command": best_match, "confidence": best_score}

    return {"command": "Unknown", "confidence": 0.0}


def match_skill(text: str, skills: list) -> tuple:
    """키워드 기반 스킬 매칭"""
    text_lower = text.lower()
    candidates = []

    for skill in skills:
        skill_lower = skill.lower()

        # 정확히 일치
        if skill_lower == text_lower:
            candidates.append({"name": skill, "confidence": 0.95})
        # 스킬 이름이 텍스트에 포함
        elif skill_lower in text_lower:
            candidates.append({"name": skill, "confidence": 0.85})
        # 텍스트가 스킬 이름에 포함
        elif text_lower in skill_lower:
            candidates.append({"name": skill, "confidence": 0.75})
        # 부분 일치 (첫 글자 또는 마지막 글자)
        elif skill_lower.startswith(text_lower[:2]) or skill_lower.endswith(text_lower[-2:]):
            candidates.append({"name": skill, "confidence": 0.5})

    # 신뢰도 순으로 정렬
    candidates.sort(key=lambda x: x["confidence"], reverse=True)

    if candidates:
        return candidates[0]["name"], candidates[0]["confidence"], candidates[:5]

    return None, 0.0, []


@app.get("/")
async def root():
    """서버 상태 확인"""
    return {
        "status": "running",
        "message": "Voice Command Server (Offline) is running",
        "version": "offline",
        "whisper_model": MODEL_SIZE,
        "openai_available": False
    }


@app.get("/commands")
async def get_commands():
    """사용 가능한 명령어 목록 반환"""
    return {"commands": AVAILABLE_FUNCTIONS}


@app.post("/voice_command", response_model=CommandResponse)
async def process_voice_command(request: AudioRequest):
    """
    음성 명령 처리
    1. Base64 디코딩 → WAV 파일
    2. 로컬 Whisper로 음성 → 텍스트
    3. 키워드 기반 의도 파악
    4. 명령어 반환
    """
    try:
        # 1. Base64 디코딩
        audio_bytes = base64.b64decode(request.audioData)

        # 2. 임시 WAV 파일로 저장
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
            f.write(audio_bytes)
            temp_path = f.name

        # 3. 로컬 Whisper로 음성 인식
        print(f"Transcribing audio from: {temp_path}")
        transcribed_text = transcribe_audio(temp_path)
        print(f"Transcribed text: {transcribed_text}")

        # 4. 임시 파일 삭제
        os.unlink(temp_path)

        # 5. 텍스트가 비어있으면 Unknown
        if not transcribed_text:
            return CommandResponse(
                text="(인식된 음성 없음)",
                command="Unknown",
                confidence=0.0
            )

        # 6. 키워드 기반 의도 파악
        classification = classify_intent(transcribed_text)

        return CommandResponse(
            text=transcribed_text,
            command=classification["command"],
            confidence=classification["confidence"]
        )

    except Exception as e:
        print(f"Error processing voice command: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/transcribe")
async def transcribe_only(request: AudioRequest):
    """음성 인식만 수행 (명령 분류 없이)"""
    try:
        audio_bytes = base64.b64decode(request.audioData)

        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
            f.write(audio_bytes)
            temp_path = f.name

        transcribed_text = transcribe_audio(temp_path)
        os.unlink(temp_path)

        return {"text": transcribed_text}

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/recognize")
async def recognize_skill(
    audio: UploadFile = File(...),
    language: str = Form("ko"),
    skills: str = Form(""),
    context: str = Form(""),
    context_keywords: str = Form("")
):
    """
    기존 Unity VoiceServerClient와 호환되는 스킬 인식 엔드포인트
    시스템 명령(설정, 메뉴 등)과 스킬 모두 인식
    """
    start_time = time.time()

    try:
        # 1. 오디오 파일 저장
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
            content = await audio.read()
            f.write(content)
            temp_path = f.name

        print(f"[/recognize] Audio saved, Language: {language}, Context: {context}, Skills: {skills}")

        # 2. 로컬 Whisper로 음성 인식
        transcribed_text = transcribe_audio(temp_path, language)
        print(f"[/recognize] Transcribed: {transcribed_text}")

        # 3. 임시 파일 삭제
        os.unlink(temp_path)

        # 4. 먼저 시스템 명령인지 확인
        system_result = classify_intent(transcribed_text)
        print(f"[/recognize] System command check: {system_result}")

        # 시스템 명령이 감지되면 (Unknown이 아니고 신뢰도가 0.5 이상)
        if system_result["command"] != "Unknown" and system_result["confidence"] >= 0.5:
            processing_time = time.time() - start_time
            return {
                "success": True,
                "text": transcribed_text,
                "matched_skill": f"SYSTEM:{system_result['command']}",
                "confidence": system_result["confidence"],
                "candidates": [{"name": f"SYSTEM:{system_result['command']}", "confidence": system_result["confidence"]}],
                "processing_time": processing_time,
                "is_system_command": True
            }

        # 5. 시스템 명령이 아니면 스킬 매칭
        skill_list = [s.strip() for s in skills.split(",") if s.strip()]
        matched_skill, confidence, candidates = match_skill(transcribed_text, skill_list)

        processing_time = time.time() - start_time

        return {
            "success": True,
            "text": transcribed_text,
            "matched_skill": matched_skill,
            "confidence": confidence,
            "candidates": candidates,
            "processing_time": processing_time,
            "is_system_command": False
        }

    except Exception as e:
        print(f"[/recognize] Error: {e}")
        return {
            "success": False,
            "text": "",
            "matched_skill": None,
            "confidence": 0.0,
            "candidates": [],
            "processing_time": time.time() - start_time,
            "error": str(e)
        }


@app.get("/models")
async def get_models():
    """사용 가능한 Whisper 모델 목록"""
    models = {
        "tiny": {"description": "가장 빠름, 낮은 정확도", "size": "~39MB", "downloaded": True},
        "base": {"description": "빠름, 적당한 정확도", "size": "~74MB", "downloaded": True},
        "small": {"description": "균형", "size": "~244MB", "downloaded": False},
        "medium": {"description": "느림, 높은 정확도", "size": "~769MB", "downloaded": False},
        "large": {"description": "가장 느림, 최고 정확도", "size": "~1.5GB", "downloaded": False},
    }
    return {
        "status": "success",
        "current_model": MODEL_SIZE,
        "models": models
    }


if __name__ == "__main__":
    import uvicorn
    print(f"Starting Voice Command Server (Offline) on port 8000...")
    print(f"Using Whisper model: {MODEL_SIZE}")
    uvicorn.run(app, host="0.0.0.0", port=8000)
