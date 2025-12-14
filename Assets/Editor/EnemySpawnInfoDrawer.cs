#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using LostSpells.Data;

namespace LostSpells.Editor
{
    /// <summary>
    /// EnemySpawnInfo 커스텀 PropertyDrawer
    /// enemyPrefabName을 드롭다운으로 표시
    /// </summary>
    [CustomPropertyDrawer(typeof(EnemySpawnInfo))]
    public class EnemySpawnInfoDrawer : PropertyDrawer
    {
        private static string[] enemyNames;
        private static string[] enemyDisplayNames;

        /// <summary>
        /// 적 프리팹 목록 로드
        /// </summary>
        private static void LoadEnemyNames()
        {
            var prefabs = Resources.LoadAll<GameObject>("Enemies");
            enemyNames = prefabs.Select(p => p.name).OrderBy(n => n).ToArray();
            enemyDisplayNames = enemyNames.Select(n => n.Replace("Enemy", "")).ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 펼쳐진 상태일 때 모든 필드 높이 계산
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight * 7 + EditorGUIUtility.standardVerticalSpacing * 6;
            }
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 적 목록 로드
            if (enemyNames == null || enemyNames.Length == 0)
            {
                LoadEnemyNames();
            }

            EditorGUI.BeginProperty(position, label, property);

            // 프로퍼티 가져오기
            SerializedProperty enemyPrefabNameProp = property.FindPropertyRelative("enemyPrefabName");
            SerializedProperty spawnSideProp = property.FindPropertyRelative("spawnSide");
            SerializedProperty countProp = property.FindPropertyRelative("count");
            SerializedProperty spawnIntervalProp = property.FindPropertyRelative("spawnInterval");
            SerializedProperty healthBonusProp = property.FindPropertyRelative("healthBonus");
            SerializedProperty speedBonusProp = property.FindPropertyRelative("speedBonus");

            // Foldout 레이블
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            string currentName = string.IsNullOrEmpty(enemyPrefabNameProp.stringValue)
                ? "(None)"
                : enemyPrefabNameProp.stringValue.Replace("Enemy", "");
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"{label.text}: {currentName}", true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float y = position.y + lineHeight + spacing;

                // Enemy Prefab 드롭다운
                Rect enemyRect = new Rect(position.x, y, position.width, lineHeight);
                if (enemyNames != null && enemyNames.Length > 0)
                {
                    int currentIndex = System.Array.IndexOf(enemyNames, enemyPrefabNameProp.stringValue);
                    if (currentIndex < 0) currentIndex = 0;

                    int newIndex = EditorGUI.Popup(enemyRect, "Enemy Prefab", currentIndex, enemyDisplayNames);
                    if (newIndex != currentIndex || string.IsNullOrEmpty(enemyPrefabNameProp.stringValue))
                    {
                        enemyPrefabNameProp.stringValue = enemyNames[newIndex];
                    }
                }
                else
                {
                    EditorGUI.PropertyField(enemyRect, enemyPrefabNameProp, new GUIContent("Enemy Prefab"));
                }
                y += lineHeight + spacing;

                // Spawn Side
                Rect sideRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.PropertyField(sideRect, spawnSideProp);
                y += lineHeight + spacing;

                // Count
                Rect countRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.PropertyField(countRect, countProp);
                y += lineHeight + spacing;

                // Spawn Interval
                Rect intervalRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.PropertyField(intervalRect, spawnIntervalProp);
                y += lineHeight + spacing;

                // Health Bonus
                Rect healthRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.PropertyField(healthRect, healthBonusProp);
                y += lineHeight + spacing;

                // Speed Bonus
                Rect speedRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.PropertyField(speedRect, speedBonusProp);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 적 목록 새로고침 (메뉴에서 호출 가능)
        /// </summary>
        [MenuItem("LostSpells/Refresh Enemy List Cache")]
        public static void RefreshEnemyList()
        {
            enemyNames = null;
            enemyDisplayNames = null;
            LoadEnemyNames();
            Debug.Log($"[EnemySpawnInfoDrawer] 적 목록 새로고침: {enemyNames.Length}개");
        }
    }
}
#endif
