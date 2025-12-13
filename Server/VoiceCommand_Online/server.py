"""
음성 명령 서버 (온라인 버전)
- OpenAI Whisper API: 음성 → 텍스트 변환
- OpenAI GPT: 의도 파악 및 명령 분류
- 인터넷 연결 및 OpenAI API 키 필요
"""

import os
import base64
import tempfile
import json
from fastapi import FastAPI, HTTPException, File, UploadFile, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from openai import OpenAI
from dotenv import load_dotenv
import time

# 환경 변수 로드
load_dotenv()

# OpenAI 클라이언트 생성
client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

app = FastAPI(title="Voice Command Server (Online)", version="1.0.0")

# CORS 설정
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

print("Voice Command Server (Online) initialized!")
print(f"OpenAI API Key: {'Set' if os.getenv('OPENAI_API_KEY') else 'Not Set'}")


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
        "examples": ["설정 열어", "옵션 열어줘", "세팅 보여줘", "설정창 열어", "옵션 화면"]
    },
    {
        "name": "CloseSettings",
        "description": "설정창을 닫고 인게임으로 돌아갑니다",
        "examples": ["설정 닫아", "옵션 닫아줘", "설정창 닫아", "인게임으로 돌아가", "게임으로 돌아가", "돌아가", "뒤로가기", "뒤로", "나가기"]
    },
    {
        "name": "OpenMenu",
        "description": "게임 메뉴를 엽니다",
        "examples": ["메뉴 열어", "메뉴 보여줘", "메뉴창 열어"]
    },
    {
        "name": "CloseMenu",
        "description": "게임 메뉴를 닫습니다",
        "examples": ["메뉴 닫아", "메뉴 닫아줘"]
    },
    {
        "name": "PauseGame",
        "description": "게임을 일시정지합니다",
        "examples": ["일시정지", "멈춰", "정지", "퍼즈", "게임 멈춰", "잠깐 멈춰"]
    },
    {
        "name": "ResumeGame",
        "description": "일시정지된 게임을 재개합니다 (설정창이 아닌 일시정지 상태에서)",
        "examples": ["계속", "재개", "게임 계속", "플레이", "일시정지 해제"]
    },
    {
        "name": "RestartGame",
        "description": "게임을 재시작합니다 (게임오버 시 재도전)",
        "examples": ["재시작", "다시 시작", "리스타트", "처음부터", "재도전", "다시 해볼래", "다시"]
    },
    {
        "name": "QuitToMainMenu",
        "description": "메인 메뉴로 나갑니다",
        "examples": ["메인 메뉴로", "나가기", "종료", "메인으로"]
    },
    {
        "name": "OpenInventory",
        "description": "인벤토리/가방을 엽니다",
        "examples": ["인벤토리 열어", "가방 열어", "아이템 보여줘", "소지품"]
    },
    {
        "name": "CloseInventory",
        "description": "인벤토리를 닫습니다",
        "examples": ["인벤토리 닫아", "가방 닫아"]
    },
    {
        "name": "OpenMap",
        "description": "지도를 엽니다",
        "examples": ["지도 열어", "맵 열어", "지도 보여줘", "위치 보여줘"]
    },
    {
        "name": "CloseMap",
        "description": "지도를 닫습니다",
        "examples": ["지도 닫아", "맵 닫아"]
    },
    {
        "name": "ShowHelp",
        "description": "도움말을 표시합니다",
        "examples": ["도움말", "도와줘", "뭐라고 해야해", "명령어 알려줘"]
    },
    {
        "name": "StartGame",
        "description": "게임 모드 선택 화면으로 이동합니다 (메인 메뉴에서)",
        "examples": ["게임 시작", "플레이", "시작", "게임 모드 선택", "게임하자", "게임 할래"]
    },
    {
        "name": "SelectStoryMode",
        "description": "스토리 모드를 선택합니다",
        "examples": ["스토리 모드", "스토리", "챕터 모드", "스토리 선택"]
    },
    {
        "name": "SelectEndlessMode",
        "description": "무한 모드를 선택합니다",
        "examples": ["무한 모드", "엔드리스", "엔드리스 모드", "무한 선택"]
    },
    {
        "name": "StartEndless",
        "description": "무한 모드 게임을 시작합니다 (무한 모드 화면에서)",
        "examples": ["게임 시작", "시작", "플레이", "시작해줘", "게임 시작해줘"]
    },
    {
        "name": "GoBack",
        "description": "이전 화면으로 돌아갑니다",
        "examples": ["뒤로", "뒤로가기", "이전", "취소"]
    },
    {
        "name": "GoToMainMenu",
        "description": "메인 메뉴 화면으로 돌아갑니다",
        "examples": ["메인 메뉴로", "메인 메뉴로 돌아가", "메인 메뉴로 가줘", "메인으로", "메인으로 돌아가"]
    },
    {
        "name": "GoToGameModeSelection",
        "description": "게임 모드 선택 화면으로 돌아갑니다",
        "examples": ["게임 모드 선택으로", "게임 모드로 돌아가", "모드 선택으로", "모드 선택 화면으로"]
    },
    {
        "name": "OpenStore",
        "description": "상점을 엽니다",
        "examples": ["상점", "상점 열어", "스토어", "아이템 사러 가자"]
    },
    {
        "name": "SelectTutorial",
        "description": "튜토리얼(챕터 0)을 시작합니다",
        "examples": ["튜토리얼", "튜토리얼 시작", "튜토리얼 시작해줘", "챕터 0", "챕터 0 시작", "챕터 0 시작해줘", "챕터 영", "0챕터", "영챕터"]
    },
    {
        "name": "SelectChapter1",
        "description": "챕터 1을 선택합니다",
        "examples": ["챕터 1", "첫번째 챕터", "1챕터", "챕터 일"]
    },
    {
        "name": "SelectChapter2",
        "description": "챕터 2를 선택합니다",
        "examples": ["챕터 2", "두번째 챕터", "2챕터", "챕터 이"]
    },
    {
        "name": "SelectChapter3",
        "description": "챕터 3을 선택합니다",
        "examples": ["챕터 3", "세번째 챕터", "3챕터", "챕터 삼"]
    },
    {
        "name": "SelectChapter4",
        "description": "챕터 4를 선택합니다",
        "examples": ["챕터 4", "네번째 챕터", "4챕터", "챕터 사"]
    },
    {
        "name": "SelectChapter5",
        "description": "챕터 5를 선택합니다",
        "examples": ["챕터 5", "다섯번째 챕터", "5챕터", "챕터 오"]
    },
    {
        "name": "SelectChapter6",
        "description": "챕터 6을 선택합니다",
        "examples": ["챕터 6", "여섯번째 챕터", "6챕터", "챕터 육"]
    },
    {
        "name": "SelectChapter7",
        "description": "챕터 7을 선택합니다",
        "examples": ["챕터 7", "일곱번째 챕터", "7챕터", "챕터 칠"]
    },
    {
        "name": "SelectChapter8",
        "description": "챕터 8을 선택합니다",
        "examples": ["챕터 8", "여덟번째 챕터", "8챕터", "챕터 팔"]
    },
    {
        "name": "SelectChapter9",
        "description": "챕터 9를 선택합니다",
        "examples": ["챕터 9", "아홉번째 챕터", "9챕터", "챕터 구"]
    },
    {
        "name": "SelectChapter10",
        "description": "챕터 10을 선택합니다",
        "examples": ["챕터 10", "열번째 챕터", "10챕터", "챕터 십"]
    },
    {
        "name": "SelectChapter11",
        "description": "챕터 11을 선택합니다",
        "examples": ["챕터 11", "열한번째 챕터", "11챕터"]
    },
    {
        "name": "SelectChapter12",
        "description": "챕터 12를 선택합니다",
        "examples": ["챕터 12", "열두번째 챕터", "12챕터"]
    },
    # Options 탭 전환
    {
        "name": "ShowAudioTab",
        "description": "설정 화면에서 오디오 탭을 엽니다",
        "examples": ["오디오 탭", "오디오", "소리 설정", "오디오 열어"]
    },
    {
        "name": "ShowGraphicsTab",
        "description": "설정 화면에서 그래픽 탭을 엽니다",
        "examples": ["그래픽 탭", "그래픽", "화면 설정", "그래픽 열어"]
    },
    {
        "name": "ShowLanguageTab",
        "description": "설정 화면에서 언어 탭을 엽니다",
        "examples": ["언어 탭", "언어", "언어 설정", "언어 열어"]
    },
    {
        "name": "ShowGameTab",
        "description": "설정 화면에서 게임 탭을 엽니다",
        "examples": ["게임 탭", "게임 설정", "게임 열어"]
    },
    # Options 섹션 명령
    {
        "name": "ExpandVoiceRecognition",
        "description": "게임 탭에서 음성인식 섹션을 펼칩니다",
        "examples": ["음성인식 펼쳐", "음성인식 열어", "음성인식 보여줘"]
    },
    {
        "name": "CollapseVoiceRecognition",
        "description": "게임 탭에서 음성인식 섹션을 접습니다",
        "examples": ["음성인식 접어", "음성인식 닫아", "음성인식 숨겨"]
    },
    {
        "name": "ExpandKeyBinding",
        "description": "게임 탭에서 키설정 섹션을 펼칩니다",
        "examples": ["키설정 펼쳐", "키설정 열어", "키바인딩 펼쳐", "키설정 보여줘"]
    },
    {
        "name": "CollapseKeyBinding",
        "description": "게임 탭에서 키설정 섹션을 접습니다",
        "examples": ["키설정 접어", "키설정 닫아", "키바인딩 접어", "키설정 숨겨"]
    },
    # InGame 이동 명령
    {
        "name": "MoveLeft",
        "description": "캐릭터를 왼쪽으로 이동합니다",
        "examples": ["왼쪽", "왼쪽 이동", "왼쪽으로", "왼쪽으로 가"]
    },
    {
        "name": "MoveRight",
        "description": "캐릭터를 오른쪽으로 이동합니다",
        "examples": ["오른쪽", "오른쪽 이동", "오른쪽으로", "오른쪽으로 가"]
    },
    {
        "name": "Jump",
        "description": "캐릭터가 점프합니다",
        "examples": ["점프", "뛰어", "점프해", "뛰어올라"]
    },
    {
        "name": "StopMove",
        "description": "캐릭터 이동을 멈춥니다",
        "examples": ["정지", "멈춰", "이동 멈춰", "그만"]
    },
    # InGame 방향 전환
    {
        "name": "TurnLeft",
        "description": "캐릭터가 왼쪽으로 방향을 전환합니다",
        "examples": ["좌측으로 돌아"]
    },
    {
        "name": "TurnRight",
        "description": "캐릭터가 오른쪽으로 방향을 전환합니다",
        "examples": ["우측으로 돌아"]
    }
]


