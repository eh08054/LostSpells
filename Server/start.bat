@echo off
chcp 65001 >nul 2>&1
echo ============================================================
echo Lost Spells Voice Server (CPU)
echo ============================================================
echo.

REM FFmpeg 경로를 PATH에 추가
set PATH=%PATH%;C:\Users\eh080\Downloads\ffmpeg-8.0-full_build\ffmpeg-8.0-full_build\bin

echo Starting server...
echo.

"C:\Users\eh080\AppData\Local\Programs\Python\Python312\python.exe" main.py

pause
