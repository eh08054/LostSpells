using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostSpells.Data
{
    /// <summary>
    /// 게임 설정 데이터 관리 싱글톤
    /// JSON 파일에서 게임 설정 데이터를 로드하고 관리
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        private static DataManager instance;
        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("DataManager");
                    instance = go.AddComponent<DataManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private List<ChapterData> chapterDataList = new List<ChapterData>();
        private List<MonsterData> monsterDataList = new List<MonsterData>();
        private List<SkillData> skillDataList = new List<SkillData>();

        private Dictionary<int, ChapterData> chapterDataDict;
        private Dictionary<string, MonsterData> monsterDataDict;
        private Dictionary<string, SkillData> skillDataDict;

        private void Awake()
        {
            // 싱글톤 인스턴스 체크
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // 데이터 초기화
            InitializeData();
        }

        /// <summary>
        /// 데이터 초기화
        /// Resources/GameData 폴더에서 JSON 파일을 로드
        /// </summary>
        private void InitializeData()
        {
            // JSON 파일에서 데이터 로드
            LoadMonsterData();
            LoadChapterData();
            LoadSkillData();
        }

        /// <summary>
        /// 몬스터 데이터 JSON 로드
        /// </summary>
        private void LoadMonsterData()
        {
            monsterDataDict = new Dictionary<string, MonsterData>();
            monsterDataList = new List<MonsterData>();

            // Resources/GameData/Monsters.json 파일 로드
            TextAsset jsonFile = Resources.Load<TextAsset>("GameData/Monsters");
            if (jsonFile != null)
            {
                MonsterDataList wrapper = JsonUtility.FromJson<MonsterDataList>(jsonFile.text);
                if (wrapper != null && wrapper.monsters != null)
                {
                    monsterDataList = wrapper.monsters;
                    foreach (var monster in monsterDataList)
                    {
                        if (!string.IsNullOrEmpty(monster.monsterId))
                        {
                            monsterDataDict[monster.monsterId] = monster;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Resources/GameData/Monsters.json 파일을 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 챕터 데이터 JSON 로드
        /// </summary>
        private void LoadChapterData()
        {
            chapterDataDict = new Dictionary<int, ChapterData>();
            chapterDataList = new List<ChapterData>();

            // Resources/GameData/Chapters.json 파일 로드
            TextAsset jsonFile = Resources.Load<TextAsset>("GameData/Chapters");
            if (jsonFile != null)
            {
                ChapterDataList wrapper = JsonUtility.FromJson<ChapterDataList>(jsonFile.text);
                if (wrapper != null && wrapper.chapters != null)
                {
                    chapterDataList = wrapper.chapters;
                    foreach (var chapter in chapterDataList)
                    {
                        chapterDataDict[chapter.chapterId] = chapter;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Resources/GameData/Chapters.json 파일을 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 챕터 ID로 챕터 데이터 가져오기
        /// </summary>
        public ChapterData GetChapterData(int chapterId)
        {
            if (chapterDataDict != null && chapterDataDict.ContainsKey(chapterId))
            {
                return chapterDataDict[chapterId];
            }

            Debug.LogWarning($"챕터 ID {chapterId}의 데이터를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 몬스터 ID로 몬스터 데이터 가져오기
        /// </summary>
        public MonsterData GetMonsterData(string monsterId)
        {
            if (monsterDataDict != null && monsterDataDict.ContainsKey(monsterId))
            {
                return monsterDataDict[monsterId];
            }

            Debug.LogWarning($"몬스터 ID '{monsterId}'의 데이터를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 모든 챕터 데이터 리스트 가져오기 (챕터 ID 순서대로 정렬)
        /// </summary>
        public List<ChapterData> GetAllChapterData()
        {
            // 챕터 ID 순서대로 정렬하여 반환
            var sortedChapters = new List<ChapterData>(chapterDataList);
            sortedChapters.Sort((a, b) => a.chapterId.CompareTo(b.chapterId));
            return sortedChapters;
        }

        /// <summary>
        /// 모든 몬스터 데이터 리스트 가져오기
        /// </summary>
        public List<MonsterData> GetAllMonsterData()
        {
            return monsterDataList;
        }

        /// <summary>
        /// 스킬 데이터 JSON 로드
        /// </summary>
        private void LoadSkillData()
        {
            skillDataDict = new Dictionary<string, SkillData>();
            skillDataList = new List<SkillData>();

            // Resources/GameData/Skills.json 파일 로드
            TextAsset jsonFile = Resources.Load<TextAsset>("GameData/Skills");
            if (jsonFile != null)
            {
                SkillDataList wrapper = JsonUtility.FromJson<SkillDataList>(jsonFile.text);
                if (wrapper != null && wrapper.skills != null)
                {
                    skillDataList = wrapper.skills;
                    foreach (var skill in skillDataList)
                    {
                        if (!string.IsNullOrEmpty(skill.skillId))
                        {
                            skillDataDict[skill.skillId] = skill;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Resources/GameData/Skills.json 파일을 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 스킬 ID로 스킬 데이터 가져오기
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            if (skillDataDict != null && skillDataDict.ContainsKey(skillId))
            {
                return skillDataDict[skillId];
            }

            Debug.LogWarning($"스킬 ID '{skillId}'의 데이터를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 모든 스킬 데이터 리스트 가져오기
        /// </summary>
        public List<SkillData> GetAllSkillData()
        {
            return skillDataList;
        }

        /// <summary>
        /// 데이터 리로드 (런타임 중 데이터 변경 시)
        /// </summary>
        public void ReloadData()
        {
            InitializeData();
        }
    }

    /// <summary>
    /// JSON 배열 로드를 위한 래퍼 클래스 - 챕터 데이터
    /// </summary>
    [Serializable]
    public class ChapterDataList
    {
        public List<ChapterData> chapters;
    }

    /// <summary>
    /// JSON 배열 로드를 위한 래퍼 클래스 - 몬스터 데이터
    /// </summary>
    [Serializable]
    public class MonsterDataList
    {
        public List<MonsterData> monsters;
    }

    /// <summary>
    /// JSON 배열 로드를 위한 래퍼 클래스 - 스킬 데이터
    /// </summary>
    [Serializable]
    public class SkillDataList
    {
        public List<SkillData> skills;
    }
}