def build_system_prompt() -> str:
    """LLM용 시스템 프롬프트 생성"""
    functions_desc = "\n".join([
        f"- {f['name']}: {f['description']} (예: {', '.join(f['examples'][:3])})"
        for f in AVAILABLE_FUNCTIONS
    ])

    return f"""당신은 게임 음성 명령 분류기입니다.
사용자의 음성 인식 결과를 보고 가장 적절한 명령어를 선택하세요.

사용 가능한 명령어:
{functions_desc}

규칙:
1. 반드시 위 명령어 이름 중 하나만 JSON 형식으로 반환하세요.
2. 매칭되는 것이 없으면 "Unknown"을 반환하세요.
3. 응답 형식: {{"command": "명령어이름", "confidence": 0.0~1.0}}
4. confidence는 얼마나 확신하는지를 나타냅니다 (0.0 = 불확실, 1.0 = 확실)
5. 한국어와 영어 모두 지원합니다.
6. 유사한 표현도 적절히 매핑하세요."""


def is_whisper_hallucination(text: str) -> bool:
    """Whisper 환각(hallucination) 감지

    환각 패턴:
    - 반복적인 텍스트
    - 유튜브/트위치 관련 문구
    - 음악/자막 관련 문구
    """
    if not text or len(text.strip()) < 2:
        return True

    text_lower = text.lower()

    # 유튜브/트위치 환각 패턴
    hallucination_phrases = [
        "구독", "좋아요", "알림", "시청", "감사합니다", "시청해 주셔서",
        "subscribe", "like", "comment", "thank you for watching",
        "tv", "채널", "영상", "편집", "자막",
        "music", "♪", "♫", "lyrics",
        "copyright", "all rights reserved",
        "subtitles", "captions"
    ]

    for phrase in hallucination_phrases:
        if phrase in text_lower:
            print(f"[Hallucination] 유튜브/미디어 관련 문구 감지: {text}")
            return True

    # 반복 패턴 감지 (같은 단어가 3번 이상 반복)
    words = text.replace(",", " ").replace(".", " ").split()
    if len(words) >= 3:
        from collections import Counter
        word_counts = Counter(words)
        most_common_word, count = word_counts.most_common(1)[0]
        if count >= 3 and count / len(words) > 0.4:
            print(f"[Hallucination] 반복 패턴 감지: '{most_common_word}'가 {count}번 반복 ({text})")
            return True

    # 쉼표로 시작하거나 이상한 패턴
    if text.startswith(",") or text.startswith("."):
        print(f"[Hallucination] 비정상 시작 문자: {text}")
        return True

    return False


