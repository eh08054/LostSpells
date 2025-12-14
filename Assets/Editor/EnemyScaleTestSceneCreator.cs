#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LostSpells.Test;

namespace LostSpells.Editor
{
    /// <summary>
    /// 적 크기 테스트 씬 생성 에디터 도구
    /// InGame 씬을 복사하여 테스트용으로 수정
    /// </summary>
    public class EnemyScaleTestSceneCreator : EditorWindow
    {
        [MenuItem("LostSpells/Create Enemy Scale Test Scene")]
        public static void CreateTestScene()
        {
            // InGame 씬 열기
            string inGameScenePath = "Assets/Scenes/InGame/InGame.unity";
            if (!System.IO.File.Exists(inGameScenePath))
            {
                EditorUtility.DisplayDialog("오류", "InGame 씬을 찾을 수 없습니다.\n경로: " + inGameScenePath, "확인");
                return;
            }

            // 현재 씬 저장 확인
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
            }

            // InGame 씬 열기
            EditorSceneManager.OpenScene(inGameScenePath);

            // ========== EnemySpawner에서 spawnHeight 가져오기 ==========
            float spawnHeight = 0f;
            var enemySpawner = Object.FindFirstObjectByType<LostSpells.Systems.EnemySpawner>();
            if (enemySpawner != null)
            {
                // SerializedObject를 통해 spawnHeight 값 가져오기
                SerializedObject spawnerSO = new SerializedObject(enemySpawner);
                var spawnHeightProp = spawnerSO.FindProperty("spawnHeight");
                if (spawnHeightProp != null)
                {
                    spawnHeight = spawnHeightProp.floatValue;
                    Debug.Log($"[EnemyScaleTest] EnemySpawner spawnHeight: {spawnHeight}");
                }

                // EnemySpawner 비활성화
                enemySpawner.enabled = false;
                Debug.Log("[EnemyScaleTest] EnemySpawner 비활성화");
            }

            // ========== Player 설정 ==========
            var player = GameObject.FindGameObjectWithTag("Player");
            float playerX = -3f;
            if (player != null)
            {
                var playerComponent = player.GetComponent<LostSpells.Components.PlayerComponent>();
                if (playerComponent != null)
                {
                    playerComponent.enabled = false;
                }
                var playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.bodyType = RigidbodyType2D.Kinematic;
                    playerRb.gravityScale = 0;
                }

                // 플레이어 위치 고정 (spawnHeight 사용)
                playerX = player.transform.position.x; // 기존 X 위치 유지
                if (playerX == 0) playerX = -3f; // 기본값
                player.transform.position = new Vector3(playerX, spawnHeight, 0);
                Debug.Log($"[EnemyScaleTest] Player 위치 고정: ({playerX}, {spawnHeight}, 0)");
            }

            // InGameUI의 게임 시작 로직 비활성화
            var inGameUI = Object.FindFirstObjectByType<LostSpells.UI.InGameUI>();
            if (inGameUI != null)
            {
                inGameUI.enabled = false;
                Debug.Log("[EnemyScaleTest] InGameUI 비활성화");
            }

            // VoiceRecognitionManager 비활성화
            var voiceManager = Object.FindFirstObjectByType<LostSpells.Systems.VoiceRecognitionManager>();
            if (voiceManager != null)
            {
                voiceManager.enabled = false;
                Debug.Log("[EnemyScaleTest] VoiceRecognitionManager 비활성화");
            }

            // ========== 적 크기 테스트 컨트롤러 추가 ==========
            GameObject controllerObj = new GameObject("EnemyScaleTestController");
            var testController = controllerObj.AddComponent<EnemyScaleTestController>();

            // 적 스폰 위치 설정 (플레이어 반대편, 같은 높이)
            float enemyX = -playerX; // 플레이어 반대편
            if (enemyX < 1f) enemyX = 3f; // 최소 3f

            SerializedObject so = new SerializedObject(testController);
            so.FindProperty("enemySpawnPosition").vector3Value = new Vector3(enemyX, spawnHeight, 0f);
            if (player != null)
            {
                so.FindProperty("playerPosition").objectReferenceValue = player.transform;
            }
            so.ApplyModifiedProperties();

            Debug.Log($"[EnemyScaleTest] 적 스폰 위치: ({enemyX}, {spawnHeight}, 0)");

            // ========== 안내 텍스트 ==========
            GameObject uiText = new GameObject("InstructionText");
            uiText.transform.position = new Vector3(0, 3.5f, 0);
            var instructionText = uiText.AddComponent<TextMesh>();
            instructionText.text = "[ Enemy Scale Test Mode ]\nInspector에서 EnemyScaleTestController 선택";
            instructionText.fontSize = 24;
            instructionText.characterSize = 0.1f;
            instructionText.anchor = TextAnchor.MiddleCenter;
            instructionText.alignment = TextAlignment.Center;
            instructionText.color = Color.yellow;

            // ========== 씬 저장 ==========
            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/Test"))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "Test");
            }

            string testScenePath = "Assets/Scenes/Test/EnemyScaleTest.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), testScenePath);

            Debug.Log($"[EnemyScaleTestSceneCreator] 테스트 씬 생성 완료: {testScenePath}");
            Debug.Log("[EnemyScaleTestSceneCreator] 플레이 모드에서 Inspector 버튼으로 적 변경/크기 조절");
            Debug.Log("[EnemyScaleTestSceneCreator] 'Save All to File' 버튼으로 JSON 저장");

            // 컨트롤러 선택
            Selection.activeGameObject = controllerObj;

            EditorUtility.DisplayDialog("완료",
                $"적 크기 테스트 씬이 생성되었습니다!\n\n" +
                $"spawnHeight: {spawnHeight}\n" +
                $"플레이어 위치: ({playerX}, {spawnHeight})\n" +
                $"적 스폰 위치: ({enemyX}, {spawnHeight})\n\n" +
                "1. Play 버튼을 눌러 플레이 모드 진입\n" +
                "2. EnemyScaleTestController 선택\n" +
                "3. Inspector에서 적 변경/크기 조절\n" +
                "4. 'Save All to File'로 저장",
                "확인");
        }
    }
}
#endif
