using LostSpells.Components;
using LostSpells.Data;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 

namespace LostSpells.Systems
{
    /// <summary>
    /// 게임 컨텍스트 (현재 화면 상태)
    /// </summary>
    public enum GameContext
    {
        Unknown,
        Menu_MainMenu,
        Menu_GameModeSelection,
        Menu_StoryMode,
        Menu_EndlessMode,
        InGame_Playing,
        InGame_Paused,
        InGame_GameOver,
        Options,
        Store
    }

    /// <summary>
    /// 음성 인식 매니저 (전역 싱글톤)
    /// 스페이스바로 음성 인식 시작/중지
    /// 모든 씬에서 동작하며 씬 전환 시에도 유지됨
    /// </summary>
    public class VoiceRecognitionManager : MonoBehaviour
    {
        // 싱글톤 인스턴스
        private static VoiceRecognitionManager _instance;
        public static VoiceRecognitionManager Instance => _instance;

        [Header("Components")]
        [Tooltip("음성 녹음 컴포넌트")]
        public VoiceRecorder voiceRecorder;

        [Tooltip("서버 클라이언트")]
        public VoiceServerClient serverClient;

        [Tooltip("플레이어 컴포넌트 (상태 표시용)")]
        public PlayerComponent playerComponent;

        [Header("Settings")]
        [Tooltip("최소 녹음 시간. 이 시간 미만은 서버로 보내지 않음.")]
        public float minSpeechDuration = 1f;

        [Header("Constant Recording Settings")]
        [SerializeField] private int sampleRate = 44100;
        [SerializeField] private int preRollSeconds = 1; // 발화 전 추가할 시간
        [SerializeField] private float threshold = 0.02f; // 음성 감지 임계값
        [SerializeField] private float silenceTimeout = 1f; // 발화 종료 판정 시간

        private AudioClip audioClip;
        private string micDevice;

        private bool isRecording = false; // 현재 음성 발화 중인지 여부
        private float silenceTimer = 0f;
        private float recordingTimer = 0f;

        private int detectionStartIndex = 0; // 발화 시작 위치 (AudioClip 샘플 인덱스)
        private int sampleSize = 1024; // RMS 계산에 사용할 샘플 크기

        private bool isProcessing = false; // 서버 통신 중 상태 (중복 방지)

        private string originalPlayerName = "Wizard";
        private System.Collections.Generic.List<SkillData> activeSkills = new System.Collections.Generic.List<SkillData>();
        private UI.InGameUI inGameUI;
        private UI.OptionsUI optionUI;
        private UI. StoreUI storeUI;
        private UIDocument inGame_uiDocument;



        private void Awake()
        {
            // 중복 방지 로직 
            VoiceRecognitionManager[] managers = FindObjectsByType<VoiceRecognitionManager>(FindObjectsSortMode.None);
            if (managers.Length > 1)
            // 싱글톤 패턴: 이미 인스턴스가 있으면 제거
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지

            // 컴포넌트 자동 찾기 - Awake에서 초기화하여 다른 스크립트의 Start보다 먼저 실행되도록 함
            if (voiceRecorder == null)
            {
                voiceRecorder = gameObject.AddComponent<VoiceRecorder>();
            }

            if (serverClient == null)
            {
                serverClient = FindFirstObjectByType<VoiceServerClient>();
                if (serverClient == null)
                {
                    GameObject serverObj = new GameObject("VoiceServerClient");
                    serverObj.transform.SetParent(transform); // 자식으로 설정하여 함께 유지
                    serverClient = serverObj.AddComponent<VoiceServerClient>();
                }
            }

            // 씬 로드 이벤트 등록
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            // PlayerComponent 초기화 
            if (playerComponent == null)
            {
                playerComponent = FindFirstObjectByType<PlayerComponent>();
            }

            if (playerComponent != null)
            {
                originalPlayerName = playerComponent.GetPlayerName();
            }

            // InGameUI 찾기 
            inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();
            inGame_uiDocument = inGameUI.GetComponent<UIDocument>();

            // 언어 설정 로드 및 이벤트 구독 
            LoadLanguageSettings();
            LocalizationManager.Instance.OnLanguageChanged += OnUILanguageChanged;
            InitializeSkills();

            // --- ConstantRecording 로직 적용 시작 ---

            audioClip = Microphone.Start(micDevice, true, 60, sampleRate);

            while (Microphone.GetPosition(micDevice) <= 0) { }

            // --- ConstantRecording 로직 적용 끝 ---
        }

