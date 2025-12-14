#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using LostSpells.Data;

namespace LostSpells.Editor
{
    /// <summary>
    /// ChapterWaveData 에셋 자동 생성 도구
    /// </summary>
    public class ChapterWaveDataCreator : EditorWindow
    {
        private const string OUTPUT_PATH = "Assets/Resources/WaveData";

        // 적 분류 (난이도별)
        private static readonly string[] EasyEnemies = new string[]
        {
            "BrownRatEnemy", "GreyRatEnemy", "GreenFrogEnemy", "RedFrogEnemy",
            "YellowBeeEnemy", "PurpleBatEnemy", "GreenDuckEnemy", "YellowDuckEnemy",
            "BrownPigEnemy", "PinkPigEnemy", "MosquitoEnemy", "BirdEnemy"
        };

        private static readonly string[] NormalEnemies = new string[]
        {
            "BlueDragonEnemy", "GreenDragonEnemy", "RedDragonEnemy",
            "BrownWolfEnemy", "GreyWolfEnemy", "BlueWolfEnemy", "BlackWolfEnemy",
            "BrownSpiderEnemy", "BlackSpiderEnemy", "PurpleSpiderEnemy", "RedSpiderEnemy",
            "BrownBoarEnemy", "BlackBoarEnemy", "GreenSnakeEnemy", "YellowSnakeEnemy",
            "BlueCrabEnemy", "RedCrabEnemy", "GreenTortoiseEnemy", "RedScorpionEnemy"
        };

        private static readonly string[] HardEnemies = new string[]
        {
            "BlueRexEnemy", "GreenRexEnemy", "RedRexEnemy",
            "GreenOgreEnemy", "RedOgreEnemy", "PurpleOgreEnemy",
            "BrownBearEnemy", "BlackBearEnemy", "GreenTrollEnemy", "BlueTrollEnemy", "BlackTrollEnemy",
            "BrownWerevolfEnemy", "RedWerevolfEnemy", "BlackWerewolfEnemy",
            "BlueManticoraEnemy", "RoyalManticoraEnemy", "GreenDemigrifEnemy", "GoldenDemigrifEnemy"
        };

        private static readonly string[] BossEnemies = new string[]
        {
            "Minotaur1Enemy", "Minotaur2Enemy", "Minotaur3Enemy",
            "Naga1Enemy", "Naga2Enemy", "Naga3Enemy",
            "Gorgon1Enemy", "Gorgon2Enemy", "Gorgon3Enemy", "Gorgon4Enemy", "Gorgon5Enemy",
            "Lich1Enemy", "Lich2Enemy", "Lich3Enemy",
            "DeathDog1Enemy", "DeathDog2Enemy", "DeathDog3Enemy",
            "DemonEnemy", "BeholderEnemy", "BrainsterEnemy"
        };

        [MenuItem("LostSpells/Create Chapter Wave Data")]
        public static void CreateAllChapterData()
        {
            CreateChapterDataInternal(false);
        }

        [MenuItem("LostSpells/Force Regenerate Chapter Wave Data")]
        public static void ForceRegenerateAllChapterData()
        {
            CreateChapterDataInternal(true);
        }

        private static void CreateChapterDataInternal(bool forceRegenerate)
        {
            // 출력 폴더 생성
            if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "WaveData");
            }

            // 실제 존재하는 적 프리팹 목록 로드
            var existingPrefabs = Resources.LoadAll<GameObject>("Enemies")
                .Select(p => p.name)
                .ToHashSet();

