using UnityEditor;
using UnityEngine;
using System.IO;

namespace LostSpells.Editor
{
    /// <summary>
    /// 저장 데이터 관리 에디터 유틸리티
    /// </summary>
    public static class SaveDataManager
    {
        [MenuItem("Lost Spells/Reset Game Data", false, 1)]
        public static void ResetGameData()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "게임 초기화",
                "⚠️ 경고 ⚠️\n\n" +
                "모든 저장 데이터를 삭제합니다:\n" +
                "• 저장 파일 (PlayerSaveData.json)\n" +
                "• 모든 PlayerPrefs\n" +
                "• 기타 저장 데이터\n\n" +
                "이 작업은 되돌릴 수 없습니다!\n\n" +
                "정말 초기화하시겠습니까?",
                "초기화",
                "취소"
            );

            if (!confirm)
            {
                Debug.Log("게임 초기화가 취소되었습니다.");
                return;
            }

            int deletedCount = 0;
            string persistentPath = Application.persistentDataPath;

            // 1. 저장 파일 삭제
            string saveFilePath = Path.Combine(persistentPath, "PlayerSaveData.json");
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log($"<color=green>✓ 저장 파일 삭제:</color> {saveFilePath}");
                deletedCount++;
            }

            // 2. PlayerPrefs 삭제
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("<color=green>✓ PlayerPrefs 삭제 완료</color>");
            deletedCount++;

            // 3. persistentDataPath 폴더의 모든 저장 파일 검색 및 삭제
            if (Directory.Exists(persistentPath))
            {
                // JSON 파일 삭제
                string[] jsonFiles = Directory.GetFiles(persistentPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (string file in jsonFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Debug.Log($"<color=green>✓ JSON 파일 삭제:</color> {Path.GetFileName(file)}");
                        deletedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"파일 삭제 실패: {file}\n{e.Message}");
                    }
                }

                // .dat 파일 삭제 (혹시 있을 수 있는 바이너리 저장 파일)
                string[] datFiles = Directory.GetFiles(persistentPath, "*.dat", SearchOption.TopDirectoryOnly);
                foreach (string file in datFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Debug.Log($"<color=green>✓ DAT 파일 삭제:</color> {Path.GetFileName(file)}");
                        deletedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"파일 삭제 실패: {file}\n{e.Message}");
                    }
                }

                // .sav 파일 삭제 (혹시 있을 수 있는 세이브 파일)
                string[] savFiles = Directory.GetFiles(persistentPath, "*.sav", SearchOption.TopDirectoryOnly);
                foreach (string file in savFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Debug.Log($"<color=green>✓ SAV 파일 삭제:</color> {Path.GetFileName(file)}");
                        deletedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"파일 삭제 실패: {file}\n{e.Message}");
                    }
                }
            }

            // 완료 메시지
            string resultMessage = $"게임 초기화 완료!\n\n삭제된 항목: {deletedCount}개\n\n저장 데이터 경로:\n{persistentPath}";
            EditorUtility.DisplayDialog("초기화 완료", resultMessage, "확인");
            Debug.Log($"<color=cyan>=== 게임 초기화 완료 ===</color>\n{resultMessage}");
        }
    }
}
