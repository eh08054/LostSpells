using UnityEngine;
using System;

namespace LostSpells.UI
{
    /// <summary>
    /// 옵션 데이터 관리 클래스
    /// PlayerPrefs를 사용하여 설정값을 저장/로드
    /// </summary>
    public static class OptionsData
    {
        // PlayerPrefs 키 상수
        private const string PREF_MASTER_VOLUME = "Options.MasterVolume";
        private const string PREF_BGM_VOLUME = "Options.BGMVolume";
        private const string PREF_SFX_VOLUME = "Options.SFXVolume";
        private const string PREF_VOICE_THRESHOLD = "Options.VoiceThreshold";

        private const string PREF_RESOLUTION = "Options.Resolution";
        private const string PREF_SCREEN_MODE = "Options.ScreenMode";
        private const string PREF_VSYNC = "Options.VSync";
        private const string PREF_BRIGHTNESS = "Options.Brightness";
        private const string PREF_CONTRAST = "Options.Contrast";

        private const string PREF_GAME_LANGUAGE = "Options.GameLanguage";
        private const string PREF_VOICE_LANGUAGE = "Options.VoiceLanguage";
        private const string PREF_SUBTITLE = "Options.Subtitle";

        private const string PREF_DIFFICULTY = "Options.Difficulty";
        private const string PREF_SCREEN_SHAKE = "Options.ScreenShake";
        private const string PREF_HIT_EFFECT = "Options.HitEffect";

        private const string PREF_MICROPHONE = "Options.Microphone";