async def transcribe_audio(audio_path: str, prompt: str = "") -> str:
    """OpenAI Whisper API로 음성 인식

    prompt: 예상되는 단어들을 제공하면 인식률이 향상됨
    """
    try:
        # 오디오 파일 크기 확인
        import os
        file_size = os.path.getsize(audio_path)
        print(f"[Whisper] 오디오 파일 크기: {file_size} bytes")

        with open(audio_path, "rb") as audio_file:
            transcript = client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                language="ko",
                prompt=prompt if prompt else None
            )
        result = transcript.text.strip()
        print(f"[Whisper] 원본 결과: '{result}' (길이: {len(result)})")

        # 환각 감지
        if is_whisper_hallucination(result):
            print(f"[Whisper] 환각으로 판단되어 무시: {result}")
            return ""

        return result
    except Exception as e:
        print(f"Whisper API 오류: {e}")
        raise e


async def classify_intent(text: str) -> dict:
    """LLM을 사용해 사용자 의도를 파악"""

    # 먼저 키워드 기반 분류 시도 (더 정확함)
    fallback_result = fallback_classify(text)
    if fallback_result["command"] != "Unknown":
        print(f"[classify_intent] Keyword match: {fallback_result}")
        return fallback_result

    # 키워드 매칭 실패 시 LLM 사용
    if not os.getenv("OPENAI_API_KEY"):
        return fallback_result

    try:
        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {"role": "system", "content": build_system_prompt()},
                {"role": "user", "content": f"음성 인식 결과: \"{text}\""}
            ],
            temperature=0,
            max_tokens=100
        )

        result_text = response.choices[0].message.content.strip()

        # JSON 파싱
        if "```" in result_text:
            result_text = result_text.split("```")[1]
            if result_text.startswith("json"):
                result_text = result_text[4:]

        result = json.loads(result_text)
        return {
            "command": result.get("command", "Unknown"),
            "confidence": result.get("confidence", 0.5)
        }

    except Exception as e:
        print(f"LLM 분류 오류: {e}")
        return fallback_result


