using UnityEngine;
using UnityEditor;
using LostSpells.Data;

namespace LostSpells.Editor
{
    public static class WaveDataCreator
    {
        [MenuItem("LostSpells/Create Chapter 1 Wave Data")]
        public static void CreateChapter1WaveData()
        {
            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/WaveData"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "WaveData");
            }

            // ScriptableObject 생성
            ChapterWaveData chapterData = ScriptableObject.CreateInstance<ChapterWaveData>();
            chapterData.chapterId = 1;
            chapterData.chapterName = "Chapter 1";

            // Wave 1: 드래곤 1마리 (오른쪽) + 오우거 1마리 (왼쪽)
            WaveInfo wave1 = new WaveInfo
            {
                waveNumber = 1,
                startDelay = 1f,
                enemies = new EnemySpawnInfo[]
                {
                    new EnemySpawnInfo
                    {
                        enemyPrefabName = "BlueDragonEnemy",
                        spawnSide = SpawnSide.Right,
                        count = 1,
                        spawnInterval = 0f,
                        healthBonus = 0,
                        speedBonus = 0f
                    },
                    new EnemySpawnInfo
                    {
                        enemyPrefabName = "GreenOgreEnemy",
                        spawnSide = SpawnSide.Left,
                        count = 1,
                        spawnInterval = 0f,
                        healthBonus = 0,
                        speedBonus = 0f
                    }
                }
            };

            chapterData.waves = new WaveInfo[] { wave1 };

            // 기존 에셋 삭제
            string assetPath = "Assets/Resources/WaveData/Chapter1WaveData.asset";
            if (AssetDatabase.LoadAssetAtPath<ChapterWaveData>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            // 에셋 생성
            AssetDatabase.CreateAsset(chapterData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WaveDataCreator] Chapter 1 Wave Data created at {assetPath}");

            // 생성된 에셋 선택
            Selection.activeObject = chapterData;
        }
    }
}
