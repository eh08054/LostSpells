using UnityEngine;
using UnityEditor;
using System.IO;

namespace LostSpells.Data
{
    /// <summary>
    /// SaveManager 에디터 유틸리티
    /// </summary>
    public class SaveManagerEditor
    {
        [MenuItem("LostSpells/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            string path = Application.persistentDataPath;
            if (Directory.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                Debug.LogWarning($"저장 폴더를 찾을 수 없습니다: {path}");
            }
        }

        [MenuItem("LostSpells/Delete Save File")]
        public static void DeleteSaveFile()
        {
            string path = Path.Combine(Application.persistentDataPath, "PlayerSaveData.json");
            if (File.Exists(path))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "저장 파일 삭제",
                    "정말로 저장 파일을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
                    "삭제",
                    "취소"
                );

                if (confirm)
                {
                    File.Delete(path);
                    Debug.Log($"[SaveManagerEditor] 저장 파일이 삭제되었습니다: {path}");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveManagerEditor] 저장 파일이 존재하지 않습니다: {path}");
            }
        }

        [MenuItem("LostSpells/Log Save File Info")]
        public static void LogSaveFileInfo()
        {
            string path = Path.Combine(Application.persistentDataPath, "PlayerSaveData.json");
            Debug.Log("========== 저장 파일 정보 ==========");
            Debug.Log($"저장 파일 경로: {path}");
            Debug.Log($"파일 존재 여부: {File.Exists(path)}");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Debug.Log($"저장 파일 내용:\n{json}");
            }
            Debug.Log("===================================");
        }
    }
}