def fallback_classify(text: str) -> dict:
    """키워드 기반 폴백 분류 (시스템 명령만 - 스킬은 별도 처리)"""
    text_lower = text.lower()

    # 긴 키워드를 먼저 매칭하도록 순서 조정!
    keyword_map = {
        # 메인 메뉴 관련 (긴 것 먼저)
        "메인 메뉴": "GoToMainMenu",
        "메인메뉴": "GoToMainMenu",
        "메인으로": "GoToMainMenu",
        # 게임 시작 관련 (긴 것 먼저)
        "게임 시작": "StartGame",
        "게임시작": "StartGame",
        # 스토리 모드 관련 (긴 것 먼저)
        "스토리 모드": "SelectStoryMode",
        "스토리모드": "SelectStoryMode",
        "스토리": "SelectStoryMode",
        # 무한 모드 관련 (긴 것 먼저)
        "무한 모드": "SelectEndlessMode",
        "무한모드": "SelectEndlessMode",
        "무한": "SelectEndlessMode",
        "엔드리스": "SelectEndlessMode",
        # 뒤로가기 관련 (긴 것 먼저)
        "뒤로가기": "GoBack",
        "뒤로": "GoBack",
        # 일반 명령어
        "설정": "OpenSettings",
        "옵션": "OpenSettings",
        "세팅": "OpenSettings",
        "메뉴": "OpenMenu",
        "일시정지": "PauseGame",
        "퍼즈": "PauseGame",
        "계속": "ResumeGame",
        "재개": "ResumeGame",
        "재시작": "RestartGame",
        "재도전": "RestartGame",
        "인벤토리": "OpenInventory",
        "가방": "OpenInventory",
        "지도": "OpenMap",
        "맵": "OpenMap",
        "도움말": "ShowHelp",
        "도와": "ShowHelp",
        "상점": "OpenStore",
        "스토어": "OpenStore",
        "플레이": "StartGame",
        "시작": "StartGame",
        "종료": "QuitGame",
        "끝내기": "QuitGame",
        "나가기": "QuitGame",
        "메인": "GoToMainMenu",
        "닫기": "CloseSettings",
        # Options 탭 전환 (구체적인 것 먼저)
        "오디오 탭": "ShowAudioTab",
        "오디오탭": "ShowAudioTab",
        "오디오": "ShowAudioTab",
        "그래픽 탭": "ShowGraphicsTab",
        "그래픽탭": "ShowGraphicsTab",
        "그래픽": "ShowGraphicsTab",
        "언어 탭": "ShowLanguageTab",
        "언어탭": "ShowLanguageTab",
        "언어": "ShowLanguageTab",
        "게임 탭": "ShowGameTab",
        "게임탭": "ShowGameTab",
        # Options 섹션 명령
        "음성인식 펼쳐": "ExpandVoiceRecognition",
        "음성인식 열어": "ExpandVoiceRecognition",
        "음성인식 접어": "CollapseVoiceRecognition",
        "음성인식 닫아": "CollapseVoiceRecognition",
        "키설정 펼쳐": "ExpandKeyBinding",
        "키설정 열어": "ExpandKeyBinding",
        "키바인딩 펼쳐": "ExpandKeyBinding",
        "키설정 접어": "CollapseKeyBinding",
        "키설정 닫아": "CollapseKeyBinding",
        "키바인딩 접어": "CollapseKeyBinding",
        # 튜토리얼 및 챕터 선택
        "튜토리얼": "SelectTutorial",
        "챕터 0": "SelectTutorial",
        # 챕터명 (7대 죄악)
        "교만": "SelectChapter1",
        "탐욕": "SelectChapter2",
        "색욕": "SelectChapter3",
        "질투": "SelectChapter4",
        "폭식": "SelectChapter5",
        "분노": "SelectChapter6",
        "나태": "SelectChapter7",
        # 챕터 번호 (숫자가 큰 것 먼저, 공백 있음/없음 모두)
        "챕터12": "SelectChapter12",
        "챕터 12": "SelectChapter12",
        "챕터11": "SelectChapter11",
        "챕터 11": "SelectChapter11",
        "챕터10": "SelectChapter10",
        "챕터 10": "SelectChapter10",
        "챕터9": "SelectChapter9",
        "챕터 9": "SelectChapter9",
        "챕터8": "SelectChapter8",
        "챕터 8": "SelectChapter8",
        "챕터7": "SelectChapter7",
        "챕터 7": "SelectChapter7",
        "챕터6": "SelectChapter6",
        "챕터 6": "SelectChapter6",
        "챕터5": "SelectChapter5",
        "챕터 5": "SelectChapter5",
        "챕터4": "SelectChapter4",
        "챕터 4": "SelectChapter4",
        "챕터3": "SelectChapter3",
        "챕터 3": "SelectChapter3",
        "챕터2": "SelectChapter2",
        "챕터 2": "SelectChapter2",
        "챕터1": "SelectChapter1",
        "챕터 1": "SelectChapter1",
        "챕터0": "SelectTutorial",
        # InGame 이동 명령 (긴 것 먼저)
        "왼쪽으로 이동": "MoveLeft",
        "왼쪽 이동": "MoveLeft",
        "왼쪽으로": "MoveLeft",
        "왼쪽": "MoveLeft",
        "오른쪽으로 이동": "MoveRight",
        "오른쪽 이동": "MoveRight",
        "오른쪽으로": "MoveRight",
        "오른쪽": "MoveRight",
        "점프": "Jump",
        "뛰어": "Jump",
        "정지": "StopMove",
        "멈춰": "StopMove",
        "그만": "StopMove",
        "좌측으로 돌아": "TurnLeft",
        "우측으로 돌아": "TurnRight",
    }

    for keyword, command in keyword_map.items():
        if keyword in text_lower:
            return {"command": command, "confidence": 0.7}

    return {"command": "Unknown", "confidence": 0.0}


