using System;
using System.IO;
using UnityEngine;

namespace LostSpells.Data
{
    /// <summary>
    /// 저장/로드 관리 싱글톤
    /// 게임 데이터를 JSON 파일로 저장하고 로드함
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    instance = go.AddComponent<SaveManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private PlayerSaveData currentSaveData;
        private string saveFilePath;

        private void Awake()
        {
            // 싱글톤 인스턴스 체크
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[SaveManager] 중복된 SaveManager 인스턴스가 감지되어 제거됩니다. (GameObject: {gameObject.name})");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // 저장 파일 경로 설정
            saveFilePath = Path.Combine(Application.persistentDataPath, "PlayerSaveData.json");

            // 게임 시작 시 데이터 로드
            LoadGame();
        }

        private void OnDestroy()
        {
            // 인스턴스가 이 객체라면 null로 설정
            if (instance == this)
            {
                instance = null;
            }
        }

        // OnApplicationQuit은 Unity 에디터에서 불안정하게 여러 번 호출될 수 있으므로 제거
        // 대신 에디터 플레이 모드 종료 시(ExitingPlayMode)에만 저장

#if UNITY_EDITOR
        private static bool editorEventRegistered = false;

        // 에디터에서 플레이 모드 종료 시 저장
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // 이벤트 중복 등록 방지
            if (!editorEventRegistered)
            {
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                editorEventRegistered = true;
            }
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                if (instance != null)
                {
                    instance.SaveGame();
                }
            }
        }
