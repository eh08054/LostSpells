using UnityEngine;
using UnityEditor;
using LostSpells.Components;

namespace LostSpells.Editor
{
    [CustomEditor(typeof(EnemyComponent))]
    public class EnemyComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty enemyName;
        private SerializedProperty maxHealth;
        private SerializedProperty currentHealth;
        private SerializedProperty moveSpeed;
        private SerializedProperty spriteRenderer;
        private SerializedProperty enemyColor;
        private SerializedProperty nameText;
        private SerializedProperty healthBarBackground;
        private SerializedProperty healthBarFill;

        private void OnEnable()
        {
            enemyName = serializedObject.FindProperty("enemyName");
            maxHealth = serializedObject.FindProperty("maxHealth");
            currentHealth = serializedObject.FindProperty("currentHealth");
            moveSpeed = serializedObject.FindProperty("moveSpeed");
            spriteRenderer = serializedObject.FindProperty("spriteRenderer");
            enemyColor = serializedObject.FindProperty("enemyColor");
            nameText = serializedObject.FindProperty("nameText");
            healthBarBackground = serializedObject.FindProperty("healthBarBackground");
            healthBarFill = serializedObject.FindProperty("healthBarFill");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Enemy Stats Header
            EditorGUILayout.LabelField("Enemy Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enemyName);
            EditorGUILayout.PropertyField(maxHealth);

            // Current Health (0부터 Max Health까지 제한)
            int maxHealthValue = Mathf.Max(1, maxHealth.intValue);
            int newCurrentHealth = EditorGUILayout.IntField("Current Health", currentHealth.intValue);
            currentHealth.intValue = Mathf.Clamp(newCurrentHealth, 0, maxHealthValue);

            EditorGUILayout.PropertyField(moveSpeed);

            EditorGUILayout.Space();

            // Visual Header
            EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spriteRenderer);
            EditorGUILayout.PropertyField(enemyColor);

            EditorGUILayout.Space();

            // UI Elements Header
            EditorGUILayout.LabelField("UI Elements", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nameText);
            EditorGUILayout.PropertyField(healthBarBackground);
            EditorGUILayout.PropertyField(healthBarFill);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