@app.get("/")
async def root():
    """서버 상태 확인"""
    return {
        "status": "running",
        "message": "Voice Command Server (Online) is running",
        "version": "online",
        "openai_available": bool(os.getenv("OPENAI_API_KEY"))
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
    2. OpenAI Whisper API로 음성 → 텍스트
    3. LLM으로 의도 파악
    4. 명령어 반환
    """
    try:
        # 1. Base64 디코딩
        audio_bytes = base64.b64decode(request.audioData)

        # 2. 임시 WAV 파일로 저장
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
            f.write(audio_bytes)
            temp_path = f.name

        # 3. OpenAI Whisper API로 음성 인식
        print(f"Transcribing audio from: {temp_path}")
        transcribed_text = await transcribe_audio(temp_path)
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

        # 6. LLM으로 의도 파악
        classification = await classify_intent(transcribed_text)

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

        transcribed_text = await transcribe_audio(temp_path)
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
    context: 현재 게임 화면 상태 (예: Menu_MainMenu, InGame_Playing 등)
    context_keywords: 현재 화면에서 사용 가능한 시스템 명령 키워드
    """
    import time
    start_time = time.time()

    try:
        # 1. 오디오 파일 저장
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
            content = await audio.read()
            f.write(content)
            temp_path = f.name

        print(f"[/recognize] Audio saved, Language: {language}, Context: {context}, Skills: {skills}")

        # 2. Whisper API로 음성 인식
        # 컨텍스트별 키워드가 제공되면 해당 키워드 사용, 아니면 전체 키워드 사용 (하위 호환성)
        if context_keywords:
            system_keywords = context_keywords
            print(f"[/recognize] Using context-specific keywords: {system_keywords}")
        else:
            # 기존 전역 키워드 (하위 호환성 유지)
            system_keywords = "설정, 옵션, 메뉴, 일시정지, 멈춰, 계속, 재시작, 재도전, 인벤토리, 지도, 게임 시작, 플레이, 스토리 모드, 무한 모드, 엔드리스, 뒤로, 상점, 튜토리얼, 메인 메뉴, 챕터 0, 챕터 1, 챕터 2, 챕터 3, 챕터 4, 챕터 5, 챕터 6, 챕터 7, 챕터 8, 챕터 9, 챕터 10, 챕터 11, 챕터 12"
            print(f"[/recognize] Using global keywords (no context provided)")

        skill_prompt = f"게임 음성 명령입니다. 시스템 명령: {system_keywords}. 스킬: {skills}" if skills else f"게임 음성 명령입니다. 시스템 명령: {system_keywords}"
        transcribed_text = await transcribe_audio(temp_path, prompt=skill_prompt)
        print(f"[/recognize] Transcribed: {transcribed_text}")

        # 3. 임시 파일 삭제
        os.unlink(temp_path)

        # 3-1. 환각 등으로 텍스트가 비어있으면 실패 반환
        if not transcribed_text:
            processing_time = time.time() - start_time
            return {
                "success": False,
                "text": "",
                "matched_skill": None,
                "confidence": 0.0,
                "candidates": [],
                "processing_time": processing_time,
                "is_system_command": False,
                "error": "No speech detected or hallucination filtered"
            }

        # 스킬 목록 파싱 (먼저 정의해야 InGame_Playing 최적화에서 사용 가능)
        skill_list = [s.strip() for s in skills.split(",") if s.strip()] if skills else []

        # 4. 인게임 플레이 중에는 이동 명령만 빠르게 확인 후 스킬 매칭
        if context == "InGame_Playing" and skill_list:
            # 먼저 이동/점프/정지 명령인지 확인 (키워드 매칭)
            movement_result = fallback_classify(transcribed_text)
            if movement_result["command"] in ["MoveLeft", "MoveRight", "TurnRight", "TurnLeft", "Jump", "StopMove", "PauseGame", "OpenMenu"]:
                print(f"[/recognize] InGame_Playing: 이동/시스템 명령 감지: {movement_result}")
                processing_time = time.time() - start_time
                return {
                    "success": True,
                    "text": transcribed_text,
                    "matched_skill": f"SYSTEM:{movement_result['command']}",
                    "confidence": movement_result["confidence"],
                    "candidates": [{"name": f"SYSTEM:{movement_result['command']}", "confidence": movement_result["confidence"]}],
                    "processing_time": processing_time,
                    "is_system_command": True
                }

            # 이동 명령이 아니면 스킬 매칭 (GPT 분류 건너뛰기)
            print(f"[/recognize] InGame_Playing: 스킬 매칭만 수행")
            matched_skill, confidence, candidates = fallback_skill_match(transcribed_text, skill_list)
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

        # 4-1. 메뉴 컨텍스트에서는 키워드 기반 매칭 먼저 시도 (GPT 오분류 방지)
        if context and context.startswith("Menu_"):
            keyword_result = fallback_classify(transcribed_text)
            if keyword_result["command"] != "Unknown" and keyword_result["confidence"] >= 0.7:
                print(f"[/recognize] {context}: 키워드 매칭 성공: {keyword_result}")
                processing_time = time.time() - start_time
                return {
                    "success": True,
                    "text": transcribed_text,
                    "matched_skill": f"SYSTEM:{keyword_result['command']}",
                    "confidence": keyword_result["confidence"],
                    "candidates": [{"name": f"SYSTEM:{keyword_result['command']}", "confidence": keyword_result["confidence"]}],
                    "processing_time": processing_time,
                    "is_system_command": True
                }

        # 4-2. 키워드 매칭 실패 시 GPT 분류
        system_result = await classify_intent(transcribed_text)
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
        matched_skill, confidence, candidates = await match_skill_with_llm(
            transcribed_text, skill_list, language
        )

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
        import time
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


async def match_skill_with_llm(text: str, skills: list, language: str) -> tuple:
    """LLM을 사용해 텍스트와 가장 유사한 스킬 매칭"""

    if not skills:
        return None, 0.0, []

    if not os.getenv("OPENAI_API_KEY"):
        return fallback_skill_match(text, skills)

    try:
        skills_str = ", ".join(skills)

        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "system",
                    "content": f"""당신은 게임 음성 명령 매칭 시스템입니다.
사용자의 음성 인식 결과와 가장 유사한 스킬을 찾아주세요.

사용 가능한 스킬: {skills_str}

응답 형식 (JSON만):
{{"matched_skill": "스킬명 또는 null", "confidence": 0.0~1.0, "candidates": [{{"name": "스킬명", "confidence": 0.9}}]}}"""
                },
                {"role": "user", "content": f"음성: \"{text}\""}
            ],
            temperature=0,
            max_tokens=200
        )

        result_text = response.choices[0].message.content.strip()

        if "```" in result_text:
            result_text = result_text.split("```")[1]
            if result_text.startswith("json"):
                result_text = result_text[4:]

        result = json.loads(result_text)
        return result.get("matched_skill"), result.get("confidence", 0.0), result.get("candidates", [])

    except Exception as e:
        print(f"[match_skill_with_llm] Error: {e}")
        return fallback_skill_match(text, skills)


