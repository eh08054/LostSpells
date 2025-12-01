@echo off
chcp 65001 >nul 2>&1
echo ============================================================
echo Lost Spells Voice Server (CPU)
echo ============================================================
echo.

REM FFmpeg 경로를 PATH에 추가
set PATH=%PATH%;C:\Users\qqpmzz\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.0.1-full_build\bin

echo Starting server...
echo.

"C:\Users\qqpmzz\AppData\Local\Programs\Python\Python313\python.exe" main.py

pause
