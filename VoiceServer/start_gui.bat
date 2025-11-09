@echo off
cd /d "%~dp0"
echo Starting Voice Server GUI...
echo Current directory: %CD%
echo.

REM Check if venv exists
if not exist "venv\Scripts\python.exe" (
    echo ERROR: Virtual environment not found!
    echo Expected: %CD%\venv\Scripts\python.exe
    echo.
    pause
    exit /b 1
)

REM Check if server_gui.py exists
if not exist "server_gui.py" (
    echo ERROR: server_gui.py not found!
    echo Expected: %CD%\server_gui.py
    echo.
    pause
    exit /b 1
)

echo Launching GUI...
"venv\Scripts\python.exe" server_gui.py
if errorlevel 1 (
    echo.
    echo ERROR: Failed to start GUI
    pause
)
exit /b 0
