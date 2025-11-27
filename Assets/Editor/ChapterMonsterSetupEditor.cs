using UnityEngine;
using UnityEditor;
using System.IO;

namespace LostSpells.Editor
{
    public class ChapterMonsterSetupEditor : EditorWindow
    {
        [MenuItem("LostSpells/Setup Chapter Monsters")]
        public static void ShowWindow()
        {
            GetWindow<ChapterMonsterSetupEditor>("Chapter Monster Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("챕터 몬스터 데이터 자동 생성", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("챕터 1~8에 대한 ChapterMonsterData 에셋을 자동으로 생성하고\nDragon 스프라이트를 할당합니다.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create All Chapter Monster Data", GUILayout.Height(40)))
            {
                CreateChapterMonsterData();
            }
        }

        private void CreateChapterMonsterData()
        {
            // Resources 폴더 생성 (없으면)
            string resourcesPath = "Assets/Resources/ChapterMonsters";
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }

            // Dragon 스프라이트 로드
            string dragonPath = "Assets/Templates/PixelFantasy/PixelMonsters/BossPack1/Dragon";
            Sprite blueDragon = AssetDatabase.LoadAssetAtPath<Sprite>($"{dragonPath}/BlueDragon.png");
            Sprite greenDragon = AssetDatabase.LoadAssetAtPath<Sprite>($"{dragonPath}/GreenDragon.png");
            Sprite redDragon = AssetDatabase.LoadAssetAtPath<Sprite>($"{dragonPath}/RedDragon.png");

            if (blueDragon == null || greenDragon == null || redDragon == null)
            {
                EditorUtility.DisplayDialog("오류", "Dragon 스프라이트를 찾을 수 없습니다!\n경로를 확인하세요.", "OK");
                return;
            }

            // 챕터 1~8에 대한 데이터 생성
            for (int chapterId = 1; chapterId <= 8; chapterId++)
            {
                string assetPath = $"{resourcesPath}/ChapterMonsterData_Chapter{chapterId}.asset";

                // 이미 존재하면 건너뛰기
                if (File.Exists(assetPath))
                {
                    Debug.Log($"Chapter {chapterId} 데이터가 이미 존재합니다. 건너뜁니다.");
                    continue;
                }

                // ChapterMonsterData 생성
                LostSpells.Data.ChapterMonsterData data = ScriptableObject.CreateInstance<LostSpells.Data.ChapterMonsterData>();

                // 챕터별로 다른 Dragon 스프라이트 할당
                Sprite dragonSprite = blueDragon;
                if (chapterId % 3 == 1)
                    dragonSprite = blueDragon;
                else if (chapterId % 3 == 2)
                    dragonSprite = greenDragon;
                else
                    dragonSprite = redDragon;

                // 데이터 설정
                data.chapterId = chapterId;
                data.monsterSprite = dragonSprite;
                data.monsterName = $"Dragon";
                data.baseHealth = 50 + (chapterId - 1) * 10; // 챕터마다 체력 10 증가
                data.baseSpeed = 2f + (chapterId - 1) * 0.1f; // 챕터마다 속도 0.1 증가

                // 에셋 저장
                AssetDatabase.CreateAsset(data, assetPath);
                Debug.Log($"✅ Chapter {chapterId} 몬스터 데이터 생성: {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("완료", "챕터 1~8의 몬스터 데이터가 생성되었습니다!\n\n경로: Assets/Resources/ChapterMonsters/", "OK");
        }
    }
}