def fallback_skill_match(text: str, skills: list) -> tuple:
    """단순 키워드 매칭 폴백"""
    print(f"[fallback_skill_match] Text: '{text}', Skills: {skills}")
    text_lower = text.lower()
    candidates = []

    # 스킬 별칭 (공백 차이, 짧은 형태 등)
    skill_aliases = {
        # 매직 미사일
        "매직미사일": "매직 미사일",
        "미사일": "매직 미사일",
        "magic missile": "매직 미사일",
        # 매직 실드
        "매직실드": "매직 실드",
        "실드": "매직 실드",
        "magic shield": "매직 실드",
        # 토네이도
        "회오리": "토네이도",
        "tornado": "토네이도",
        # 슬래시
        "slash": "슬래시",
        # 익스플로전
        "폭발": "익스플로전",
        "explosion": "익스플로전",
        # 큐어 힐
        "큐어힐": "큐어 힐",
        "힐": "큐어 힐",
        "cure": "큐어 힐",
    }

    # 별칭으로 먼저 매칭 시도
    for alias, target_skill in skill_aliases.items():
        if alias in text_lower:
            # 대상 스킬이 활성 스킬 목록에 있는지 확인
            for skill in skills:
                if skill.lower() == target_skill.lower() or target_skill.lower() in skill.lower():
                    candidates.append({"name": skill, "confidence": 0.9})
                    break

    # 직접 매칭
    for skill in skills:
        if skill.lower() in text_lower or text_lower in skill.lower():
            # 이미 candidates에 있으면 건너뛰기
            if not any(c["name"] == skill for c in candidates):
                candidates.append({"name": skill, "confidence": 0.8})

    if candidates:
        # 가장 높은 confidence 순으로 정렬
        candidates.sort(key=lambda x: x["confidence"], reverse=True)
        print(f"[fallback_skill_match] Matched: {candidates[0]['name']} (confidence: {candidates[0]['confidence']})")
        return candidates[0]["name"], candidates[0]["confidence"], candidates

    print(f"[fallback_skill_match] No match found")
    return None, 0.0, []


if __name__ == "__main__":
    import uvicorn
    print("Starting Voice Command Server (Online) on port 8000...")
    uvicorn.run(app, host="0.0.0.0", port=8000)
