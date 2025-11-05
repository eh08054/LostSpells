using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LostSpells.Components;
using LostSpells.Data.Save;

namespace LostSpells.Editor
{
    /// <summary>
    /// 기존 GameManager 오브젝트에 컴포넌트 추가 에디터 유틸리티
    /// </summary>
    public static class DataManagerSetup
    {
        [MenuItem("LostSpells/Setup/0. Remove Missing Scripts")]
        public static void RemoveMissingScripts()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("오류",
                    "Play 모드에서는 실행할 수 없습니다.\nPlay 모드를 종료하고 다시 시도하세요.",
                    "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog("Missing 스크립트 제거",
                "모든 씬의 GameManager에서 Missing 스크립트를 제거하시겠습니까?",
                "제거", "취소"))
            {
                return;
            }

            int removedCount = 0;

            RemoveMissingScriptsFromScene("Assets/UI/MainMenu/MainMenu.unity", ref removedCount);
            RemoveMissingScriptsFromScene("Assets/UI/GameModeSelection/GameModeSelection.unity", ref removedCount);
            RemoveMissingScriptsFromScene("Assets/UI/ChapterSelect/ChapterSelect.unity", ref removedCount);
            RemoveMissingScriptsFromScene("Assets/UI/EndlessMode/EndlessMode.unity", ref removedCount);
            RemoveMissingScriptsFromScene("Assets/UI/Options/Options.unity", ref removedCount);
            RemoveMissingScriptsFromScene("Assets/UI/StoryMode/StoryMode.unity", ref removedCount);
            RemoveMissingScriptsFromScene("Assets/UI/InGame/InGame.unity", ref removedCount);
            RemoveMissingScriptsFromScene("Assets/UI/Store/Store.unity", ref removedCount);