        /// <summary>
        /// 씬이 로드될 때마다 참조 갱신
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Additive 로드는 무시 (Options 등)
            if (mode == UnityEngine.SceneManagement.LoadSceneMode.Additive)
                return;

            RefreshSceneReferences();
        }

        /// <summary>
        /// 현재 씬의 참조 갱신
        /// </summary>
        private void RefreshSceneReferences()
        {
            playerComponent = FindFirstObjectByType<PlayerComponent>();
            if (playerComponent != null)
            {
                originalPlayerName = playerComponent.GetPlayerName();
            }

            // InGameUI 찾기 (없으면 null)
            inGameUI = FindFirstObjectByType<LostSpells.UI.InGameUI>();

            // 스킬 초기화
            InitializeSkills();
        }

        private void OnDestroy()
        {
            // 이벤트 해제 
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnUILanguageChanged;
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// UI 언어가 변경될 때 음성인식 언어도 자동 변경 
        /// </summary>
        private void OnUILanguageChanged()
        {
            var currentLanguage = LocalizationManager.Instance.CurrentLanguage;
            string languageCode = currentLanguage == Language.Korean ? "ko" : "en";

            ChangeLanguage(languageCode);

            var skills = DataManager.Instance.GetAllSkillData();
            if (skills != null && skills.Count > 0)
            {
                StartCoroutine(serverClient.SetSkills(skills));
            }

            if (SaveManager.Instance != null)
            {
                var saveData = SaveManager.Instance.GetCurrentSaveData();
                if (saveData != null)
                {
                    saveData.voiceRecognitionLanguage = languageCode;
                    SaveManager.Instance.SaveGame();
                }
            }
        }

        /// <summary>
        /// SaveManager에서 언어 설정 로드 
        /// </summary>
        private void LoadLanguageSettings()
        {
            if (SaveManager.Instance != null)
            {
                var saveData = SaveManager.Instance.GetCurrentSaveData();
                if (saveData != null && !string.IsNullOrEmpty(saveData.voiceRecognitionLanguage))
                {
                    string languageCode = saveData.voiceRecognitionLanguage;
                    serverClient.SetLanguage(languageCode);
                }
                else
                {
                    serverClient.SetLanguage("ko");
                }
            }
            else
            {
                serverClient.SetLanguage("ko");
            }
        }

        /// <summary>
        /// 활성화할 스킬 설정 (InGameUI에서 호출) (기존과 동일)
        /// </summary>
        public void SetActiveSkills(System.Collections.Generic.List<SkillData> skills)
        {
            activeSkills = skills;
            if (skills != null && skills.Count > 0)
            {
                if (serverClient == null)
                {
                    return;
                }
                StartCoroutine(serverClient.SetSkills(skills));
            }
        }

        private void Update()
        {
            // 서버 통신 중이거나 AudioClip이 없으면 로직 건너뛰기
            if (audioClip == null || isProcessing) return;

            int currentPos = Microphone.GetPosition(micDevice);

            // 현재 위치의 RMS 계산
            float rms = CalculateRMS(currentPos);

            if (!isRecording)
            {
                // rms가 threshold를 넘을 경우 발화 시작 판정. 
                if (rms > threshold)
                {
                    Debug.Log("Voice detected, starting recording.");
                    isRecording = true;
                    detectionStartIndex = currentPos;
                    silenceTimer = 0f;
                    recordingTimer = 0f; // 새로 시작 시 타이머 초기화

                    // UI 업데이트 (ConstantRecording의 UI 표시를 대체)
                    string recordingText = LocalizationManager.Instance.GetText("voice_recording");
                    UpdateVoiceRecognitionDisplay(recordingText);
                }
            }
            else
            {
                recordingTimer += Time.unscaledDeltaTime;

                // rms가 threshold / 2보다 낮을 경우 "silence" 판정 
                if (rms < threshold / 2)
                {
                    silenceTimer += Time.unscaledDeltaTime;
                    if (silenceTimer > silenceTimeout)
                    {
                        Debug.Log("Silence detected, stopping recording.");
                        Debug.Log("Total Recording Time: " + recordingTimer + " seconds");

                        // 녹음된 길이가 최소 시간 미만일 시 서버로 전달하지 않음.
                        if (recordingTimer < minSpeechDuration)
                        {
                            Debug.Log("Recording too short, discarding.");
                            isRecording = false;
                            silenceTimer = 0f;
                            recordingTimer = 0f;

                            // UI 업데이트
                            string tooShortText = LocalizationManager.Instance.GetText("voice_too_short");
                            UpdateVoiceRecognitionDisplay(tooShortText);
                            StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(1f));
                            return;
                        }
                        else
                        {
                            // 발화 종료 및 서버 전송
                            StopAndSend(currentPos);
                            silenceTimer = 0f;
                            recordingTimer = 0f;
                            isRecording = false;

                            // UI 업데이트
                            string processingText = LocalizationManager.Instance.GetText("voice_processing");
                            UpdateVoiceRecognitionDisplay(processingText);
                        }
                    }
                }
                else // 음성 지속
                {
                    silenceTimer = 0f; // 음성이 다시 감지되면 침묵 타이머 초기화
                }
            }
        }

