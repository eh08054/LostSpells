@echo off
chcp 65001 > nul
cd /d "%~dp0"

echo ========================================
echo   Voice Command Server (Online)
echo   OpenAI Whisper API + GPT-4o-mini
echo ========================================

set PYTHON_PATH=C:\Users\qqpmzz\AppData\Local\Programs\Python\Python313\python.exe

echo Installing dependencies...
"%PYTHON_PATH%" -m pip install -r requirements.txt

echo.
echo Starting server on http://127.0.0.1:8000
echo Press Ctrl+C to stop
echo.

"%PYTHON_PATH%" server.py

pause
