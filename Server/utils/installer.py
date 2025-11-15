"""
Dependency installer and checker
서버 실행 전 필요한 의존성 패키지 확인 및 설치
"""
import sys
import subprocess
import importlib.util
from typing import Tuple, List


class DependencyInstaller:
    """의존성 패키지 관리"""

    # 필수 패키지 목록
    REQUIRED_PACKAGES = {
        "fastapi": "fastapi",
        "uvicorn": "uvicorn",
        "python-multipart": "python-multipart",
        "faster-whisper": "faster-whisper",
        "python-Levenshtein": "Levenshtein",
        "pydantic": "pydantic"
    }

    @staticmethod
    def check_python_version() -> Tuple[bool, str]:
        """
        Python 버전 확인 (3.8 이상 필요)

        Returns:
            (성공 여부, 메시지)
        """
        version = sys.version_info
        if version.major < 3 or (version.major == 3 and version.minor < 8):
            return False, f"Python 3.8 or higher is required. Current version: {sys.version}"
        return True, f"Python version OK: {sys.version}"

    @staticmethod
    def is_package_installed(package_name: str, import_name: str) -> bool:
        """
        패키지가 설치되어 있는지 확인

        Args:
            package_name: pip 패키지명 (예: "python-Levenshtein")
            import_name: import에 사용되는 이름 (예: "Levenshtein")

        Returns:
            설치 여부
        """
        spec = importlib.util.find_spec(import_name)
        return spec is not None

    @classmethod
    def check_dependencies(cls) -> Tuple[bool, List[str]]:
        """
        모든 필수 패키지가 설치되어 있는지 확인

        Returns:
            (모두 설치됨 여부, 누락된 패키지 리스트)
        """
        missing_packages = []

        for package_name, import_name in cls.REQUIRED_PACKAGES.items():
            if not cls.is_package_installed(package_name, import_name):
                missing_packages.append(package_name)

        return len(missing_packages) == 0, missing_packages

    @staticmethod
    def install_package(package_name: str) -> Tuple[bool, str]:
        """
        패키지 설치

        Args:
            package_name: 설치할 패키지명

        Returns:
            (성공 여부, 메시지)
        """
        try:
            print(f"Installing {package_name}...")
            subprocess.check_call(
                [sys.executable, "-m", "pip", "install", package_name],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE
            )
            return True, f"Successfully installed {package_name}"
        except subprocess.CalledProcessError as e:
            return False, f"Failed to install {package_name}: {str(e)}"

    @classmethod
    def install_all_dependencies(cls) -> Tuple[bool, List[str]]:
        """
        모든 누락된 패키지 설치

        Returns:
            (성공 여부, 설치 결과 메시지 리스트)
        """
        all_installed, missing_packages = cls.check_dependencies()

        if all_installed:
            return True, ["All dependencies are already installed"]

        results = []
        failed = False

        for package_name in missing_packages:
            success, message = cls.install_package(package_name)
            results.append(message)

            if not success:
                failed = True

        return not failed, results

    @classmethod
    def install_from_requirements(cls, requirements_file: str = "requirements.txt") -> Tuple[bool, str]:
        """
        requirements.txt 파일에서 패키지 설치

        Args:
            requirements_file: requirements.txt 파일 경로

        Returns:
            (성공 여부, 메시지)
        """
        try:
            print(f"Installing packages from {requirements_file}...")
            subprocess.check_call(
                [sys.executable, "-m", "pip", "install", "-r", requirements_file],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE
            )
            return True, f"Successfully installed packages from {requirements_file}"
        except subprocess.CalledProcessError as e:
            return False, f"Failed to install from {requirements_file}: {str(e)}"
        except FileNotFoundError:
            return False, f"File not found: {requirements_file}"

    @classmethod
    def verify_installation(cls) -> Tuple[bool, str]:
        """
        전체 설치 검증

        Returns:
            (성공 여부, 상태 메시지)
        """
        # Python 버전 확인
        version_ok, version_msg = cls.check_python_version()
        if not version_ok:
            return False, version_msg

        # 패키지 확인
        all_installed, missing = cls.check_dependencies()

        if all_installed:
            return True, "All dependencies are installed and ready"
        else:
            return False, f"Missing packages: {', '.join(missing)}"

    @classmethod
    def setup(cls, auto_install: bool = True) -> bool:
        """
        초기 설정 - 의존성 확인 및 설치

        Args:
            auto_install: 누락된 패키지 자동 설치 여부

        Returns:
            설정 성공 여부
        """
        print("=" * 50)
        print("Voice Recognition Server Setup")
        print("=" * 50)

        # Python 버전 확인
        version_ok, version_msg = cls.check_python_version()
        print(f"\n[Python Version Check]")
        print(version_msg)

        if not version_ok:
            return False

        # 의존성 확인
        print(f"\n[Dependency Check]")
        all_installed, missing = cls.check_dependencies()

        if all_installed:
            print("✓ All dependencies are installed")
            return True

        print(f"✗ Missing packages: {', '.join(missing)}")

        if not auto_install:
            print("\nPlease install missing packages manually:")
            print(f"  pip install {' '.join(missing)}")
            return False

        # 자동 설치
        print(f"\n[Installing Dependencies]")
        success, results = cls.install_all_dependencies()

        for result in results:
            print(f"  {result}")

        if success:
            print("\n✓ Setup completed successfully!")
            return True
        else:
            print("\n✗ Setup failed. Please install dependencies manually.")
            return False


# 직접 실행 시 설치 진행
if __name__ == "__main__":
    installer = DependencyInstaller()
    success = installer.setup(auto_install=True)
    sys.exit(0 if success else 1)