        //현재 인덱스 앞 sampleSize만큼의 음성의 RMS를 계산함.
        private float CalculateRMS(int currentPos)
        {
            int startReadPos = currentPos - sampleSize;
            if (startReadPos < 0) startReadPos += audioClip.samples;

            float[] tempSamples = new float[sampleSize];
            if (startReadPos + sampleSize <= audioClip.samples)
            {
                audioClip.GetData(tempSamples, startReadPos);
            }
            else
            {
                int endCount = audioClip.samples - startReadPos;
                int remainingCount = sampleSize - endCount;
                if(endCount > 0)
                {
                    float[] part1 = new float[endCount];
                    audioClip.GetData(part1, startReadPos);
                    Array.Copy(part1, 0, tempSamples, 0, endCount);
                }
                if (remainingCount > 0)
                {
                    float[] part2 = new float[remainingCount];
                    audioClip.GetData(part2, 0);
                    Array.Copy(part2, 0, tempSamples, endCount, remainingCount);
                }
            }

            float sum = 0f;
            for (int i = 0; i < tempSamples.Length; i++)
            {
                sum += tempSamples[i] * tempSamples[i];
            }
            if (tempSamples.Length == 0) return 0f;
            return Mathf.Sqrt(sum / tempSamples.Length);
        }

        //발화된 시간 사이의 음성 + 발화 전 preRollSeconds를 클립에 합쳐서 처리 준비. (ConstantRecording 로직)
        private void StopAndSend(int currentPos)
        {
            int preRollSamples = preRollSeconds * sampleRate;
            int finalStartIndex = detectionStartIndex - preRollSamples;
            if (finalStartIndex < 0) finalStartIndex += audioClip.samples;

            int totalLength = 0;
            if (currentPos >= finalStartIndex)
            {
                totalLength = currentPos - finalStartIndex;
            }
            else
            {
                totalLength = (audioClip.samples - finalStartIndex) + currentPos;
            }

            float[] fullData = new float[totalLength];

            if (currentPos >= finalStartIndex)
            {
                audioClip.GetData(fullData, finalStartIndex);
            }
            else
            {
                float[] part1 = new float[audioClip.samples - finalStartIndex];
                float[] part2 = new float[currentPos];

                audioClip.GetData(part1, finalStartIndex);
                audioClip.GetData(part2, 0);

                Array.Copy(part1, 0, fullData, 0, part1.Length);
                Array.Copy(part2, 0, fullData, part1.Length, part2.Length);
            }

            // ConstantRecording과 달리, 여기서 바로 서버로 보내지 않고
            // VoiceRecorder를 사용하기 위해 AudioClip을 생성하고 코루틴을 호출합니다.
            AudioClip clipToSend = AudioClip.Create("UserVoice", totalLength, 1, sampleRate, false);
            clipToSend.SetData(fullData, 0);

            // isProcessing 플래그 설정
            isProcessing = true;

            // 녹음된 클립을 처리하고 서버로 전송
            StartCoroutine(SendAudioToServer(clipToSend));
        }