            // 각 챕터 데이터 생성 (0~7)
            CreateChapter0Data(existingPrefabs, forceRegenerate);
            CreateChapter1Data(existingPrefabs, forceRegenerate);
            CreateChapter2Data(existingPrefabs, forceRegenerate);
            CreateChapter3Data(existingPrefabs, forceRegenerate);
            CreateChapter4Data(existingPrefabs, forceRegenerate);
            CreateChapter5Data(existingPrefabs, forceRegenerate);
            CreateChapter6Data(existingPrefabs, forceRegenerate);
            CreateChapter7Data(existingPrefabs, forceRegenerate);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ChapterWaveDataCreator] 챕터 웨이브 데이터 생성 완료!");
            EditorUtility.DisplayDialog("Chapter Wave Data Creator",
                "챕터 0~7 웨이브 데이터가 생성되었습니다.\n경로: Assets/Resources/WaveData/",
                "확인");
        }

        /// <summary>
        /// 챕터 0: 튜토리얼 - 드래곤
        /// </summary>
        private static void CreateChapter0Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter0WaveData.asset";

            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 0;
            data.chapterName = "튜토리얼";

            var bossList = FilterExisting(BossEnemies, existingPrefabs);

            data.waves = new WaveInfo[]
            {
                CreateWave(1, 2f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "BlueDragonEnemy"), SpawnSide.Right, 1, 2f),
                }),
                CreateWave(2, 1.5f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "GreenDragonEnemy"), SpawnSide.Left, 1, 2f),
                }),
                CreateWave(3, 1.5f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "RedDragonEnemy"), SpawnSide.Both, 1, 1.5f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 챕터 1: 쉬운 적 위주
        /// </summary>
        private static void CreateChapter1Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter1WaveData.asset";

            // 기존 에셋 확인 및 삭제
            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 1;
            data.chapterName = "숲의 시작";

            var easyList = FilterExisting(EasyEnemies, existingPrefabs);
            var normalList = FilterExisting(NormalEnemies, existingPrefabs);

            // 고정된 적 구성 (랜덤하지 않게)
            data.waves = new WaveInfo[]
            {
                CreateWave(1, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(easyList, "BrownPigEnemy"), SpawnSide.Right, 2, 1.5f),
                }),
                CreateWave(2, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(easyList, "BrownRatEnemy", 1), SpawnSide.Left, 2, 1.5f),
                    CreateEnemy(GetFirstOrDefault(easyList, "GreyRatEnemy", 2), SpawnSide.Right, 1, 1.5f),
                }),
                CreateWave(3, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(normalList, "GreenTortoiseEnemy"), SpawnSide.Both, 3, 1.2f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 챕터 2: 보통 적 + 어려운 적
        /// </summary>
        private static void CreateChapter2Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter2WaveData.asset";

            // 기존 에셋 확인 및 삭제
            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 2;
            data.chapterName = "어둠의 숲";

            var normalList = FilterExisting(NormalEnemies, existingPrefabs);
            var hardList = FilterExisting(HardEnemies, existingPrefabs);

            // 고정된 적 구성
            data.waves = new WaveInfo[]
            {
                CreateWave(1, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(normalList, "BrownWolfEnemy"), SpawnSide.Right, 2, 1.2f),
                    CreateEnemy(GetFirstOrDefault(normalList, "BrownSpiderEnemy", 1), SpawnSide.Left, 2, 1.2f),
                }),
                CreateWave(2, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(normalList, "RedScorpionEnemy", 2), SpawnSide.Both, 3, 1f),
                    CreateEnemy(GetFirstOrDefault(hardList, "RedOgreEnemy"), SpawnSide.Right, 1, 2f),
                }),
                CreateWave(3, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "BlackTrollEnemy", 1), SpawnSide.Both, 2, 1.5f),
                    CreateEnemy(GetFirstOrDefault(normalList, "BrownWolfEnemy"), SpawnSide.Both, 2, 1f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 챕터 3: 어려운 적 + 보스
        /// </summary>
        private static void CreateChapter3Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter3WaveData.asset";

            // 기존 에셋 확인 및 삭제
            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 3;
            data.chapterName = "마왕의 성";

            var hardList = FilterExisting(HardEnemies, existingPrefabs);
            var bossList = FilterExisting(BossEnemies, existingPrefabs);

            // 고정된 적 구성
            data.waves = new WaveInfo[]
            {
                CreateWave(1, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "BlueManticoraEnemy"), SpawnSide.Both, 3, 1.2f),
                }),
                CreateWave(2, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "RedWerevolfEnemy", 1), SpawnSide.Right, 2, 1f),
                    CreateEnemy(GetFirstOrDefault(hardList, "RedRexEnemy", 2), SpawnSide.Left, 2, 1f),
                }),
                CreateWave(3, 2f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "BeholderEnemy"), SpawnSide.Right, 1, 0f, 50, 0.5f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 챕터 4: 황야의 땅 - 보통 + 어려운 적 혼합
        /// </summary>
        private static void CreateChapter4Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter4WaveData.asset";

            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 4;
            data.chapterName = "황야의 땅";

            var normalList = FilterExisting(NormalEnemies, existingPrefabs);
            var hardList = FilterExisting(HardEnemies, existingPrefabs);

            data.waves = new WaveInfo[]
            {
                CreateWave(1, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(normalList, "BlackBoarEnemy"), SpawnSide.Both, 3, 1.2f),
                    CreateEnemy(GetFirstOrDefault(normalList, "GreenSnakeEnemy"), SpawnSide.Right, 2, 1f),
                }),
                CreateWave(2, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "GreenOgreEnemy"), SpawnSide.Left, 2, 1.5f),
                    CreateEnemy(GetFirstOrDefault(normalList, "YellowSnakeEnemy"), SpawnSide.Right, 3, 1f),
                }),
                CreateWave(3, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "BrownBearEnemy"), SpawnSide.Both, 2, 1.5f),
                    CreateEnemy(GetFirstOrDefault(hardList, "GreenTrollEnemy"), SpawnSide.Right, 1, 2f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 챕터 5: 용암 지대 - 어려운 적 위주
        /// </summary>
        private static void CreateChapter5Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter5WaveData.asset";

            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 5;
            data.chapterName = "용암 지대";

            var hardList = FilterExisting(HardEnemies, existingPrefabs);

            data.waves = new WaveInfo[]
            {
                CreateWave(1, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "BlueRexEnemy"), SpawnSide.Right, 2, 1.2f),
                    CreateEnemy(GetFirstOrDefault(hardList, "GreenRexEnemy"), SpawnSide.Left, 2, 1.2f),
                }),
                CreateWave(2, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "RedRexEnemy"), SpawnSide.Both, 3, 1f),
                    CreateEnemy(GetFirstOrDefault(hardList, "PurpleOgreEnemy"), SpawnSide.Right, 2, 1.5f),
                }),
                CreateWave(3, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "BlackBearEnemy"), SpawnSide.Left, 2, 1.2f),
                    CreateEnemy(GetFirstOrDefault(hardList, "BlueTrollEnemy"), SpawnSide.Right, 2, 1.2f),
                    CreateEnemy(GetFirstOrDefault(hardList, "RoyalManticoraEnemy"), SpawnSide.Both, 1, 2f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 챕터 6: 심연의 동굴 - 어려운 적 + 미니 보스
        /// </summary>
        private static void CreateChapter6Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter6WaveData.asset";

            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 6;
            data.chapterName = "심연의 동굴";

            var hardList = FilterExisting(HardEnemies, existingPrefabs);
            var bossList = FilterExisting(BossEnemies, existingPrefabs);

            data.waves = new WaveInfo[]
            {
                CreateWave(1, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(hardList, "BlackWerewolfEnemy"), SpawnSide.Both, 3, 1f),
                    CreateEnemy(GetFirstOrDefault(hardList, "GoldenDemigrifEnemy"), SpawnSide.Right, 2, 1.2f),
                }),
                CreateWave(2, 1f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "Minotaur1Enemy"), SpawnSide.Right, 1, 0f, 30, 0.3f),
                    CreateEnemy(GetFirstOrDefault(hardList, "BlackTrollEnemy"), SpawnSide.Left, 2, 1.5f),
                }),
                CreateWave(3, 2f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "Naga1Enemy"), SpawnSide.Left, 1, 0f, 40, 0.3f),
                    CreateEnemy(GetFirstOrDefault(bossList, "Gorgon1Enemy"), SpawnSide.Right, 1, 0f, 40, 0.3f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 챕터 7: 최후의 결전 - 보스 러시
        /// </summary>
        private static void CreateChapter7Data(HashSet<string> existingPrefabs, bool forceRegenerate)
        {
            string path = $"{OUTPUT_PATH}/Chapter7WaveData.asset";

            var existingAsset = AssetDatabase.LoadAssetAtPath<ChapterWaveData>(path);
            if (existingAsset != null)
            {
                if (!forceRegenerate)
                {
                    Debug.Log($"[ChapterWaveDataCreator] 이미 존재: {path}");
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }

            var data = ScriptableObject.CreateInstance<ChapterWaveData>();
            data.chapterId = 7;
            data.chapterName = "최후의 결전";

            var bossList = FilterExisting(BossEnemies, existingPrefabs);

            data.waves = new WaveInfo[]
            {
                CreateWave(1, 2f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "Lich1Enemy"), SpawnSide.Right, 1, 0f, 50, 0.4f),
                    CreateEnemy(GetFirstOrDefault(bossList, "DeathDog1Enemy"), SpawnSide.Left, 1, 0f, 40, 0.5f),
                }),
                CreateWave(2, 2f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "Minotaur2Enemy"), SpawnSide.Left, 1, 0f, 60, 0.4f),
                    CreateEnemy(GetFirstOrDefault(bossList, "Naga2Enemy"), SpawnSide.Right, 1, 0f, 60, 0.4f),
                }),
                CreateWave(3, 3f, new[]
                {
                    CreateEnemy(GetFirstOrDefault(bossList, "DemonEnemy"), SpawnSide.Right, 1, 0f, 100, 0.5f),
                }),
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[ChapterWaveDataCreator] 생성: {path}");
        }

        /// <summary>
        /// 웨이브 생성 헬퍼
        /// </summary>
        private static WaveInfo CreateWave(int waveNumber, float startDelay, EnemySpawnInfo[] enemies)
        {
            return new WaveInfo
            {
                waveNumber = waveNumber,
                startDelay = startDelay,
                enemies = enemies
            };
        }

        /// <summary>
        /// 적 스폰 정보 생성 헬퍼
        /// </summary>
        private static EnemySpawnInfo CreateEnemy(string prefabName, SpawnSide side, int count, float interval, int healthBonus = 0, float speedBonus = 0f)
        {
            return new EnemySpawnInfo
            {
                enemyPrefabName = prefabName,
                spawnSide = side,
                count = count,
                spawnInterval = interval,
                healthBonus = healthBonus,
                speedBonus = speedBonus
            };
        }

        /// <summary>
        /// 존재하는 프리팹만 필터링
        /// </summary>
        private static List<string> FilterExisting(string[] names, HashSet<string> existing)
        {
            return names.Where(n => existing.Contains(n)).ToList();
        }

        /// <summary>
        /// 특정 적 또는 인덱스에서 선택 (고정된 구성을 위해)
        /// </summary>
        private static string GetFirstOrDefault(List<string> list, string preferredName, int fallbackIndex = 0)
        {
            if (list == null || list.Count == 0)
                return "BlueDragonEnemy"; // 기본값

            // 선호하는 적이 리스트에 있으면 사용
            if (list.Contains(preferredName))
                return preferredName;

            // 없으면 인덱스로 선택
            int index = Mathf.Clamp(fallbackIndex, 0, list.Count - 1);
            return list[index];
        }

        [MenuItem("LostSpells/List Enemy Categories")]
        public static void ListEnemyCategories()
        {
            var existingPrefabs = Resources.LoadAll<GameObject>("Enemies")
                .Select(p => p.name)
                .ToHashSet();

            Debug.Log("========== 적 분류 ==========");

            var easyExist = FilterExisting(EasyEnemies, existingPrefabs);
            Debug.Log($"\n쉬움 ({easyExist.Count}개): {string.Join(", ", easyExist.Select(e => e.Replace("Enemy", "")))}");

            var normalExist = FilterExisting(NormalEnemies, existingPrefabs);
            Debug.Log($"\n보통 ({normalExist.Count}개): {string.Join(", ", normalExist.Select(e => e.Replace("Enemy", "")))}");

            var hardExist = FilterExisting(HardEnemies, existingPrefabs);
            Debug.Log($"\n어려움 ({hardExist.Count}개): {string.Join(", ", hardExist.Select(e => e.Replace("Enemy", "")))}");

            var bossExist = FilterExisting(BossEnemies, existingPrefabs);
            Debug.Log($"\n보스 ({bossExist.Count}개): {string.Join(", ", bossExist.Select(e => e.Replace("Enemy", "")))}");
        }
    }
}
#endif
