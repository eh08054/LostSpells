using UnityEngine;
using UnityEditor;
using LostSpells.Test;

namespace LostSpells.Editor
{
    /// <summary>
    /// EnemyScaleTestController 커스텀 에디터
    /// 버튼으로 적 변경, 크기 조절, 저장 기능 제공
    /// </summary>
    [CustomEditor(typeof(EnemyScaleTestController))]
    public class EnemyScaleTestControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty currentEnemyIndex;
        private SerializedProperty currentScale;
        private SerializedProperty currentHealthBarHeight;
        private SerializedProperty enemySpawnPosition;
        private SerializedProperty playerPosition;
        private SerializedProperty currentEnemyName;
        private SerializedProperty totalEnemyCount;

        private void OnEnable()
        {
            currentEnemyIndex = serializedObject.FindProperty("currentEnemyIndex");
            currentScale = serializedObject.FindProperty("currentScale");
            currentHealthBarHeight = serializedObject.FindProperty("currentHealthBarHeight");
            enemySpawnPosition = serializedObject.FindProperty("enemySpawnPosition");
            playerPosition = serializedObject.FindProperty("playerPosition");
            currentEnemyName = serializedObject.FindProperty("currentEnemyName");
            totalEnemyCount = serializedObject.FindProperty("totalEnemyCount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var controller = (EnemyScaleTestController)target;

            // 현재 상태 표시
            EditorGUILayout.LabelField("Current Enemy Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(currentEnemyName, new GUIContent("Enemy Name"));
            EditorGUILayout.IntField("Index", currentEnemyIndex.intValue);
            EditorGUILayout.IntField("Total Enemies", totalEnemyCount.intValue);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // 적 네비게이션 버튼
            EditorGUILayout.LabelField("Enemy Navigation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("◀ Previous", GUILayout.Height(30)))
            {
                controller.PreviousEnemy();
            }

            if (GUILayout.Button("Next ▶", GUILayout.Height(30)))
            {
                controller.NextEnemy();
            }

            EditorGUILayout.EndHorizontal();

            // 인덱스 직접 입력
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.IntSlider("Enemy Index", currentEnemyIndex.intValue, 0, Mathf.Max(0, totalEnemyCount.intValue - 1));
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                currentEnemyIndex.intValue = newIndex;
                serializedObject.ApplyModifiedProperties();
                controller.SpawnCurrentEnemy();
            }

            EditorGUILayout.Space(10);

            // 스케일 조절
            EditorGUILayout.LabelField("Scale Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            float newScale = EditorGUILayout.Slider("Current Scale", currentScale.floatValue, 0.1f, 5.0f);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                currentScale.floatValue = newScale;
                serializedObject.ApplyModifiedProperties();
                controller.OnScaleChanged(newScale);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("-0.1", GUILayout.Height(25)))
            {
                controller.DecreaseScale(0.1f);
                serializedObject.Update(); // 컨트롤러에서 변경된 값 읽기
            }

            if (GUILayout.Button("-0.5", GUILayout.Height(25)))
            {
                controller.DecreaseScale(0.5f);
                serializedObject.Update();
            }

            if (GUILayout.Button("+0.1", GUILayout.Height(25)))
            {
                controller.IncreaseScale(0.1f);
                serializedObject.Update();
            }

            if (GUILayout.Button("+0.5", GUILayout.Height(25)))
            {
                controller.IncreaseScale(0.5f);
                serializedObject.Update();
            }

            EditorGUILayout.EndHorizontal();

            // 프리셋 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x")) { SetScale(controller, 0.5f); }
            if (GUILayout.Button("1.0x")) { SetScale(controller, 1.0f); }
            if (GUILayout.Button("1.5x")) { SetScale(controller, 1.5f); }
            if (GUILayout.Button("2.0x")) { SetScale(controller, 2.0f); }
            if (GUILayout.Button("2.5x")) { SetScale(controller, 2.5f); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 체력바 높이 조절
            EditorGUILayout.LabelField("Health Bar Height", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            float newHeight = EditorGUILayout.Slider("Current Height", currentHealthBarHeight.floatValue, 0.5f, 5.0f);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                currentHealthBarHeight.floatValue = newHeight;
                serializedObject.ApplyModifiedProperties();
                controller.OnHealthBarHeightChanged(newHeight);
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("-0.1", GUILayout.Height(25)))
            {
                controller.DecreaseHealthBarHeight(0.1f);
                serializedObject.Update();
            }

            if (GUILayout.Button("-0.5", GUILayout.Height(25)))
            {
                controller.DecreaseHealthBarHeight(0.5f);
                serializedObject.Update();
            }

            if (GUILayout.Button("+0.1", GUILayout.Height(25)))
            {
                controller.IncreaseHealthBarHeight(0.1f);
                serializedObject.Update();
            }

            if (GUILayout.Button("+0.5", GUILayout.Height(25)))
            {
                controller.IncreaseHealthBarHeight(0.5f);
                serializedObject.Update();
            }

            EditorGUILayout.EndHorizontal();

            // 체력바 높이 프리셋 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1.0")) { SetHealthBarHeight(controller, 1.0f); }
            if (GUILayout.Button("1.5")) { SetHealthBarHeight(controller, 1.5f); }
            if (GUILayout.Button("2.0")) { SetHealthBarHeight(controller, 2.0f); }
            if (GUILayout.Button("2.5")) { SetHealthBarHeight(controller, 2.5f); }
            if (GUILayout.Button("3.0")) { SetHealthBarHeight(controller, 3.0f); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 자동 저장 안내
            EditorGUILayout.HelpBox("Scale과 Health Bar Height 변경 시 자동 저장됩니다.", MessageType.Info);

            EditorGUILayout.Space(10);

            // 기타 설정
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enemySpawnPosition);
            EditorGUILayout.PropertyField(playerPosition);

            serializedObject.ApplyModifiedProperties();
        }

        private void SetScale(EnemyScaleTestController controller, float scale)
        {
            currentScale.floatValue = scale;
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying)
            {
                controller.OnScaleChanged(scale);
            }
        }

        private void SetHealthBarHeight(EnemyScaleTestController controller, float height)
        {
            currentHealthBarHeight.floatValue = height;
            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying)
            {
                controller.OnHealthBarHeightChanged(height);
            }
        }
    }
}