        #region Audio Settings

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 80f);
            set
            {
                PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, Mathf.Clamp(value, 0f, 100f));
                OnAudioSettingsChanged?.Invoke();
            }
        }

        public static float BGMVolume
        {
            get => PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 80f);
            set
            {
                PlayerPrefs.SetFloat(PREF_BGM_VOLUME, Mathf.Clamp(value, 0f, 100f));
                OnAudioSettingsChanged?.Invoke();
            }
        }

        public static float SFXVolume
        {
            get => PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 80f);
            set
            {
                PlayerPrefs.SetFloat(PREF_SFX_VOLUME, Mathf.Clamp(value, 0f, 100f));
                OnAudioSettingsChanged?.Invoke();
            }
        }

        public static float VoiceThreshold
        {
            get => PlayerPrefs.GetFloat(PREF_VOICE_THRESHOLD, 70f);
            set
            {
                PlayerPrefs.SetFloat(PREF_VOICE_THRESHOLD, Mathf.Clamp(value, 0f, 100f));
                OnAudioSettingsChanged?.Invoke();
            }
        }

        public static string MicrophoneDevice
        {
            get => PlayerPrefs.GetString(PREF_MICROPHONE, "");
            set
            {
                PlayerPrefs.SetString(PREF_MICROPHONE, value);
                OnAudioSettingsChanged?.Invoke();
            }
        }

        #endregion

        #region Graphics Settings

        public static int ResolutionIndex
        {
            get => PlayerPrefs.GetInt(PREF_RESOLUTION, 0);
            set
            {
                PlayerPrefs.SetInt(PREF_RESOLUTION, value);
                OnGraphicsSettingsChanged?.Invoke();
            }
        }

        public static ScreenMode ScreenMode
        {
            get => (ScreenMode)PlayerPrefs.GetInt(PREF_SCREEN_MODE, 0);
            set
            {
                PlayerPrefs.SetInt(PREF_SCREEN_MODE, (int)value);
                OnGraphicsSettingsChanged?.Invoke();
            }
        }

        public static bool VSync
        {
            get => PlayerPrefs.GetInt(PREF_VSYNC, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_VSYNC, value ? 1 : 0);
                QualitySettings.vSyncCount = value ? 1 : 0;
                OnGraphicsSettingsChanged?.Invoke();
            }
        }

        public static float Brightness
        {
            get => PlayerPrefs.GetFloat(PREF_BRIGHTNESS, 50f);
            set
            {
                PlayerPrefs.SetFloat(PREF_BRIGHTNESS, Mathf.Clamp(value, 0f, 100f));
                OnGraphicsSettingsChanged?.Invoke();
            }
        }

        public static float Contrast
        {
            get => PlayerPrefs.GetFloat(PREF_CONTRAST, 50f);
            set
            {
                PlayerPrefs.SetFloat(PREF_CONTRAST, Mathf.Clamp(value, 0f, 100f));
                OnGraphicsSettingsChanged?.Invoke();
            }
        }

        #endregion

        #region Language Settings

        public static Language GameLanguage
        {
            get => (Language)PlayerPrefs.GetInt(PREF_GAME_LANGUAGE, 0);
            set
            {
                PlayerPrefs.SetInt(PREF_GAME_LANGUAGE, (int)value);
                OnLanguageSettingsChanged?.Invoke();
            }
        }

        public static Language VoiceLanguage
        {
            get => (Language)PlayerPrefs.GetInt(PREF_VOICE_LANGUAGE, 0);
            set
            {
                PlayerPrefs.SetInt(PREF_VOICE_LANGUAGE, (int)value);
                OnLanguageSettingsChanged?.Invoke();
            }
        }

        public static bool Subtitle
        {
            get => PlayerPrefs.GetInt(PREF_SUBTITLE, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_SUBTITLE, value ? 1 : 0);
                OnLanguageSettingsChanged?.Invoke();
            }
        }

        #endregion

        #region Game Settings

        public static Difficulty Difficulty
        {
            get => (Difficulty)PlayerPrefs.GetInt(PREF_DIFFICULTY, 1);
            set
            {
                PlayerPrefs.SetInt(PREF_DIFFICULTY, (int)value);
                OnGameSettingsChanged?.Invoke();
            }
        }

        public static bool ScreenShake
        {
            get => PlayerPrefs.GetInt(PREF_SCREEN_SHAKE, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_SCREEN_SHAKE, value ? 1 : 0);
                OnGameSettingsChanged?.Invoke();
            }
        }

        public static bool HitEffect
        {
            get => PlayerPrefs.GetInt(PREF_HIT_EFFECT, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_HIT_EFFECT, value ? 1 : 0);
                OnGameSettingsChanged?.Invoke();
            }
        }

        #endregion

        #region Events

        public static event Action OnAudioSettingsChanged;
        public static event Action OnGraphicsSettingsChanged;
        public static event Action OnLanguageSettingsChanged;
        public static event Action OnGameSettingsChanged;

        #endregion

        #region Methods

        /// <summary>
        /// 모든 설정을 저장합니다
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 모든 설정을 기본값으로 초기화합니다
        /// </summary>
        public static void ResetToDefaults()
        {
            // Audio
            MasterVolume = 80f;
            BGMVolume = 80f;
            SFXVolume = 80f;
            VoiceThreshold = 70f;

            // Graphics
            ResolutionIndex = 0;
            ScreenMode = ScreenMode.Fullscreen;
            VSync = true;
            Brightness = 50f;
            Contrast = 50f;

            // Language
            GameLanguage = Language.English;
            VoiceLanguage = Language.English;
            Subtitle = true;

            // Game
            Difficulty = Difficulty.Normal;
            ScreenShake = true;
            HitEffect = true;

            Save();
        }

        /// <summary>
        /// 해상도를 실제로 적용합니다
        /// </summary>
        public static void ApplyResolution(int index)
        {
            var resolutions = new (int width, int height)[]
            {
                (1920, 1080),
                (1600, 900),
                (1366, 768),
                (1280, 720)
            };

            if (index >= 0 && index < resolutions.Length)
            {
                var res = resolutions[index];
                Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
            }
        }

        /// <summary>
        /// 화면 모드를 실제로 적용합니다
        /// </summary>
        public static void ApplyScreenMode(ScreenMode mode)
        {
            Screen.fullScreenMode = mode switch
            {
                ScreenMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
                ScreenMode.Windowed => FullScreenMode.Windowed,
                ScreenMode.BorderlessWindow => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.ExclusiveFullScreen
            };
        }

        /// <summary>
        /// 오디오 설정 변경 이벤트를 수동으로 트리거합니다
        /// </summary>
        public static void TriggerAudioSettingsChanged()
        {
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// 그래픽 설정 변경 이벤트를 수동으로 트리거합니다
        /// </summary>
        public static void TriggerGraphicsSettingsChanged()
        {
            OnGraphicsSettingsChanged?.Invoke();
        }

        /// <summary>
        /// 언어 설정 변경 이벤트를 수동으로 트리거합니다
        /// </summary>
        public static void TriggerLanguageSettingsChanged()
        {
            OnLanguageSettingsChanged?.Invoke();
        }

        /// <summary>
        /// 게임 설정 변경 이벤트를 수동으로 트리거합니다
        /// </summary>
        public static void TriggerGameSettingsChanged()
        {
            OnGameSettingsChanged?.Invoke();
        }

        #endregion
    }

    #region Enums

    public enum ScreenMode
    {
        Fullscreen = 0,
        Windowed = 1,
        BorderlessWindow = 2
    }

    public enum Language
    {
        Korean = 0,
        English = 1,
        Japanese = 2,
        Chinese = 3
    }

    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        VeryHard = 3
    }

    #endregion
}