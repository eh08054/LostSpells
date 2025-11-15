@echo off
echo ========================================
echo   Voice Recognition Server
echo ========================================
echo.

REM 가상환경이 있으면 사용, 없으면 시스템 Python 사용
if exist "venv\Scripts\python.exe" (
    echo [*] Using virtual environment Python
    venv\Scripts\python.exe main.py
) else (
    echo [*] Using system Python
    python main.py
)

pause
