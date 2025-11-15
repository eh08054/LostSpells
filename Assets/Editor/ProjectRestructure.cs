using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 프로젝트 구조 재구성을 위한 Unity Editor 스크립트
/// Unity 메뉴: Tools > Restructure Project
/// </summary>
public class ProjectRestructure : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showMigrationPlan = true;
    private bool backupCreated = false;

    [MenuItem("Tools/Restructure Project")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProjectRestructure>("Project Restructure");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Lost Spells - Project Restructure", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "이 도구는 프로젝트 구조를 재구성합니다.\n" +
            "실행 전 반드시 프로젝트를 백업하거나 Git commit을 하세요!",
            MessageType.Warning);

        EditorGUILayout.Space(10);

        // 백업 상태 표시
        if (backupCreated)
        {
            EditorGUILayout.HelpBox("✓ Git 백업이 권장됩니다.", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // 마이그레이션 계획 표시
        showMigrationPlan = EditorGUILayout.Foldout(showMigrationPlan, "마이그레이션 계획 보기", true);
        if (showMigrationPlan)
        {
            EditorGUILayout.TextArea(GetMigrationPlan(), GUILayout.Height(250));
        }

        EditorGUILayout.Space(10);

        // 실행 버튼
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("1. 폴더 구조 생성", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("폴더 생성 확인",
                "새로운 폴더 구조를 생성하시겠습니까?",
                "실행", "취소"))
            {
                CreateFolderStructure();
            }
        }

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("2. 파일 이동 실행", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("파일 이동 확인",
                "파일들을 새로운 위치로 이동하시겠습니까?\n이 작업은 되돌릴 수 없습니다!",
                "실행", "취소"))
            {
                MigrateFiles();
            }
        }

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("3. 빈 폴더 정리", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("폴더 정리 확인",
                "빈 폴더를 삭제하시겠습니까?",
                "실행", "취소"))
            {
                CleanupEmptyFolders();
            }
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "권장 순서:\n" +
            "1. Git commit 또는 프로젝트 백업\n" +
            "2. '폴더 구조 생성' 버튼 클릭\n" +
            "3. '파일 이동 실행' 버튼 클릭\n" +
            "4. Unity 에디터 재시작\n" +
            "5. 컴파일 에러 확인\n" +
            "6. '빈 폴더 정리' 버튼 클릭",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private string GetMigrationPlan()
    {
        return @"=== 마이그레이션 계획 ===

[1] 씬 UI 파일 재구성
    Scenes/*/[파일].uxml,uss → Scenes/*/UI/
    Scenes/*/[UI].cs → Scenes/*/Scripts/

[2] 게임플레이 컴포넌트 → InGame
    Scripts/Components/* → Scenes/InGame/Scripts/
    Scripts/Systems/EnemySpawner.cs → Scenes/InGame/Scripts/

[3] 스킬 시스템
    Scripts/Components/Skill*.cs → Scenes/InGame/Scripts/Skills/

[4] 에디터 스크립트
    Scripts/Editor/* → Scenes/InGame/Editor/

[5] 데이터 모델
    Data/GameConfig/Scripts/* → Data/Models/
    Data/SaveData/* → Data/Models/

[6] 전역 시스템
    Scripts/Systems/* → Core/
    음성 관련 → Core/Voice/

[7] 리소스
    Resources/GameData/*.json → Data/Resources/

[8] 템플릿 에셋
    2D Casual UI → Templates/
    Fantasy Wooden GUI Free → Templates/
    Gothicvania-Town → Templates/
";
    }

    private void CreateFolderStructure()
    {
        Debug.Log("=== 폴더 구조 생성 시작 ===");

        List<string> folders = new List<string>
        {
            // Scenes 폴더 구조
            "Assets/Scenes/Common/UI",
            "Assets/Scenes/Common/Scripts",
            "Assets/Scenes/MainMenu/UI",
            "Assets/Scenes/MainMenu/Scripts",
            "Assets/Scenes/GameModeSelection/UI",
            "Assets/Scenes/GameModeSelection/Scripts",
            "Assets/Scenes/StoryMode/UI",
            "Assets/Scenes/StoryMode/Scripts",
            "Assets/Scenes/EndlessMode/UI",
            "Assets/Scenes/EndlessMode/Scripts",
            "Assets/Scenes/InGame/UI",
            "Assets/Scenes/InGame/Scripts",
            "Assets/Scenes/InGame/Scripts/Skills",
            "Assets/Scenes/InGame/Editor",
            "Assets/Scenes/Options/UI",
            "Assets/Scenes/Options/Scripts",
            "Assets/Scenes/Store/UI",
            "Assets/Scenes/Store/Scripts",

            // Data 폴더 구조
            "Assets/Data/Models",
            "Assets/Data/Resources",

            // Core 폴더 구조
            "Assets/Core",
            "Assets/Core/Voice",

            // Templates 폴더
            "Assets/Templates"
        };

        foreach (var folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parentFolder = Path.GetDirectoryName(folder).Replace("\\", "/");
                string newFolderName = Path.GetFileName(folder);

                AssetDatabase.CreateFolder(parentFolder, newFolderName);
                Debug.Log($"✓ 폴더 생성: {folder}");
            }
            else
            {
                Debug.Log($"○ 폴더 존재: {folder}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== 폴더 구조 생성 완료 ===");
        EditorUtility.DisplayDialog("완료", "폴더 구조가 생성되었습니다!", "확인");
    }

    private void MigrateFiles()
    {
        Debug.Log("=== 파일 마이그레이션 시작 ===");

        int movedCount = 0;
        int errorCount = 0;

        // 마이그레이션 맵: [현재 경로] -> [새 경로]
        Dictionary<string, string> migrationMap = new Dictionary<string, string>
        {
            // === 씬 UI 파일 ===
            // Common
            {"Assets/Scenes/Common/Common.uss", "Assets/Scenes/Common/UI/Common.uss"},

            // MainMenu
            {"Assets/Scenes/MainMenu/MainMenu.uxml", "Assets/Scenes/MainMenu/UI/MainMenu.uxml"},
            {"Assets/Scenes/MainMenu/MainMenu.uss", "Assets/Scenes/MainMenu/UI/MainMenu.uss"},
            {"Assets/Scenes/MainMenu/MainMenuUI.cs", "Assets/Scenes/MainMenu/Scripts/MainMenuUI.cs"},

            // GameModeSelection
            {"Assets/Scenes/GameModeSelection/GameModeSelection.uxml", "Assets/Scenes/GameModeSelection/UI/GameModeSelection.uxml"},
            {"Assets/Scenes/GameModeSelection/GameModeSelection.uss", "Assets/Scenes/GameModeSelection/UI/GameModeSelection.uss"},
            {"Assets/Scenes/GameModeSelection/GameModeSelectionUI.cs", "Assets/Scenes/GameModeSelection/Scripts/GameModeSelectionUI.cs"},

            // StoryMode
            {"Assets/Scenes/StoryMode/StoryMode.uxml", "Assets/Scenes/StoryMode/UI/StoryMode.uxml"},
            {"Assets/Scenes/StoryMode/StoryMode.uss", "Assets/Scenes/StoryMode/UI/StoryMode.uss"},
            {"Assets/Scenes/StoryMode/StoryModeUI.cs", "Assets/Scenes/StoryMode/Scripts/StoryModeUI.cs"},

            // EndlessMode
            {"Assets/Scenes/EndlessMode/EndlessMode.uxml", "Assets/Scenes/EndlessMode/UI/EndlessMode.uxml"},
            {"Assets/Scenes/EndlessMode/EndlessMode.uss", "Assets/Scenes/EndlessMode/UI/EndlessMode.uss"},
            {"Assets/Scenes/EndlessMode/EndlessModeUI.cs", "Assets/Scenes/EndlessMode/Scripts/EndlessModeUI.cs"},

            // InGame
            {"Assets/Scenes/InGame/InGame.uxml", "Assets/Scenes/InGame/UI/InGame.uxml"},
            {"Assets/Scenes/InGame/InGame.uss", "Assets/Scenes/InGame/UI/InGame.uss"},
            {"Assets/Scenes/InGame/InGameUI.cs", "Assets/Scenes/InGame/Scripts/InGameUI.cs"},

            // Options
            {"Assets/Scenes/Options/Options.uxml", "Assets/Scenes/Options/UI/Options.uxml"},
            {"Assets/Scenes/Options/Options.uss", "Assets/Scenes/Options/UI/Options.uss"},
            {"Assets/Scenes/Options/OptionsUI.cs", "Assets/Scenes/Options/Scripts/OptionsUI.cs"},

            // Store
            {"Assets/Scenes/Store/Store.uxml", "Assets/Scenes/Store/UI/Store.uxml"},
            {"Assets/Scenes/Store/Store.uss", "Assets/Scenes/Store/UI/Store.uss"},
            {"Assets/Scenes/Store/StoreUI.cs", "Assets/Scenes/Store/Scripts/StoreUI.cs"},

            // === 게임플레이 컴포넌트 → InGame ===
            {"Assets/Scripts/Components/PlayerComponent.cs", "Assets/Scenes/InGame/Scripts/PlayerComponent.cs"},
            {"Assets/Scripts/Components/EnemyComponent.cs", "Assets/Scenes/InGame/Scripts/EnemyComponent.cs"},
            {"Assets/Scripts/Systems/EnemySpawner.cs", "Assets/Scenes/InGame/Scripts/EnemySpawner.cs"},

            // === 스킬 시스템 ===
            {"Assets/Scripts/Components/SkillBehavior.cs", "Assets/Scenes/InGame/Scripts/Skills/SkillBehavior.cs"},
            {"Assets/Scripts/Components/ProjectileSkillBehavior.cs", "Assets/Scenes/InGame/Scripts/Skills/ProjectileSkillBehavior.cs"},
            {"Assets/Scripts/Components/InstantSkillBehavior.cs", "Assets/Scenes/InGame/Scripts/Skills/InstantSkillBehavior.cs"},
            {"Assets/Scripts/Components/ShieldSkillBehavior.cs", "Assets/Scenes/InGame/Scripts/Skills/ShieldSkillBehavior.cs"},

            // === 에디터 스크립트 ===
            {"Assets/Scripts/Editor/PlayerComponentEditor.cs", "Assets/Scenes/InGame/Editor/PlayerComponentEditor.cs"},
            {"Assets/Scripts/Editor/EnemyComponentEditor.cs", "Assets/Scenes/InGame/Editor/EnemyComponentEditor.cs"},

            // === 데이터 모델 ===
            {"Assets/Data/GameConfig/Scripts/ChapterData.cs", "Assets/Data/Models/ChapterData.cs"},
            {"Assets/Data/GameConfig/Scripts/MonsterData.cs", "Assets/Data/Models/MonsterData.cs"},
            {"Assets/Data/GameConfig/Scripts/SkillData.cs", "Assets/Data/Models/SkillData.cs"},
            {"Assets/Data/GameConfig/Scripts/StoreItemData.cs", "Assets/Data/Models/StoreItemData.cs"},
            {"Assets/Data/GameConfig/Scripts/WaveConfig.cs", "Assets/Data/Models/WaveConfig.cs"},
            {"Assets/Data/SaveData/PlayerSaveData.cs", "Assets/Data/Models/PlayerSaveData.cs"},

            // === 전역 시스템 → Core ===
            {"Assets/Scripts/Systems/GameStateManager.cs", "Assets/Core/GameStateManager.cs"},
            {"Assets/Scripts/Systems/LocalizationManager.cs", "Assets/Core/LocalizationManager.cs"},
            {"Assets/Scripts/Systems/SceneNavigationManager.cs", "Assets/Core/SceneNavigationManager.cs"},
            {"Assets/Scripts/Systems/VoiceRecognitionManager.cs", "Assets/Core/Voice/VoiceRecognitionManager.cs"},
            {"Assets/Scripts/Systems/VoiceRecorder.cs", "Assets/Core/Voice/VoiceRecorder.cs"},
            {"Assets/Scripts/Systems/VoiceServerClient.cs", "Assets/Core/Voice/VoiceServerClient.cs"},

            // === 리소스 ===
            {"Assets/Resources/GameData/Chapters.json", "Assets/Data/Resources/Chapters.json"},
            {"Assets/Resources/GameData/Monsters.json", "Assets/Data/Resources/Monsters.json"},
            {"Assets/Resources/GameData/Skills.json", "Assets/Data/Resources/Skills.json"},
        };

        foreach (var migration in migrationMap)
        {
            string oldPath = migration.Key;
            string newPath = migration.Value;

            if (File.Exists(oldPath) || Directory.Exists(oldPath))
            {
                string result = AssetDatabase.MoveAsset(oldPath, newPath);
                if (string.IsNullOrEmpty(result))
                {
                    Debug.Log($"✓ 이동 완료: {Path.GetFileName(oldPath)} → {newPath}");
                    movedCount++;
                }
                else
                {
                    Debug.LogError($"✗ 이동 실패: {oldPath} → {result}");
                    errorCount++;
                }
            }
            else
            {
                Debug.LogWarning($"○ 파일 없음: {oldPath}");
            }
        }

        // === 템플릿 폴더 이동 (폴더 전체) ===
        MoveFolder("Assets/2D Casual UI", "Assets/Templates/2D Casual UI", ref movedCount, ref errorCount);
        MoveFolder("Assets/Fantasy Wooden GUI Free", "Assets/Templates/Fantasy Wooden GUI Free", ref movedCount, ref errorCount);
        MoveFolder("Assets/Gothicvania-Town", "Assets/Templates/Gothicvania-Town", ref movedCount, ref errorCount);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"=== 파일 마이그레이션 완료 ===");
        Debug.Log($"이동 성공: {movedCount}개, 실패: {errorCount}개");

        EditorUtility.DisplayDialog("완료",
            $"파일 마이그레이션이 완료되었습니다!\n\n이동 성공: {movedCount}개\n실패: {errorCount}개\n\nUnity 에디터를 재시작하세요.",
            "확인");
    }

    private void MoveFolder(string oldPath, string newPath, ref int movedCount, ref int errorCount)
    {
        if (AssetDatabase.IsValidFolder(oldPath))
        {
            string result = AssetDatabase.MoveAsset(oldPath, newPath);
            if (string.IsNullOrEmpty(result))
            {
                Debug.Log($"✓ 폴더 이동 완료: {oldPath} → {newPath}");
                movedCount++;
            }
            else
            {
                Debug.LogError($"✗ 폴더 이동 실패: {oldPath} → {result}");
                errorCount++;
            }
        }
        else
        {
            Debug.LogWarning($"○ 폴더 없음: {oldPath}");
        }
    }

    private void CleanupEmptyFolders()
    {
        Debug.Log("=== 빈 폴더 정리 시작 ===");

        List<string> foldersToDelete = new List<string>
        {
            "Assets/Scripts/Components",
            "Assets/Scripts/Editor",
            "Assets/Scripts/Systems",
            "Assets/Scripts",
            "Assets/Data/GameConfig/Scripts",
            "Assets/Data/GameConfig",
            "Assets/Data/SaveData",
            "Assets/Resources/GameData",
        };

        int deletedCount = 0;

        foreach (var folder in foldersToDelete)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                if (IsFolderEmpty(folder))
                {
                    AssetDatabase.DeleteAsset(folder);
                    Debug.Log($"✓ 폴더 삭제: {folder}");
                    deletedCount++;
                }
                else
                {
                    Debug.LogWarning($"○ 폴더가 비어있지 않음: {folder}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"=== 빈 폴더 정리 완료 ({deletedCount}개 삭제) ===");
        EditorUtility.DisplayDialog("완료", $"{deletedCount}개의 빈 폴더가 삭제되었습니다!", "확인");
    }

    private bool IsFolderEmpty(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        return guids.Length == 0;
    }
}
