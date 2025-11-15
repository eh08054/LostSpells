import tkinter as tk
from tkinter import ttk, messagebox
import subprocess
import threading
import os
import sys
from pathlib import Path
import requests
import time

class VoiceServerGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Voice Recognition Server Manager")
        self.root.geometry("700x500")
        self.root.resizable(False, False)

        # 서버 프로세스
        self.server_process = None
        self.is_running = False

        # 서버 정보
        self.server_url = "http://localhost:8000"
        self.current_model = "Unknown"

        # 모델 정보
        self.models_info = {}
        self.model_widgets = {}  # 각 모델의 위젯 저장

        self.setup_ui()

        # 초기 상태 확인
        self.check_server_status()

        # 주기적인 상태 확인 시작 (5초마다)
        self.start_periodic_status_check()

    def setup_ui(self):
        """UI 구성"""
        # 상단 프레임: 서버 제어
        control_frame = ttk.LabelFrame(self.root, text="Server Control", padding=10)
        control_frame.pack(fill=tk.X, padx=10, pady=10)

        # 상태 표시
        status_frame = ttk.Frame(control_frame)
        status_frame.pack(fill=tk.X, pady=5)

        ttk.Label(status_frame, text="Status:", font=("Arial", 10, "bold")).pack(side=tk.LEFT, padx=5)
        self.status_label = ttk.Label(status_frame, text="Stopped", foreground="red", font=("Arial", 10))
        self.status_label.pack(side=tk.LEFT, padx=5)

        ttk.Label(status_frame, text="Port:", font=("Arial", 10, "bold")).pack(side=tk.LEFT, padx=(20, 5))
        ttk.Label(status_frame, text="8000", font=("Arial", 10)).pack(side=tk.LEFT)

        ttk.Label(status_frame, text="Current Model:", font=("Arial", 10, "bold")).pack(side=tk.LEFT, padx=(20, 5))
        self.model_combobox = ttk.Combobox(status_frame,
                                           values=["tiny", "base", "small", "medium", "large-v3"],
                                           state="readonly",
                                           width=12,
                                           font=("Arial", 9))
        self.model_combobox.set("Unknown")
        self.model_combobox.pack(side=tk.LEFT)
        self.model_combobox.bind("<<ComboboxSelected>>", self.on_model_selected)

        # 버튼 프레임
        button_frame = ttk.Frame(control_frame)
        button_frame.pack(fill=tk.X, pady=5)

        self.start_button = ttk.Button(button_frame, text="Start Server", command=self.start_server, width=15)
        self.start_button.pack(side=tk.LEFT, padx=5)

        self.stop_button = ttk.Button(button_frame, text="Stop Server", command=self.stop_server, width=15, state=tk.DISABLED)
        self.stop_button.pack(side=tk.LEFT, padx=5)

        # 모델 관리 프레임
        models_frame = ttk.LabelFrame(self.root, text="Model Management", padding=10)
        models_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        # 모델 리스트 헤더
        header_frame = ttk.Frame(models_frame)
        header_frame.pack(fill=tk.X, pady=(0, 5))

        ttk.Label(header_frame, text="Model", font=("Arial", 10, "bold"), width=15).grid(row=0, column=0, padx=5)
        ttk.Label(header_frame, text="Size", font=("Arial", 10, "bold"), width=10).grid(row=0, column=1, padx=5)
        ttk.Label(header_frame, text="Status", font=("Arial", 10, "bold"), width=15).grid(row=0, column=2, padx=5)
        ttk.Label(header_frame, text="Actions", font=("Arial", 10, "bold"), width=25).grid(row=0, column=3, padx=5)

        # 스크롤 가능한 프레임
        canvas = tk.Canvas(models_frame, highlightthickness=0)
        scrollbar = ttk.Scrollbar(models_frame, orient="vertical", command=canvas.yview)
        self.scrollable_frame = ttk.Frame(canvas)

        self.scrollable_frame.bind(
            "<Configure>",
            lambda e: canvas.configure(scrollregion=canvas.bbox("all"))
        )

        canvas.create_window((0, 0), window=self.scrollable_frame, anchor="nw")
        canvas.configure(yscrollcommand=scrollbar.set)

        canvas.pack(side="left", fill="both", expand=True)
        scrollbar.pack(side="right", fill="y")

        # 모델 목록 초기화
        self.create_model_rows()

    def create_model_rows(self):
        """모델 행 생성"""
        models = {
            "tiny": {"name": "Tiny", "size": "~75MB"},
            "base": {"name": "Base", "size": "~145MB"},
            "small": {"name": "Small", "size": "~466MB"},
            "medium": {"name": "Medium", "size": "~1.5GB"},
            "large-v3": {"name": "Large-v3", "size": "~2.9GB"}
        }

        for idx, (model_id, model_info) in enumerate(models.items()):
            frame = ttk.Frame(self.scrollable_frame)
            frame.pack(fill=tk.X, pady=2)

            # 모델 이름
            name_label = ttk.Label(frame, text=model_info["name"], width=15)
            name_label.grid(row=0, column=0, padx=5, sticky="w")

            # 크기
            size_label = ttk.Label(frame, text=model_info["size"], width=10)
            size_label.grid(row=0, column=1, padx=5)

            # 상태
            status_label = ttk.Label(frame, text="Checking...", width=15, foreground="gray")
            status_label.grid(row=0, column=2, padx=5)

            # 액션 버튼
            action_frame = ttk.Frame(frame)
            action_frame.grid(row=0, column=3, padx=5)

            download_btn = ttk.Button(action_frame, text="Download", width=10,
                                     command=lambda m=model_id: self.download_model(m))
            download_btn.pack(side=tk.LEFT, padx=2)

            delete_btn = ttk.Button(action_frame, text="Delete", width=10,
                                   command=lambda m=model_id: self.delete_model(m))
            delete_btn.pack(side=tk.LEFT, padx=2)

            # 위젯 저장
            self.model_widgets[model_id] = {
                "status_label": status_label,
                "download_btn": download_btn,
                "delete_btn": delete_btn
            }

    def start_server(self):
        """서버 시작"""
        if self.is_running:
            messagebox.showwarning("Warning", "Server is already running")
            return

        try:
            # 실행 파일의 실제 위치 찾기
            if getattr(sys, 'frozen', False):
                # .exe로 실행 중인 경우
                base_path = Path(sys.executable).parent
            else:
                # .py로 실행 중인 경우
                base_path = Path(__file__).parent

            # 가상환경의 Python 경로
            venv_python = base_path / "venv" / "Scripts" / "python.exe"
            main_py = base_path / "main.py"

            if not venv_python.exists():
                messagebox.showerror("Error", f"Virtual environment not found:\n{venv_python}")
                return

            if not main_py.exists():
                messagebox.showerror("Error", f"main.py not found:\n{main_py}")
                return

            # 서버 프로세스 시작
            self.server_process = subprocess.Popen(
                [str(venv_python), str(main_py)],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                creationflags=subprocess.CREATE_NEW_PROCESS_GROUP
            )

            self.is_running = True
            self.update_ui_state()

            # 2초 후 상태 확인
            self.root.after(2000, self.check_server_status)

        except Exception as e:
            messagebox.showerror("Error", f"Failed to start server:\n{e}")
            self.is_running = False
            self.update_ui_state()

    def stop_server(self):
        """서버 중지"""
        if not self.is_running and self.server_process is None:
            messagebox.showwarning("Warning", "Server is not running")
            return

        try:
            # 프로세스 종료
            if self.server_process is not None:
                try:
                    # Windows에서 taskkill 사용
                    subprocess.run(['taskkill', '/F', '/T', '/PID', str(self.server_process.pid)],
                                   capture_output=True, timeout=5)
                except Exception:
                    # taskkill 실패 시 terminate 시도
                    self.server_process.terminate()
                    try:
                        self.server_process.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        self.server_process.kill()

            # 프로세스 정리
            self.server_process = None
            self.is_running = False

            # UI 상태 업데이트
            self.update_ui_state()
            self.status_label.config(text="Stopped", foreground="red")
            self.model_combobox.set("Unknown")

        except Exception as e:
            messagebox.showerror("Error", f"Error stopping server:\n{e}")
            # 에러가 발생해도 상태는 리셋
            self.server_process = None
            self.is_running = False
            self.update_ui_state()
            self.status_label.config(text="Stopped", foreground="red")
            self.model_combobox.set("Unknown")

    def check_server_status(self):
        """서버 상태 확인"""
        try:
            response = requests.get(f"{self.server_url}/models", timeout=2)
            if response.status_code == 200:
                data = response.json()
                self.current_model = data.get("current_model", "Unknown")
                self.model_combobox.set(self.current_model)
                self.status_label.config(text="Running", foreground="green")

                if not self.is_running:
                    self.is_running = True
                    self.update_ui_state()

                # 모델 정보 업데이트
                self.update_models_status()
            else:
                self.set_server_offline()
        except requests.exceptions.RequestException:
            self.set_server_offline()

            # GUI에서 시작한 서버가 응답하지 않으면 프로세스도 정리
            if self.server_process is not None:
                try:
                    poll_result = self.server_process.poll()
                    if poll_result is not None:
                        self.server_process = None
                except:
                    pass

    def set_server_offline(self):
        """서버 오프라인 상태로 설정"""
        self.status_label.config(text="Stopped", foreground="red")
        self.model_combobox.set("Unknown")
        if self.is_running:
            self.is_running = False
            self.update_ui_state()

        # 모든 모델 상태를 "Server offline"으로 설정
        for model_id, widgets in self.model_widgets.items():
            widgets["status_label"].config(text="Server offline", foreground="gray")
            widgets["download_btn"].config(state=tk.DISABLED)
            widgets["delete_btn"].config(state=tk.DISABLED)

    def update_models_status(self):
        """모든 모델의 상태 업데이트"""
        for model_id in self.model_widgets.keys():
            threading.Thread(target=self.check_model_status, args=(model_id,), daemon=True).start()

    def check_model_status(self, model_id):
        """특정 모델의 상태 확인"""
        try:
            response = requests.get(f"{self.server_url}/models/{model_id}/status", timeout=2)
            if response.status_code == 200:
                data = response.json()
                downloaded = data.get("downloaded", False)
                status = data.get("status", "unknown")

                # UI 업데이트는 메인 스레드에서
                self.root.after(0, self.update_model_ui, model_id, downloaded, status)
        except:
            pass

    def update_model_ui(self, model_id, downloaded, status):
        """모델 UI 업데이트 (메인 스레드에서 실행)"""
        widgets = self.model_widgets.get(model_id)
        if not widgets:
            return

        if status == "downloading":
            widgets["status_label"].config(text="Downloading...", foreground="blue")
            widgets["download_btn"].config(state=tk.DISABLED)
            widgets["delete_btn"].config(state=tk.DISABLED)
        elif downloaded:
            widgets["status_label"].config(text="Installed", foreground="green")
            widgets["download_btn"].config(state=tk.DISABLED)
            widgets["delete_btn"].config(state=tk.NORMAL)
        else:
            widgets["status_label"].config(text="Not Installed", foreground="orange")
            widgets["download_btn"].config(state=tk.NORMAL)
            widgets["delete_btn"].config(state=tk.DISABLED)

    def download_model(self, model_id):
        """모델 다운로드"""
        if not self.is_running:
            messagebox.showwarning("Warning", "Server is not running. Please start the server first.")
            return

        result = messagebox.askyesno("Confirm Download",
                                     f"Do you want to download model '{model_id}'?\nThis may take several minutes.")
        if not result:
            return

        # 다운로드 시작
        threading.Thread(target=self.download_model_thread, args=(model_id,), daemon=True).start()

    def download_model_thread(self, model_id):
        """모델 다운로드 스레드"""
        try:
            # UI 업데이트
            self.root.after(0, self.update_model_ui, model_id, False, "downloading")

            # 서버에 다운로드 요청
            response = requests.post(f"{self.server_url}/models/download",
                                    data={"model_size": model_id}, timeout=10)

            if response.status_code == 200:
                # 다운로드 진행 상태 폴링
                self.poll_download_status(model_id)
            else:
                self.root.after(0, messagebox.showerror, "Error", f"Failed to start download: {response.text}")
                self.root.after(0, self.check_model_status, model_id)
        except Exception as e:
            self.root.after(0, messagebox.showerror, "Error", f"Download error:\n{e}")
            self.root.after(0, self.check_model_status, model_id)

    def poll_download_status(self, model_id):
        """다운로드 상태 폴링"""
        try:
            while True:
                response = requests.get(f"{self.server_url}/models/{model_id}/status", timeout=2)
                if response.status_code == 200:
                    data = response.json()
                    status = data.get("status", "unknown")
                    progress = data.get("download_progress", 0)

                    if status == "downloading":
                        # 진행 중
                        self.root.after(0, lambda: self.model_widgets[model_id]["status_label"].config(
                            text=f"{progress}%", foreground="blue"))
                        time.sleep(1)
                    elif status == "downloaded":
                        # 완료
                        self.root.after(0, messagebox.showinfo, "Success", f"Model '{model_id}' downloaded successfully!")
                        self.root.after(0, self.check_model_status, model_id)
                        break
                    else:
                        # 완료 또는 에러
                        self.root.after(0, self.check_model_status, model_id)
                        break
                else:
                    break
        except:
            self.root.after(0, self.check_model_status, model_id)

    def delete_model(self, model_id):
        """모델 삭제"""
        if not self.is_running:
            messagebox.showwarning("Warning", "Server is not running. Please start the server first.")
            return

        result = messagebox.askyesno("Confirm Delete",
                                     f"Are you sure you want to delete model '{model_id}'?")
        if not result:
            return

        # 삭제 시작
        threading.Thread(target=self.delete_model_thread, args=(model_id,), daemon=True).start()

    def delete_model_thread(self, model_id):
        """모델 삭제 스레드"""
        try:
            response = requests.delete(f"{self.server_url}/models/{model_id}", timeout=10)

            if response.status_code == 200:
                self.root.after(0, messagebox.showinfo, "Success", f"Model '{model_id}' deleted successfully!")
                self.root.after(0, self.check_model_status, model_id)
            else:
                self.root.after(0, messagebox.showerror, "Error", f"Failed to delete model: {response.text}")
        except Exception as e:
            self.root.after(0, messagebox.showerror, "Error", f"Delete error:\n{e}")

    def on_model_selected(self, event):
        """모델 선택 시 서버에 모델 변경 요청"""
        if not self.is_running:
            messagebox.showwarning("Warning", "Server is not running. Please start the server first.")
            # 선택을 이전 모델로 되돌림
            self.model_combobox.set(self.current_model)
            return

        selected_model = self.model_combobox.get()
        if selected_model == "Unknown" or selected_model == self.current_model:
            return

        # 서버에 모델 변경 요청
        threading.Thread(target=self.change_model_thread, args=(selected_model,), daemon=True).start()

    def change_model_thread(self, model_size):
        """모델 변경 스레드"""
        try:
            response = requests.post(f"{self.server_url}/models/select",
                                    data={"model_size": model_size}, timeout=30)

            if response.status_code == 200:
                self.current_model = model_size
                self.root.after(0, self.model_combobox.set, model_size)
            else:
                self.root.after(0, messagebox.showerror, "Error", f"Failed to change model: {response.text}")
                self.root.after(0, self.model_combobox.set, self.current_model)
        except Exception as e:
            self.root.after(0, messagebox.showerror, "Error", f"Model change error:\n{e}")
            self.root.after(0, self.model_combobox.set, self.current_model)

    def start_periodic_status_check(self):
        """주기적으로 서버 상태 확인 (5초마다)"""
        self.check_server_status()
        # 5초 후 다시 호출
        self.root.after(5000, self.start_periodic_status_check)

    def update_ui_state(self):
        """UI 버튼 상태 업데이트"""
        if self.is_running:
            self.start_button.config(state=tk.DISABLED)
            self.stop_button.config(state=tk.NORMAL)
        else:
            self.start_button.config(state=tk.NORMAL)
            self.stop_button.config(state=tk.DISABLED)

    def on_closing(self):
        """창 닫기"""
        if self.is_running:
            if messagebox.askokcancel("Quit", "Server is running. Do you want to stop it and quit?"):
                self.stop_server()
                self.root.destroy()
        else:
            self.root.destroy()

if __name__ == "__main__":
    root = tk.Tk()
    app = VoiceServerGUI(root)
    root.protocol("WM_DELETE_WINDOW", app.on_closing)
    root.mainloop()