            EditorUtility.DisplayDialog("완료",
                $"Missing 스크립트 제거 완료\n처리된 씬: {removedCount}개",
                "확인");
        }

        [MenuItem("LostSpells/Setup/1. Remove Old DataManager Objects")]
        public static void RemoveOldDataManagers()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("오류",
                    "Play 모드에서는 실행할 수 없습니다.\nPlay 모드를 종료하고 다시 시도하세요.",
                    "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog("구형 오브젝트 제거",
                "모든 씬의 구형 DataManager 오브젝트들을 제거하시겠습니까?",
                "제거", "취소"))
            {
                return;
            }

            int removedCount = 0;

            RemoveOldDataManagersFromScene("Assets/UI/MainMenu/MainMenu.unity", ref removedCount);
            RemoveOldDataManagersFromScene("Assets/UI/GameModeSelection/GameModeSelection.unity", ref removedCount);
            RemoveOldDataManagersFromScene("Assets/UI/ChapterSelect/ChapterSelect.unity", ref removedCount);
            RemoveOldDataManagersFromScene("Assets/UI/EndlessMode/EndlessMode.unity", ref removedCount);
            RemoveOldDataManagersFromScene("Assets/UI/Options/Options.unity", ref removedCount);
            RemoveOldDataManagersFromScene("Assets/UI/StoryMode/StoryMode.unity", ref removedCount);
            RemoveOldDataManagersFromScene("Assets/UI/InGame/InGame.unity", ref removedCount);
            RemoveOldDataManagersFromScene("Assets/UI/Store/Store.unity", ref removedCount);

            EditorUtility.DisplayDialog("완료",
                $"구형 오브젝트 제거 완료\n제거된 오브젝트: {removedCount}개",
                "확인");
        }

        [MenuItem("LostSpells/Setup/2. Add Components to GameManager")]
        public static void AddComponentsToGameManager()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("오류",
                    "Play 모드에서는 실행할 수 없습니다.\nPlay 모드를 종료하고 다시 시도하세요.",
                    "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog("컴포넌트 추가",
                "각 씬의 GameManager에 필요한 컴포넌트를 추가하시겠습니까?",
                "추가", "취소"))
            {
                return;
            }

            int addedCount = 0;

            // StoryMode 씬: SlotInfoComponent만
            AddComponentsToScene(
                "Assets/UI/StoryMode/StoryMode.unity",
                new System.Type[] { typeof(SlotInfoComponent) },
                ref addedCount
            );

            // InGame 씬: 모든 컴포넌트
            AddComponentsToScene(
                "Assets/UI/InGame/InGame.unity",
                new System.Type[] {
                    typeof(GameStateInfoComponent),
                    typeof(PlayerStatsComponent),
                    typeof(CurrencyComponent),
                    typeof(ProgressComponent)
                },
                ref addedCount
            );

            // Store 씬: CurrencyComponent만
            AddComponentsToScene(
                "Assets/UI/Store/Store.unity",
                new System.Type[] { typeof(CurrencyComponent) },
                ref addedCount
            );

            EditorUtility.DisplayDialog("완료",
                $"컴포넌트 추가 완료\n처리된 씬: {addedCount}개",
                "확인");
        }

        [MenuItem("LostSpells/Setup/3. Remove Components from GameManager")]
        public static void RemoveComponentsFromGameManager()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("오류",
                    "Play 모드에서는 실행할 수 없습니다.\nPlay 모드를 종료하고 다시 시도하세요.",
                    "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog("컴포넌트 제거",
                "모든 씬의 GameManager에서 데이터 컴포넌트를 제거하시겠습니까?",
                "제거", "취소"))
            {
                return;
            }

            int removedCount = 0;

            RemoveComponentsFromScene("Assets/UI/MainMenu/MainMenu.unity", ref removedCount);
            RemoveComponentsFromScene("Assets/UI/GameModeSelection/GameModeSelection.unity", ref removedCount);
            RemoveComponentsFromScene("Assets/UI/ChapterSelect/ChapterSelect.unity", ref removedCount);
            RemoveComponentsFromScene("Assets/UI/EndlessMode/EndlessMode.unity", ref removedCount);
            RemoveComponentsFromScene("Assets/UI/Options/Options.unity", ref removedCount);
            RemoveComponentsFromScene("Assets/UI/StoryMode/StoryMode.unity", ref removedCount);
            RemoveComponentsFromScene("Assets/UI/InGame/InGame.unity", ref removedCount);
            RemoveComponentsFromScene("Assets/UI/Store/Store.unity", ref removedCount);

            EditorUtility.DisplayDialog("완료",
                $"컴포넌트 제거 완료\n처리된 씬: {removedCount}개",
                "확인");
        }

        private static void RemoveMissingScriptsFromScene(string scenePath, ref int count)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject gameManager = GameObject.Find("GameManager");

            if (gameManager != null)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameManager);
                if (removed > 0)
                {
                    Debug.Log($"[Setup] {scene.name}의 GameManager에서 Missing 스크립트 {removed}개 제거");
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    count++;
                }
            }

            Debug.Log($"[Setup] {scene.name} 씬 처리 완료");
        }

        private static void RemoveOldDataManagersFromScene(string scenePath, ref int count)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            string[] oldManagerNames = new[] {
                "StoryModeDataManager",
                "InGameDataManager",
                "StoreDataManager"
            };

            bool objectsRemoved = false;
            foreach (var name in oldManagerNames)
            {
                GameObject oldManager = GameObject.Find(name);
                if (oldManager != null)
                {
                    Debug.Log($"[Setup] {scene.name}에서 {name} 오브젝트 제거");
                    GameObject.DestroyImmediate(oldManager);
                    objectsRemoved = true;
                    count++;
                }
            }

            if (objectsRemoved)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[Setup] {scene.name} 씬 처리 완료");
        }

        private static void AddComponentsToScene(string scenePath, System.Type[] componentTypes, ref int count)
        {
            Debug.Log($"[Setup] {scenePath} 씬 열기 시도...");
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[Setup] {scene.name} 씬 로드 완료");

            // 기존 GameManager 찾기
            GameObject gameManager = GameObject.Find("GameManager");
            Debug.Log($"[Setup] GameManager 검색 결과: {(gameManager != null ? "찾음" : "없음")}");

            if (gameManager == null)
            {
                Debug.LogWarning($"[Setup] {scene.name} 씬에 GameManager가 없습니다. 먼저 GameManager를 추가하세요.");
                return;
            }

            // 필요한 컴포넌트들 추가
            bool componentsAdded = false;
            foreach (var componentType in componentTypes)
            {
                if (gameManager.GetComponent(componentType) == null)
                {
                    gameManager.AddComponent(componentType);
                    Debug.Log($"[Setup] {scene.name}의 GameManager에 {componentType.Name} 추가됨");
                    componentsAdded = true;
                }
                else
                {
                    Debug.Log($"[Setup] {scene.name}의 GameManager에 {componentType.Name}이 이미 있습니다");
                }
            }

            if (componentsAdded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                count++;
            }

            Debug.Log($"[Setup] {scene.name} 씬 처리 완료");
        }

        private static void RemoveComponentsFromScene(string scenePath, ref int count)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject gameManager = GameObject.Find("GameManager");

            if (gameManager == null)
            {
                Debug.LogWarning($"[Setup] {scene.name} 씬에 GameManager가 없습니다.");
                return;
            }

            // 데이터 컴포넌트들만 제거
            var componentTypes = new System.Type[] {
                typeof(SlotInfoComponent),
                typeof(GameStateInfoComponent),
                typeof(PlayerStatsComponent),
                typeof(CurrencyComponent),
                typeof(ProgressComponent)
            };

            bool componentsRemoved = false;
            foreach (var componentType in componentTypes)
            {
                var component = gameManager.GetComponent(componentType);
                if (component != null)
                {
                    Object.DestroyImmediate(component);
                    Debug.Log($"[Setup] {scene.name}의 GameManager에서 {componentType.Name} 제거됨");
                    componentsRemoved = true;
                }
            }

            if (componentsRemoved)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                count++;
            }

            Debug.Log($"[Setup] {scene.name} 씬 처리 완료");
        }
    }
}
