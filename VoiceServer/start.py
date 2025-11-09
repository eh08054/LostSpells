#!/usr/bin/env python3
"""
Voice Recognition Server Launcher (GUI)
크로스 플랫폼 GUI 실행기 - Windows, macOS, Linux 모두 지원
"""
import sys
import subprocess
from pathlib import Path

def get_python_executable():
    """가상환경의 Python 실행 파일 경로 반환"""
    base_path = Path(__file__).parent

    # Windows
    if sys.platform == "win32":
        venv_python = base_path / "venv" / "Scripts" / "python.exe"
    # Unix (macOS, Linux)
    else:
        venv_python = base_path / "venv" / "bin" / "python"

    return venv_python

def main():
    print("=" * 60)
    print("🎤 Voice Recognition Server - GUI Mode")
    print("=" * 60)
    print()

    base_path = Path(__file__).parent
    venv_python = get_python_executable()
    server_gui = base_path / "server_gui.py"

    # 가상환경 확인
    if not venv_python.exists():
        print("❌ ERROR: Virtual environment not found!")
        print(f"Expected: {venv_python}")
        print("\nPlease create virtual environment first:")
        print("  python -m venv venv")
        print("\nThen activate and install dependencies:")
        if sys.platform == "win32":
            print("  venv\\Scripts\\activate")
        else:
            print("  source venv/bin/activate")
        print("  pip install -r requirements.txt")
        print()
        input("Press Enter to exit...")
        return 1

    # server_gui.py 확인
    if not server_gui.exists():
        print("❌ ERROR: server_gui.py not found!")
        print(f"Expected: {server_gui}")
        print()
        input("Press Enter to exit...")
        return 1

    print(f"📂 Working directory: {base_path}")
    print(f"🐍 Python: {venv_python}")
    print(f"🖥️  GUI script: {server_gui}")
    print()
    print("Launching GUI...")
    print()

    try:
        # GUI 실행
        process = subprocess.Popen(
            [str(venv_python), str(server_gui)],
            cwd=str(base_path)
        )

        # GUI 프로세스 종료 대기
        process.wait()

        return process.returncode

    except KeyboardInterrupt:
        print("\n⏹️  GUI closed by user")
        return 0
    except Exception as e:
        print(f"\n❌ Error: {e}")
        input("Press Enter to exit...")
        return 1

if __name__ == "__main__":
    sys.exit(main())
