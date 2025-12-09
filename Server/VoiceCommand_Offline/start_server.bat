@echo off
chcp 65001 > nul
cd /d "%~dp0"

echo ========================================
echo   Voice Command Server (Offline)
echo   Local Whisper + Keyword Matching
echo ========================================

REM Python 찾기 (PATH에서 자동 검색)
where python >nul 2>&1
if %errorlevel% equ 0 (
    set PYTHON_PATH=python
    echo Python found in PATH
) else (
    where py >nul 2>&1
    if %errorlevel% equ 0 (
        set PYTHON_PATH=py
        echo Python Launcher found
    ) else (
        echo ERROR: Python not found in PATH
        echo Please install Python and add it to PATH
        pause
        exit /b 1
    )
)

REM Whisper 모델 크기 설정 (tiny, base, small, medium, large)
REM 기본값: base (빠르고 적당한 정확도)
set WHISPER_MODEL=base

echo.
echo Whisper Model: %WHISPER_MODEL%
echo.

echo Installing dependencies...
echo (첫 실행 시 모델 다운로드로 시간이 걸릴 수 있습니다)
echo.

%PYTHON_PATH% -m pip install -r requirements.txt

echo.
echo Starting server on http://127.0.0.1:8000
echo Press Ctrl+C to stop
echo.

%PYTHON_PATH% server.py

pause
