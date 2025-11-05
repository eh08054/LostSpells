using UnityEngine;
using UnityEditor;
using LostSpells.Data.Save;

namespace LostSpells.Editor
{
    [CustomEditor(typeof(EndlessModeRankingComponent))]
    public class EndlessModeRankingComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 Inspector 그리기
            DrawDefaultInspector();

            EndlessModeRankingComponent component = (EndlessModeRankingComponent)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            // 버튼을 한 줄에 가로로 배치
            EditorGUILayout.BeginHorizontal();

            // Sample 버튼
            if (GUILayout.Button("Sample", GUILayout.Height(30)))
            {
                component.GenerateSample();
            }

            // Clear 버튼
            if (GUILayout.Button("Clear", GUILayout.Height(30)))
            {
                component.ClearAll();
            }

            // Sort 버튼
            if (GUILayout.Button("Sort", GUILayout.Height(30)))
            {
                component.SortAndSave();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
