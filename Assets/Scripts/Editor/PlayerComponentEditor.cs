using UnityEngine;
using UnityEditor;
using LostSpells.Components;

namespace LostSpells.Editor
{
    [CustomEditor(typeof(PlayerComponent))]
    public class PlayerComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty playerName;
        private SerializedProperty maxHealth;
        private SerializedProperty currentHealth;
        private SerializedProperty moveSpeed;
        private SerializedProperty jumpForce;
        private SerializedProperty knockbackForce;
        private SerializedProperty spriteRenderer;
        private SerializedProperty playerColor;
        private SerializedProperty nameText;
        private SerializedProperty healthBarBackground;
        private SerializedProperty healthBarFill;

        private void OnEnable()
        {
            playerName = serializedObject.FindProperty("playerName");
            maxHealth = serializedObject.FindProperty("maxHealth");
            currentHealth = serializedObject.FindProperty("currentHealth");
            moveSpeed = serializedObject.FindProperty("moveSpeed");
            jumpForce = serializedObject.FindProperty("jumpForce");
            knockbackForce = serializedObject.FindProperty("knockbackForce");
            spriteRenderer = serializedObject.FindProperty("spriteRenderer");
            playerColor = serializedObject.FindProperty("playerColor");
            nameText = serializedObject.FindProperty("nameText");
            healthBarBackground = serializedObject.FindProperty("healthBarBackground");
            healthBarFill = serializedObject.FindProperty("healthBarFill");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Player Stats Header
            EditorGUILayout.LabelField("Player Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playerName);
            EditorGUILayout.PropertyField(maxHealth);

            // Current Health (0부터 Max Health까지 제한)
            int maxHealthValue = Mathf.Max(1, maxHealth.intValue);
            int newCurrentHealth = EditorGUILayout.IntField("Current Health", currentHealth.intValue);
            currentHealth.intValue = Mathf.Clamp(newCurrentHealth, 0, maxHealthValue);

            EditorGUILayout.PropertyField(moveSpeed);
            EditorGUILayout.PropertyField(jumpForce);
            EditorGUILayout.PropertyField(knockbackForce);

            EditorGUILayout.Space();

            // Visual Header
            EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spriteRenderer);
            EditorGUILayout.PropertyField(playerColor);

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