        /// <summary>
        /// 서버로 오디오 전송 
        /// </summary>
        private IEnumerator SendAudioToServer(AudioClip clipToSend)
        {
            // VoiceRecorder의 TrimSilence/GetRecordingAsBytes 로직을 사용하지 않음
            // ConstantRecording 로직에서 이미 필요한 부분만 추출함.

            string tempPath = Path.Combine(Application.persistentDataPath, "temp_constant_audio.wav");

            // VoiceRecorder.cs에 있는 SavWav 유틸리티를 사용하여 WAV 파일로 저장
            SavWav.Save(tempPath, clipToSend);

            // 저장된 파일을 바이트 배열로 읽기
            byte[] audioData = File.ReadAllBytes(tempPath);
            File.Delete(tempPath);

            if (audioData == null || audioData.Length == 0)
            {
                string failedText = LocalizationManager.Instance.GetText("voice_failed");
                UpdateVoiceRecognitionDisplay(failedText);
                isProcessing = false;
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(2f));
                yield break;
            }

            // 현재 컨텍스트와 키워드 가져오기
            string context = GetCurrentContextString();
            string contextKeywords = GetContextKeywords();

            // 서버로 전송 (컨텍스트 정보 포함)
            yield return serverClient.RecognizeSkill(audioData, context, contextKeywords, OnRecognitionResult);
        }

        /// <summary>
        /// 인식 결과 처리 
        /// </summary>
        private void OnRecognitionResult(RecognitionResult result)
        {
            isProcessing = false; // 처리 완료

            if (result == null)
            {
                // 실패: "서버 연결 실패" 표시
                UpdateVoiceRecognitionDisplay("서버 연결 실패");
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                return;
            }

            string recognizedText = result.recognized_text;
            var inGame_root = inGame_uiDocument.rootVisualElement;

            Debug.Log("선택된 액션: " + result.action);
            Debug.Log("선택된 명령: " + result.order);

            if (result.action.Equals("attack"))
            {
                Debug.Log("선택된 스킬: " + result.best_match.skill);
                // 스킬이 1개뿐이면 무조건 그 스킬 선택
                if (activeSkills != null && activeSkills.Count == 1)
                {
                    string onlySkill = activeSkills[0].voiceKeyword;
                    string skillDisplayName = GetSkillDisplayName(onlySkill);

                    UpdateVoiceRecognitionDisplay($"인식: {skillDisplayName} (100%)");
                    ExecuteSkill(onlySkill, result.direction, result.location);

                    if (inGameUI != null)
                    {
                        var accuracyScores = new System.Collections.Generic.Dictionary<string, float>();
                        accuracyScores[onlySkill] = 1.0f;
                        inGameUI.UpdateSkillAccuracy(accuracyScores);
                    }

                    StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                    return;
                }

                // 스킬 매칭 결과 처리
                if (result.best_match != null && !string.IsNullOrEmpty(result.best_match.skill))
                {
                    string skillDisplayName = GetSkillDisplayName(result.best_match.skill);
                    int scorePercent = Mathf.RoundToInt(result.best_match.score * 100);

                    UpdateVoiceRecognitionDisplay($"인식: {skillDisplayName} ({scorePercent}%)");

                    ExecuteSkill(result.best_match.skill, result.direction, result.location);

                    if (inGameUI != null)
                    {
                        var accuracyScores = result.skill_scores;
                        if (accuracyScores == null || accuracyScores.Count == 0)
                        {
                            accuracyScores = new System.Collections.Generic.Dictionary<string, float>();
                            accuracyScores[result.best_match.skill] = result.best_match.score;
                        }
                        inGameUI.UpdateSkillAccuracy(accuracyScores);
                    }
                }
            }
            else if(result.action.Equals("move"))
            {
                if (result.direction != "none" && result.location != 0)
                {
                    playerComponent.ExecuteMoveCommand(result.direction, result.location);
                }
            }
            else if(result.action.Equals("go_back"))
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.name == "Options")
                    {
                        optionUI = FindFirstObjectByType<LostSpells.UI.OptionsUI>();
                        optionUI.backOrdered();
                        break;
                    }
                    if (scene.name == "Store")
                    {
                        storeUI = FindFirstObjectByType<LostSpells.UI.StoreUI>();
                        storeUI.backOrdered();
                        break;
                    }
                }

            }
            else if(result.action.Equals("pause"))
            {
                Debug.Log("pause");
                inGameUI.MenuOrdered();
            }
            else if (result.action.Equals("resume") && (inGame_root.Q<VisualElement>("MenuPopup").style.display == DisplayStyle.Flex))
            {
                inGameUI.ResumeOrdered();
            }
            else if (result.action .Equals("settings"))
            {
                if (result.order.Equals("open_ui") && (inGame_root.Q<VisualElement>("MenuPopup").style.display == DisplayStyle.Flex))
                {
                    inGameUI.SettingsOrdered();
                }
                if (result.order.Equals("close_ui"))
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        Debug.Log(scene.name + " ");
                        if (scene.name == "Options")
                        {
                            optionUI = FindFirstObjectByType<LostSpells.UI.OptionsUI>();
                            optionUI.backOrdered();
                            break;
                        }
                    }
                }
            }
            else if (result.action.Equals("shop"))
            {
                if ((inGame_root.Q<VisualElement>("MenuPopup").style.display == DisplayStyle.Flex))
                {
                    inGameUI.ShopOredered();
                }
                if (result.order.Equals("close_ui"))
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        Debug.Log(scene.name + " ");
                        if (scene.name == "Store")
                        {
                            storeUI = FindFirstObjectByType<LostSpells.UI.StoreUI>();
                            storeUI.backOrdered();
                            break;
                        }
                    }
                }
            }
            else if (result.action.Equals("title") && (inGame_root.Q<VisualElement>("MenuPopup").style.display == DisplayStyle.Flex))
            {
                inGameUI.MainMenuOrdered();
            }
            else if (result.action.Equals("revival"))
            {
                inGameUI.RevivalOrdered();
            }
            else if (result.action.Equals("restart"))
            {
                inGameUI.RestartOrdered();
            }
            else
            {
                // 매칭 실패
                if (string.IsNullOrEmpty(recognizedText))
                {
                    UpdateVoiceRecognitionDisplay("인식 실패: 음성 없음");
                }
                else
                {
                    UpdateVoiceRecognitionDisplay($"인식 실패: {recognizedText}");
                }
            }
            StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
        }

        /// <summary>
        /// 스킬의 로컬라이즈된 이름 가져오기 
        /// </summary>
        private string GetSkillDisplayName(string voiceKeyword)
        {
            if (activeSkills != null && activeSkills.Count > 0)
            {
                foreach (var skill in activeSkills)
                {
                    if (skill.voiceKeyword == voiceKeyword)
                    {
                        return skill.GetLocalizedName();
                    }
                }
            }

            var allSkills = DataManager.Instance.GetAllSkillData();
            foreach (var skill in allSkills)
            {
                if (skill.voiceKeyword == voiceKeyword)
                {
                    return skill.GetLocalizedName();
                }
            }

            return voiceKeyword;
        }

        /// <summary>
        /// 일정 시간 후 음성인식 표시 지우기 
        /// </summary>
        private IEnumerator ClearVoiceRecognitionDisplayAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay); // 일시정지 중에도 동작
            UpdateVoiceRecognitionDisplay("");
        }

        /// <summary>
        /// 음성인식 결과 UI 업데이트 
        /// </summary>
        private void UpdateVoiceRecognitionDisplay(string text)
        {
            if (inGameUI != null)
            {
                inGameUI.UpdateVoiceRecognitionDisplay(text);
            }
        }

        /// <summary>
        /// 인식된 스킬 실행 
        /// </summary>
        private void ExecuteSkill(string skillName, string direction, int location)
        {
            // Debug.Log($"[VoiceRecognition] ExecuteSkill 호출됨: {skillName}");

            // 시스템 명령 처리 (SYSTEM:OpenSettings 형식)
            if (skillName.StartsWith("SYSTEM:"))
            {
                string systemCommand = skillName.Substring(7); // "SYSTEM:" 제거

                // SYSTEM:UseXXX 형태는 스킬로 처리 (서버에서 잘못 분류된 경우)
                if (systemCommand.StartsWith("Use"))
                {
                    string extractedSkillName = systemCommand.Substring(3); // "Use" 제거
                    // Debug.Log($"[VoiceRecognition] SYSTEM:Use 명령을 스킬로 변환: {extractedSkillName}");
                    ExecuteSkillByName(extractedSkillName);
                    return;
                }

                // Debug.Log($"[VoiceRecognition] 시스템 명령 추출됨: {systemCommand}");
                ExecuteSystemCommand(systemCommand);
                return;
            }

            ExecuteSkillByName(skillName);
        }

        /// <summary>
        /// 스킬 이름으로 스킬 실행
        /// </summary>
        private void ExecuteSkillByName(string skillName)
        {
            if (playerComponent == null)
            {
                Debug.LogError($"[VoiceRecognition] PlayerComponent가 null입니다!");
                return;
            }

            if (activeSkills == null || activeSkills.Count == 0)
            {
                Debug.LogError($"[VoiceRecognition] activeSkills가 비어있습니다!");
                return;
            }

            // 정확한 키워드 매칭
            foreach (var skill in activeSkills)
            {
                if (skill.voiceKeyword == skillName)
                {
                    playerComponent.CastSkill(skill, direction, location);
                    return;
                }
            }

            Debug.LogWarning($"[VoiceRecognition] 스킬을 찾을 수 없음: {skillName}");
        }

        /// <summary>
        /// 시스템 명령 실행
        /// </summary>
        private void ExecuteSystemCommand(string command)
        {
            // Debug.Log($"[VoiceRecognition] 시스템 명령 실행 시도: {command}");

            // 현재 게임 컨텍스트 확인
            GameContext currentContext = GetCurrentGameContext();
            // Debug.Log($"[VoiceRecognition] 현재 컨텍스트: {currentContext}");

            // 컨텍스트에서 허용되지 않는 명령인지 확인
            if (!IsCommandAllowedInContext(command, currentContext))
            {
                string hint = GetContextHintMessage(command, currentContext);
                Debug.LogWarning($"[VoiceRecognition] 현재 컨텍스트({currentContext})에서 '{command}' 명령 불가: {hint}");
                UpdateVoiceRecognitionDisplay(hint);
                StartCoroutine(ClearVoiceRecognitionDisplayAfterDelay(3f));
                return;
            }

            // Debug.Log($"[VoiceRecognition] 명령 실행: {command}");

            switch (command)
            {
                case "OpenSettings":
                    // 설정창 열기 - 내장 Options 팝업 표시
                    if (inGameUI != null && !inGameUI.IsOptionsPopupVisible())
                    {
                        Time.timeScale = 0f;
                        inGameUI.OpenOptionsPopup();
                    }
                    break;

                case "CloseSettings":
                    // 설정창 닫기 - 내장 Options 팝업 숨기기
                    if (inGameUI != null && inGameUI.IsOptionsPopupVisible())
                    {
                        inGameUI.CloseOptionsPopup();
                    }
                    break;

                case "OpenMenu":
                case "PauseGame":
                    // 일시정지 및 메뉴 팝업 열기
                    if (inGameUI != null)
                    {
                        // InGameUI의 메뉴 팝업 열기
                        var root = inGameUI.GetComponent<UIDocument>()?.rootVisualElement;
                        var menuPopup = root?.Q<VisualElement>("MenuPopup");
                        if (menuPopup != null)
                        {
                            menuPopup.style.display = DisplayStyle.Flex;
                            // 사이드바 숨기기
                            var leftSidebar = root.Q<VisualElement>("LeftSidebar");
                            var rightSidebar = root.Q<VisualElement>("RightSidebar");
                            if (leftSidebar != null) leftSidebar.style.display = DisplayStyle.None;
                            if (rightSidebar != null) rightSidebar.style.display = DisplayStyle.None;
                        }
                    }
                    Time.timeScale = 0f;
                    // Debug.Log("[VoiceRecognition] 게임 일시정지됨");
                    break;

                case "CloseMenu":
                case "ResumeGame":
                    // 재개 및 메뉴 팝업 닫기
                    if (inGameUI != null)
                    {
                        var root = inGameUI.GetComponent<UIDocument>()?.rootVisualElement;
                        var menuPopup = root?.Q<VisualElement>("MenuPopup");
                        if (menuPopup != null)
                        {
                            menuPopup.style.display = DisplayStyle.None;
                            // 사이드바 다시 표시
                            var leftSidebar = root.Q<VisualElement>("LeftSidebar");
                            var rightSidebar = root.Q<VisualElement>("RightSidebar");
                            if (leftSidebar != null) leftSidebar.style.display = DisplayStyle.Flex;
                            if (rightSidebar != null) rightSidebar.style.display = DisplayStyle.Flex;
                        }
                    }
                    Time.timeScale = 1f;
                    // Debug.Log("[VoiceRecognition] 게임 재개됨");
                    break;

                case "RestartGame":
                    // 게임 재시작
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    break;

                case "QuitToMainMenu":
                    // 메뉴로 이동
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                    break;

                case "OpenInventory":
                    // Debug.Log("[VoiceRecognition] 인벤토리 열기 (미구현)");
                    break;

                case "CloseInventory":
                    // Debug.Log("[VoiceRecognition] 인벤토리 닫기 (미구현)");
                    break;

                case "OpenMap":
                    // Debug.Log("[VoiceRecognition] 지도 열기 (미구현)");
                    break;

                case "CloseMap":
                    // Debug.Log("[VoiceRecognition] 지도 닫기 (미구현)");
                    break;

                case "ShowHelp":
                    // Debug.Log("[VoiceRecognition] 도움말 표시 (미구현)");
                    break;

                // 메뉴 네비게이션 명령어
                case "StartGame":
                    // 게임 모드 선택 화면으로 이동
                    NavigateMenuPanel("GameModeSelection");
                    break;

                case "SelectStoryMode":
                    // 스토리 모드 선택
                    NavigateMenuPanel("StoryMode");
                    break;

                case "SelectEndlessMode":
                    // 무한 모드 선택
                    NavigateMenuPanel("EndlessMode");
                    break;

                case "StartEndless":
                    // 무한 모드 게임 시작
                    if (GameStateManager.Instance != null)
                    {
                        GameStateManager.Instance.StartEndlessMode();
                    }
                    UnityEngine.SceneManagement.SceneManager.LoadScene("InGame");
                    break;

                case "GoBack":
                    // 이전 화면으로
                    NavigateMenuBack();
                    break;

                case "GoToMainMenu":
                    // 메인 메뉴로 이동
                    var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (currentSceneName == "Store")
                    {
                        // Store에서는 Menu 씬으로 이동
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                    }
                    else
                    {
                        // Menu 씬 내에서는 패널 전환
                        NavigateMenuPanel("MainMenu");
                    }
                    break;

                case "GoToGameModeSelection":
                    // 게임 모드 선택으로 이동
                    NavigateMenuPanel("GameModeSelection");
                    break;

                case "OpenStore":
                    // 상점 열기 - 내장 Store 팝업 표시
                    if (inGameUI != null && !inGameUI.IsStorePopupVisible())
                    {
                        inGameUI.OpenStorePopup();
                    }
                    break;

                // 튜토리얼 선택
                case "SelectTutorial":
                    // Debug.Log("[VoiceRecognition] 튜토리얼 선택 명령 감지");
                    StartChapter(0);
                    break;

                // 챕터 선택 (1-12)
                case "SelectChapter1":
                case "SelectChapter2":
                case "SelectChapter3":
                case "SelectChapter4":
                case "SelectChapter5":
                case "SelectChapter6":
                case "SelectChapter7":
                case "SelectChapter8":
                case "SelectChapter9":
                case "SelectChapter10":
                case "SelectChapter11":
                case "SelectChapter12":
                    // Debug.Log($"[VoiceRecognition] 챕터 선택 명령 감지: {command}");
                    // 챕터 번호 추출 (SelectChapter1 -> 1)
                    string chapterNumStr = command.Substring("SelectChapter".Length);
                    // Debug.Log($"[VoiceRecognition] 챕터 번호 문자열: {chapterNumStr}");
                    int chapterId = int.Parse(chapterNumStr);
                    // Debug.Log($"[VoiceRecognition] 파싱된 챕터 ID: {chapterId}");
                    StartChapter(chapterId);
                    break;

                default:
                    Debug.LogWarning($"[VoiceRecognition] 알 수 없는 시스템 명령: {command}");
                    break;
            }
        }

        /// <summary>
        /// Menu 씬의 패널 네비게이션
        /// </summary>
        private void NavigateMenuPanel(string panelName)
        {
            // Debug.Log($"[VoiceRecognition] NavigateMenuPanel 호출됨: {panelName}");

            // 현재 씬 확인
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            // Debug.Log($"[VoiceRecognition] 현재 씬: {currentScene}");

            if (currentScene != "Menu")
            {
                Debug.LogWarning($"[VoiceRecognition] Menu 씬이 아닙니다. 현재: {currentScene}");
                return;
            }

            var menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();
            if (menuManager != null)
            {
                // Debug.Log($"[VoiceRecognition] MenuManager 찾음, 패널 전환 시도: {panelName}");
                switch (panelName)
                {
                    case "GameModeSelection":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.GameModeSelection);
                        break;
                    case "StoryMode":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.StoryMode);
                        break;
                    case "EndlessMode":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.EndlessMode);
                        break;
                    case "MainMenu":
                        menuManager.ShowPanel(LostSpells.UI.MenuManager.MenuPanel.MainMenu);
                        break;
                }
                // Debug.Log($"[VoiceRecognition] 메뉴 패널 이동 완료: {panelName}");
            }
            else
            {
                Debug.LogError("[VoiceRecognition] MenuManager를 찾을 수 없습니다! FindFirstObjectByType 실패");
            }
        }

        /// <summary>
        /// 챕터 시작
        /// </summary>
        private void StartChapter(int chapterId)
        {
            // Debug.Log($"[VoiceRecognition] StartChapter 호출됨: 챕터 {chapterId}");

            // GameStateManager를 통해 챕터 시작
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.StartChapter(chapterId);
                // Debug.Log($"[VoiceRecognition] GameStateManager.StartChapter({chapterId}) 호출 완료");
            }
            else
            {
                Debug.LogError("[VoiceRecognition] GameStateManager.Instance가 null입니다!");
            }

            // InGame 씬으로 이동
            // Debug.Log("[VoiceRecognition] InGame 씬으로 이동 시도...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("InGame");
        }

        /// <summary>
        /// Menu 씬에서 이전 화면으로 돌아가기
        /// </summary>
        private void NavigateMenuBack()
        {
            // Options 씬이 열려있으면 닫기
            if (IsSceneLoaded("Options"))
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Options");
                // InGame에서 메뉴 팝업이 열려있으면 일시정지 유지
                if (!IsInGameMenuPopupVisible())
                {
                    Time.timeScale = 1f;
                }
                return;
            }

            // Store 씬이 열려있으면 Menu로 이동
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene == "Store")
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
                return;
            }

            // Menu 씬에서는 MenuManager의 GoBack 사용
            var menuManager = FindFirstObjectByType<LostSpells.UI.MenuManager>();
            if (menuManager != null)
            {
                menuManager.GoBack();
                // Debug.Log("[VoiceRecognition] 이전 메뉴로 돌아가기");
            }
        }

        /// <summary>
        /// 특정 씬이 로드되어 있는지 확인
        /// </summary>
        private bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// InGame에서 메뉴 팝업이 열려있는지 확인
        /// </summary>
        private bool IsInGameMenuPopupVisible()
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != "InGame") return false;

            if (inGameUI != null)
            {
                var root = inGameUI.GetComponent<UIDocument>()?.rootVisualElement;
                var menuPopup = root?.Q<VisualElement>("MenuPopup");
                if (menuPopup != null && menuPopup.style.display == DisplayStyle.Flex)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 녹음 중인지 확인
        /// </summary>
        public bool IsRecording()
        {
            return isRecording;
        }

        /// <summary>
        /// 음성 인식 언어 변경
        /// </summary>
        public void ChangeLanguage(string languageCode)
        {
            if (serverClient != null)
            {
                serverClient.SetLanguage(languageCode);
            }
        }

        // 기존 VoiceRecognitionManager에 있던 불필요한 메서드 제거 (StartVoiceRecording, StopVoiceRecording, GetVoiceRecordKey, ParseKey)
        // 기존 Update()의 키 입력 처리 로직 제거
        // 기존 maxRecordingTime 필드 제거 (ConstantRecording 로직이 자동으로 끝냄)

        /// <summary>
        /// 스킬 초기화 - DataManager에서 스킬 로드 및 서버 설정 
        /// </summary>
        private void InitializeSkills()
        {
            var skills = DataManager.Instance.GetAllSkillData();

            if (skills != null && skills.Count > 0)
            {
                StartCoroutine(serverClient.SetSkills(skills));
            }
        }
    }
}