#endif

        private void OnApplicationPause(bool pauseStatus)
        {
            // 앱이 백그라운드로 갈 때 자동 저장 (모바일용)
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        /// <summary>
        /// 현재 저장 데이터 가져오기
        /// </summary>
        public PlayerSaveData GetCurrentSaveData()
        {
            if (currentSaveData == null)
            {
                LoadGame();
            }
            return currentSaveData;
        }

        /// <summary>
        /// 게임 데이터 로드
        /// 저장 파일이 없으면 새로운 데이터 생성
        /// </summary>
        public void LoadGame()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    currentSaveData = JsonUtility.FromJson<PlayerSaveData>(json);

                    // 이전 세이브 파일 마이그레이션: "-" 날짜를 "0000-00-00"로 변환하고 저장
                    bool needsSave = false;
                    if (currentSaveData.endlessModeTopRecords != null)
                    {
                        foreach (var record in currentSaveData.endlessModeTopRecords)
                        {
                            if (string.IsNullOrEmpty(record.date) || record.date == "-")
                            {
                                record.date = "0000-00-00";
                                needsSave = true;
                            }
                        }
                    }

                    // 마이그레이션이 필요한 경우 즉시 저장
                    if (needsSave)
                    {
                        SaveGame();
                    }
                }
                else
                {
                    currentSaveData = PlayerSaveData.CreateDefault();
                    SaveGame(); // 초기 데이터 저장
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 게임 데이터 로드 실패: {e.Message}\n{e.StackTrace}");
                currentSaveData = PlayerSaveData.CreateDefault();
            }
        }

        /// <summary>
        /// 게임 데이터 저장
        /// </summary>
        public void SaveGame()
        {
            try
            {
                if (currentSaveData == null)
                {
                    Debug.LogWarning("[SaveManager] SaveGame - currentSaveData가 null입니다.");
                    return;
                }

                // 마지막 저장 시간 업데이트
                currentSaveData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // JSON으로 변환하여 저장
                string json = JsonUtility.ToJson(currentSaveData, true);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 게임 데이터 저장 실패: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 저장 파일 삭제
        /// </summary>
        public void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                }

                currentSaveData = PlayerSaveData.CreateDefault();
            }
            catch (Exception e)
            {
                Debug.LogError($"저장 파일 삭제 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 저장 파일 경로 가져오기
        /// </summary>
        public string GetSaveFilePath()
        {
            return saveFilePath;
        }

        /// <summary>
        /// 저장 파일 존재 여부 확인
        /// </summary>
        public bool SaveFileExists()
        {
            return File.Exists(saveFilePath);
        }

        // ========== 화폐 관리 메서드 ==========

        /// <summary>
        /// 다이아몬드 추가
        /// </summary>
        public void AddDiamonds(int amount)
        {
            if (currentSaveData != null && amount > 0)
            {
                currentSaveData.diamonds += amount;
                SaveGame();
            }
        }

        /// <summary>
        /// 다이아몬드 사용
        /// </summary>
        public bool SpendDiamonds(int amount)
        {
            if (currentSaveData != null && amount > 0)
            {
                if (currentSaveData.diamonds >= amount)
                {
                    currentSaveData.diamonds -= amount;
                    SaveGame();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 부활석 추가
        /// </summary>
        public void AddReviveStones(int amount)
        {
            if (currentSaveData != null && amount > 0)
            {
                currentSaveData.reviveStones += amount;
                SaveGame();
            }
        }

        /// <summary>
        /// 부활석 사용
        /// </summary>
        public bool SpendReviveStones(int amount)
        {
            if (currentSaveData != null && amount > 0)
            {
                if (currentSaveData.reviveStones >= amount)
                {
                    currentSaveData.reviveStones -= amount;
                    SaveGame();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 골드 추가
        /// </summary>
        public void AddGold(int amount)
        {
            if (currentSaveData != null && amount > 0)
            {
                currentSaveData.gold += amount;
                SaveGame();
            }
        }

        /// <summary>
        /// 골드 사용
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (currentSaveData != null && amount > 0)
            {
                if (currentSaveData.gold >= amount)
                {
                    currentSaveData.gold -= amount;
                    SaveGame();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        // ========== 무한 모드 관리 ==========

        /// <summary>
        /// 무한 모드 기록 업데이트 (상위 5개 기록 유지)
        /// </summary>
        public void UpdateEndlessModeRecord(int score, int wave)
        {
            if (currentSaveData == null)
                return;

            // 리스트가 비어있거나 크기가 5가 아니면 초기화
            if (currentSaveData.endlessModeTopRecords == null || currentSaveData.endlessModeTopRecords.Count != 5)
            {
                currentSaveData.endlessModeTopRecords = new System.Collections.Generic.List<EndlessModeRecord>
                {
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00")
                };
            }

            // 새 기록 추가
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            currentSaveData.endlessModeTopRecords.Add(new EndlessModeRecord(score, wave, currentDate));

            // 점수 기준 내림차순 정렬
            currentSaveData.endlessModeTopRecords.Sort((a, b) => b.score.CompareTo(a.score));

            // 상위 5개만 유지
            if (currentSaveData.endlessModeTopRecords.Count > 5)
            {
                currentSaveData.endlessModeTopRecords = currentSaveData.endlessModeTopRecords.GetRange(0, 5);
            }

            SaveGame();
        }

        /// <summary>
        /// 무한 모드 상위 5개 기록 가져오기
        /// </summary>
        public System.Collections.Generic.List<EndlessModeRecord> GetEndlessModeTopRecords()
        {
            if (currentSaveData == null)
            {
                LoadGame();
            }

            // 리스트가 비어있거나 크기가 5가 아니면 초기화
            if (currentSaveData.endlessModeTopRecords == null || currentSaveData.endlessModeTopRecords.Count != 5)
            {
                currentSaveData.endlessModeTopRecords = new System.Collections.Generic.List<EndlessModeRecord>
                {
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00"),
                    new EndlessModeRecord(0, 0, "0000-00-00")
                };
            }

            return currentSaveData.endlessModeTopRecords;
        }

        // ========== 데이터 초기화 ==========

        /// <summary>
        /// 저장 데이터를 완전히 초기화 (저장 파일 삭제 및 기본값으로 재설정)
        /// </summary>
        public void ResetSaveData()
        {
            // 저장 파일 삭제
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }

            // 새로운 기본 데이터 생성
            currentSaveData = PlayerSaveData.CreateDefault();
            SaveGame();
        }
    }
}
