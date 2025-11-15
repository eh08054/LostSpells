@echo off
chcp 65001 >nul
echo ========================================
echo   Voice Recognition Server
echo ========================================
echo.

REM 가상환경 확인
if exist "venv\Scripts\python.exe" (
    echo [*] Using virtual environment Python
    echo [*] Starting server...
    echo.
    venv\Scripts\python.exe main.py
    goto :end
)

REM Python 3.13 경로 확인
if exist "C:\Users\qqpmzz\AppData\Local\Programs\Python\Python313\python.exe" (
    echo [*] Found Python 3.13
    echo [*] Starting server...
    echo.
    "C:\Users\qqpmzz\AppData\Local\Programs\Python\Python313\python.exe" main.py
    goto :end
)

REM 시스템 Python 시도
python --version >nul 2>&1
if %errorlevel% equ 0 (
    echo [*] Using system Python
    echo [*] Starting server...
    echo.
    python main.py
    goto :end
)

REM Python을 찾을 수 없음
echo [!] ERROR: Python not found!
echo.
echo Please install Python 3.9 or higher from:
echo https://www.python.org/downloads/
echo.
echo Or create a virtual environment:
echo   python -m venv venv
echo   venv\Scripts\activate
echo   pip install -r requirements.txt
echo.

:end
pause
