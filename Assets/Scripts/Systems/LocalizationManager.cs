using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostSpells.Systems
{
    public enum Language
    {
        Korean,
        English
    }

    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager instance;
        public static LocalizationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("LocalizationManager");
                    instance = go.AddComponent<LocalizationManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public event Action OnLanguageChanged;

        private Language currentLanguage = Language.English;
        private Dictionary<string, Dictionary<Language, string>> localizedTexts;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 에디터에서 씬 언로드 경고를 방지
#if UNITY_EDITOR
            gameObject.hideFlags = HideFlags.HideAndDontSave;
#else
            gameObject.hideFlags = HideFlags.DontSave;
#endif

            // 씬 언로드 시 이벤트 정리
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            InitializeLocalizationData();
            LoadSavedLanguage();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // 씬이 언로드될 때 모든 이벤트 리스너 정리
            if (OnLanguageChanged != null)
            {
                // 모든 구독자 제거
                System.Delegate[] delegates = OnLanguageChanged.GetInvocationList();
                foreach (var d in delegates)
                {
                    OnLanguageChanged -= (Action)d;
                }
            }
        }

        void OnDestroy()
        {
            // SceneManager 이벤트 해제
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            // 도메인 재로드 전에 모든 이벤트 리스너 정리
            if (OnLanguageChanged != null)
            {
                System.Delegate[] delegates = OnLanguageChanged.GetInvocationList();
                foreach (var d in delegates)
                {
                    OnLanguageChanged -= (Action)d;
                }
            }

            // 인스턴스가 이 객체라면 null로 설정
            if (instance == this)
            {
                instance = null;
            }
        }

        void OnApplicationQuit()
        {
            // SceneManager 이벤트 해제
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            // 애플리케이션 종료 시 모든 리스너 정리
            if (OnLanguageChanged != null)
            {
                System.Delegate[] delegates = OnLanguageChanged.GetInvocationList();
                foreach (var d in delegates)
                {
                    OnLanguageChanged -= (Action)d;
                }
            }
        }

        private void InitializeLocalizationData()
        {
            localizedTexts = new Dictionary<string, Dictionary<Language, string>>();

            // Main Menu
            AddText("main_menu_title", "메인 메뉴", "Main Menu");
            AddText("main_menu_play", "플레이", "Play");
            AddText("main_menu_options", "설정", "Options");
            AddText("main_menu_store", "상점", "Store");
            AddText("main_menu_quit", "종료", "Quit");
            AddText("quit_popup_title", "종료 확인", "Quit Confirmation");
            AddText("quit_popup_message", "정말 종료하시겠습니까?", "Are you sure you want to quit?");
            AddText("quit_popup_quit", "종료", "Quit");
            AddText("quit_popup_cancel", "취소", "Cancel");

            // Game Mode Selection
            AddText("game_mode_selection_title", "게임 모드 선택", "Game Mode Selection");
            AddText("game_mode_story", "스토리 모드", "Story Mode");
            AddText("game_mode_chapter", "챕터 선택", "Chapter Select");
            AddText("game_mode_endless", "무한 모드", "Endless Mode");
            AddText("game_mode_back", "돌아가기", "Back");

            // Story Mode
            AddText("story_mode_title", "스토리 모드", "Story Mode");
            AddText("story_mode_back", "돌아가기", "Back");

            // Chapter Select
            AddText("chapter_select_title", "챕터 선택", "Chapter Select");
            AddText("chapter_select_back", "돌아가기", "Back");

            // Endless Mode
            AddText("endless_mode_title", "무한 모드", "Endless Mode");
            AddText("endless_mode_best_score", "최고 점수", "Best Score");
            AddText("endless_mode_start_game", "게임 시작", "Start Game");
            AddText("endless_mode_easy", "쉬움", "Easy");
            AddText("endless_mode_normal", "보통", "Normal");
            AddText("endless_mode_hard", "어려움", "Hard");
            AddText("endless_mode_back", "돌아가기", "Back");

            // Options
            AddText("options_title", "설정", "Options");
            AddText("options_audio", "오디오", "Audio");
            AddText("options_graphics", "그래픽", "Graphics");
            AddText("options_language", "언어", "Language");
            AddText("options_game", "게임", "Game");
            AddText("options_back", "돌아가기", "Back");

            // Options - Audio Panel
            AddText("options_audio_title", "오디오 설정", "Audio Settings");
            AddText("options_audio_reset", "초기화", "Reset");
            AddText("options_audio_microphone", "마이크 장치", "Microphone Device");

            // Options - Graphics Panel
            AddText("options_graphics_title", "그래픽 설정", "Graphics Settings");
            AddText("options_graphics_reset", "초기화", "Reset");

            // Options - Language Panel
            AddText("options_language_title", "언어 설정", "Language Settings");
            AddText("options_language_reset", "초기화", "Reset");
            AddText("options_language_ui", "UI 언어", "UI Language");
            AddText("options_language_korean", "한국어", "Korean");
            AddText("options_language_english", "영어", "English");

            // Options - Game Panel
            AddText("options_game_title", "게임 설정", "Game Settings");
            AddText("options_game_reset", "초기화", "Reset");
            AddText("options_game_keybindings", "키 바인딩", "Key Bindings");
            AddText("options_game_voice_recognition", "음성 인식", "Voice Recognition");

            // Options - Key Bindings
            AddText("options_keybinding_ingame", "게임 내 조작", "In-Game Controls");
            AddText("options_keybinding_move_left", "왼쪽 이동:", "Move Left:");
            AddText("options_keybinding_move_right", "오른쪽 이동:", "Move Right:");
            AddText("options_keybinding_jump", "점프:", "Jump:");
            AddText("options_keybinding_voice_record", "음성 녹음:", "Voice Recording:");
            AddText("options_keybinding_skill_panel", "스킬 패널:", "Skill Panel:");
            AddText("options_keybinding_store", "상점 조작", "Store Controls");

            // Options - Voice Recognition
            AddText("options_voice_server_status", "서버 상태:", "Server Status:");
            AddText("options_voice_server_not_checked", "확인 안됨", "Not checked");
            AddText("options_voice_server_check", "확인", "Check");
            AddText("options_voice_server_connected", "연결됨", "Connected");
            AddText("options_voice_server_disconnected", "연결 안됨", "Disconnected");
            AddText("options_voice_language", "음성 인식 언어", "Voice Recognition Language");
            AddText("options_voice_model", "음성 인식 모델", "Voice Recognition Model");
            AddText("options_voice_model_status", "모델 상태:", "Model Status:");
            AddText("options_voice_model_downloaded", "다운로드됨", "Downloaded");
            AddText("options_voice_model_not_downloaded", "다운로드 안됨", "Not downloaded");
            AddText("options_voice_model_downloading", "다운로드 중", "Downloading");
            AddText("options_voice_model_download", "다운로드", "Download");
            AddText("options_voice_model_delete", "삭제", "Delete");

            // Store
            AddText("store_title", "상점", "Store");
            AddText("store_diamond", "다이아몬드", "Diamonds");
            AddText("store_revive_stone", "부활석", "Revive Stones");
            AddText("store_back", "돌아가기", "Back");

            // In-Game
            AddText("ingame_pause", "일시정지", "Pause");
            AddText("ingame_resume", "계속하기", "Resume");
            AddText("ingame_restart", "재시작", "Restart");
            AddText("ingame_quit", "나가기", "Quit");
            AddText("ingame_hp", "체력", "HP");
            AddText("ingame_mana", "마나", "Mana");
            AddText("ingame_wave", "웨이브", "Wave");
            AddText("ingame_score", "점수", "Score");
            AddText("ingame_chapter", "챕터", "Chapter");

            // Skill Categories
            AddText("skill_category_all", "전체", "All");
            AddText("skill_category_attack", "공격", "Attack");
            AddText("skill_category_defense", "방어", "Defense");

            // Voice Recognition
            AddText("voice_recording", "녹음 중...", "Recording...");
            AddText("voice_processing", "처리 중...", "Processing...");
            AddText("voice_too_short", "너무 짧음!", "Too short!");
            AddText("voice_failed", "실패", "Failed");
        }

        private void AddText(string key, string korean, string english)
        {
            localizedTexts[key] = new Dictionary<Language, string>
            {
                { Language.Korean, korean },
                { Language.English, english }
            };
        }

        private void LoadSavedLanguage()
        {
            // Load from SaveManager if available
            if (Data.SaveManager.Instance != null)
            {
                var saveData = Data.SaveManager.Instance.GetCurrentSaveData();
                if (saveData != null)
                {
                    // Convert string to enum
                    if (saveData.uiLanguage == "Korean")
                        currentLanguage = Language.Korean;
                    else
                        currentLanguage = Language.English;
                    return;
                }
            }

            // Default to Korean (as per original default)
            currentLanguage = Language.Korean;
        }

        public Language CurrentLanguage
        {
            get { return currentLanguage; }
        }

        public void SetLanguage(Language language)
        {
            if (currentLanguage != language)
            {
                currentLanguage = language;

                // Save to SaveManager
                if (Data.SaveManager.Instance != null)
                {
                    var saveData = Data.SaveManager.Instance.GetCurrentSaveData();
                    if (saveData != null)
                    {
                        // Convert enum to string
                        saveData.uiLanguage = language == Language.Korean ? "Korean" : "English";
                        Data.SaveManager.Instance.SaveGame();
                    }
                }

                OnLanguageChanged?.Invoke();
            }
        }

        public string GetText(string key)
        {
            if (localizedTexts.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(currentLanguage, out var text))
                {
                    return text;
                }
            }

            Debug.LogWarning($"[LocalizationManager] Missing localization for key: {key}");
            return key;
        }
    }
}
