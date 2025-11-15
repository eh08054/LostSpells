"""
Lost Spells - Voice Recognition Server
음성 인식 REST API 서버

실행 방법:
    python app.py

API 엔드포인트:
    POST /api/recognize - 음성 파일을 받아서 텍스트로 변환
    GET /api/health - 서버 상태 확인
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
import os
import json
import logging
from datetime import datetime

app = Flask(__name__)
CORS(app)  # Unity에서 접근 가능하도록 CORS 허용

# 로깅 설정
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# 설정 로드
with open('config.json', 'r', encoding='utf-8') as f:
    config = json.load(f)

# 업로드 폴더 설정
UPLOAD_FOLDER = 'uploads'
os.makedirs(UPLOAD_FOLDER, exist_ok=True)


@app.route('/api/health', methods=['GET'])
def health_check():
    """서버 상태 확인"""
    return jsonify({
        'status': 'healthy',
        'timestamp': datetime.now().isoformat(),
        'version': '1.0.0'
    })


@app.route('/api/recognize', methods=['POST'])
def recognize_speech():
    """
    음성 인식 API

    Request:
        - audio: 오디오 파일 (WAV, MP3 등)
        - language: 언어 코드 (ko-KR, en-US 등)

    Response:
        - text: 인식된 텍스트
        - confidence: 신뢰도 (0.0 ~ 1.0)
        - skill_matched: 매칭된 스킬 이름 (있을 경우)
    """
    try:
        # 파일 체크
        if 'audio' not in request.files:
            return jsonify({'error': 'No audio file provided'}), 400

        audio_file = request.files['audio']
        language = request.form.get('language', config['recognition']['language'])

        # 파일 저장
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        filename = f"{timestamp}_{audio_file.filename}"
        filepath = os.path.join(UPLOAD_FOLDER, filename)
        audio_file.save(filepath)

        logger.info(f"음성 파일 수신: {filename}, 언어: {language}")

        # TODO: 실제 음성 인식 로직 구현
        # 현재는 더미 응답 반환
        recognized_text = recognize_audio(filepath, language)
        confidence = 0.95

        # 스킬 매칭
        matched_skill = match_skill(recognized_text)

        # 임시 파일 삭제 (옵션)
        # os.remove(filepath)

        response = {
            'text': recognized_text,
            'confidence': confidence,
            'skill_matched': matched_skill,
            'language': language,
            'timestamp': datetime.now().isoformat()
        }

        logger.info(f"인식 결과: {recognized_text} (신뢰도: {confidence})")

        return jsonify(response)

    except Exception as e:
        logger.error(f"음성 인식 오류: {str(e)}")
        return jsonify({'error': str(e)}), 500


def recognize_audio(filepath, language):
    """
    실제 음성 인식 함수

    TODO: 여기에 실제 음성 인식 모델 연동
    - Google Speech-to-Text API
    - OpenAI Whisper
    - 커스텀 모델 등
    """
    # 더미 구현 (테스트용)
    # 실제로는 음성 인식 라이브러리 사용

    import speech_recognition as sr

    recognizer = sr.Recognizer()

    try:
        with sr.AudioFile(filepath) as source:
            audio = recognizer.record(source)

        # Google Speech Recognition 사용 (무료, 테스트용)
        if language.startswith('ko'):
            text = recognizer.recognize_google(audio, language='ko-KR')
        else:
            text = recognizer.recognize_google(audio, language='en-US')

        return text

    except sr.UnknownValueError:
        return ""
    except sr.RequestError as e:
        logger.error(f"음성 인식 서비스 오류: {e}")
        return ""
    except Exception as e:
        logger.error(f"음성 파일 처리 오류: {e}")
        return ""


def match_skill(text):
    """
    인식된 텍스트에서 스킬 매칭

    Args:
        text: 인식된 텍스트

    Returns:
        매칭된 스킬 ID 또는 None
    """
    if not text:
        return None

    # 스킬 목록 로드 (Unity의 Skills.json과 동기화 필요)
    skill_keywords = {
        'fireball': ['파이어볼', 'fire ball', '불구슬', '화염구'],
        'ice_spear': ['아이스 스피어', 'ice spear', '얼음창', '빙창'],
        'lightning': ['라이트닝', 'lightning', '번개', '전격'],
        'heal': ['힐', 'heal', '치유', '회복'],
        'shield': ['쉴드', 'shield', '방패', '보호막'],
    }

    text_lower = text.lower()

    for skill_id, keywords in skill_keywords.items():
        for keyword in keywords:
            if keyword.lower() in text_lower:
                logger.info(f"스킬 매칭: {skill_id} (키워드: {keyword})")
                return skill_id

    return None


@app.route('/api/skills', methods=['GET'])
def get_skills():
    """등록된 스킬 목록 반환"""
    skill_keywords = {
        'fireball': ['파이어볼', 'fire ball'],
        'ice_spear': ['아이스 스피어', 'ice spear'],
        'lightning': ['라이트닝', 'lightning'],
        'heal': ['힐', 'heal'],
        'shield': ['쉴드', 'shield'],
    }

    return jsonify({
        'skills': skill_keywords,
        'count': len(skill_keywords)
    })


@app.route('/api/update_skills', methods=['POST'])
def update_skills():
    """
    스킬 키워드 업데이트
    Unity에서 Skills.json 변경 시 동기화용
    """
    try:
        data = request.get_json()
        # TODO: 스킬 키워드 데이터베이스 업데이트
        logger.info(f"스킬 데이터 업데이트: {len(data)} 개")
        return jsonify({'status': 'success'})
    except Exception as e:
        return jsonify({'error': str(e)}), 500


if __name__ == '__main__':
    host = config['server']['host']
    port = config['server']['port']

    logger.info(f"=== Lost Spells Voice Recognition Server ===")
    logger.info(f"서버 시작: http://{host}:{port}")
    logger.info(f"언어: {config['recognition']['language']}")
    logger.info(f"신뢰도 임계값: {config['recognition']['confidence_threshold']}")

    app.run(
        host=host,
        port=port,
        debug=True
    )
