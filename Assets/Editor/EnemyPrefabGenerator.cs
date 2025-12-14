#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace LostSpells.Editor
{
    /// <summary>
    /// 몬스터 프리팹 자동 생성기
    /// BlueDragon 프리팹을 기반으로 다른 몬스터 프리팹을 생성
    /// </summary>
    public class EnemyPrefabGenerator : EditorWindow
    {
        private const string TEMPLATE_PATH = "Assets/Templates/PixelFantasy/PixelMonsters";
        private const string OUTPUT_PATH = "Assets/Resources/Enemies";
        private const string BASE_PREFAB_PATH = "Assets/Resources/Enemies/BlueDragonEnemy.prefab";

        [MenuItem("LostSpells/Generate Enemy Prefabs")]
        public static void GenerateEnemyPrefabs()
        {
            // 기본 프리팹 로드
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_PREFAB_PATH);
            if (basePrefab == null)
            {
                Debug.LogError($"[EnemyPrefabGenerator] 기본 프리팹을 찾을 수 없습니다: {BASE_PREFAB_PATH}");
                return;
            }

            // 출력 폴더 확인
            if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Enemies");
            }

            int created = 0;
            int skipped = 0;

            // 모든 Pack 폴더 검색
            string[] packFolders = new string[] { "Pack1", "Pack2", "Pack3", "Pack4", "BossPack1", "BossPack2" };

            foreach (string pack in packFolders)
            {
                string packPath = $"{TEMPLATE_PATH}/{pack}";
                if (!Directory.Exists(packPath))
                    continue;

                // 각 몬스터 폴더 검색
                string[] monsterFolders = Directory.GetDirectories(packPath);
                foreach (string monsterFolder in monsterFolders)
                {
                    // .asset 파일들 찾기
                    string[] assetFiles = Directory.GetFiles(monsterFolder, "*.asset");
                    foreach (string assetFile in assetFiles)
                    {
                        if (assetFile.EndsWith(".meta"))
                            continue;

                        string assetPath = assetFile.Replace("\\", "/");
                        string monsterName = Path.GetFileNameWithoutExtension(assetPath);
                        string outputPrefabPath = $"{OUTPUT_PATH}/{monsterName}Enemy.prefab";

                        // 이미 존재하면 스킵
                        if (File.Exists(outputPrefabPath))
                        {
                            skipped++;
                            continue;
                        }

                        // 스프라이트 라이브러리 에셋 로드
                        var spriteLibraryAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.Animation.SpriteLibraryAsset>(assetPath);
                        if (spriteLibraryAsset == null)
                        {
                            Debug.LogWarning($"[EnemyPrefabGenerator] SpriteLibraryAsset 로드 실패: {assetPath}");
                            continue;
                        }

                        // 같은 폴더에서 PNG 찾기
                        string pngPath = assetPath.Replace(".asset", ".png");
                        Sprite mainSprite = null;
                        if (File.Exists(pngPath))
                        {
                            // 스프라이트 시트에서 첫 번째 스프라이트 로드
                            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath);
                            foreach (var obj in sprites)
                            {
                                if (obj is Sprite sprite)
                                {
                                    mainSprite = sprite;
                                    break;
                                }
                            }
                        }

                        // 프리팹 복제 및 수정
                        GameObject newPrefab = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                        newPrefab.name = $"{monsterName}Enemy";

                        // EnemyComponent 찾아서 이름 변경
                        var enemyComponent = newPrefab.GetComponent<LostSpells.Components.EnemyComponent>();
                        if (enemyComponent != null)
                        {
                            SerializedObject so = new SerializedObject(enemyComponent);
                            so.FindProperty("enemyName").stringValue = FormatMonsterName(monsterName);
                            so.ApplyModifiedProperties();
                        }

                        // Body 자식에서 SpriteLibrary 찾기
                        Transform bodyTransform = newPrefab.transform.Find("Body");
                        if (bodyTransform != null)
                        {
                            // SpriteLibrary 컴포넌트 설정
                            var spriteLibrary = bodyTransform.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
                            if (spriteLibrary != null)
                            {
                                SerializedObject libSO = new SerializedObject(spriteLibrary);
                                libSO.FindProperty("m_SpriteLibraryAsset").objectReferenceValue = spriteLibraryAsset;
                                libSO.ApplyModifiedProperties();
                            }

                            // SpriteRenderer 스프라이트 설정
                            if (mainSprite != null)
                            {
                                var spriteRenderer = bodyTransform.GetComponent<SpriteRenderer>();
                                if (spriteRenderer != null)
                                {
                                    spriteRenderer.sprite = mainSprite;
                                }
                            }
                        }

                        // NameText 업데이트
                        Transform nameTextTransform = newPrefab.transform.Find("NameText");
                        if (nameTextTransform != null)
                        {
                            var tmpText = nameTextTransform.GetComponent<TMPro.TextMeshPro>();
                            if (tmpText != null)
                            {
                                tmpText.text = FormatMonsterName(monsterName);
                            }
                        }

                        // 프리팹으로 저장
                        PrefabUtility.SaveAsPrefabAsset(newPrefab, outputPrefabPath);
                        DestroyImmediate(newPrefab);

                        Debug.Log($"[EnemyPrefabGenerator] 생성됨: {outputPrefabPath}");
                        created++;
                    }
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[EnemyPrefabGenerator] 완료! 생성: {created}, 스킵: {skipped}");
            EditorUtility.DisplayDialog("Enemy Prefab Generator",
                $"프리팹 생성 완료!\n생성: {created}\n스킵 (이미 존재): {skipped}",
                "확인");
        }

        /// <summary>
        /// 몬스터 이름 포맷팅 (CamelCase -> "Blue Dragon" 형식)
        /// </summary>
        private static string FormatMonsterName(string name)
        {
            string result = "";
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    result += " ";
                }
                result += name[i];
            }
            return result;
        }

        [MenuItem("LostSpells/List Available Monsters")]
        public static void ListAvailableMonsters()
        {
            Debug.Log("========== 사용 가능한 몬스터 목록 ==========");

            string[] packFolders = new string[] { "Pack1", "Pack2", "Pack3", "Pack4", "BossPack1", "BossPack2" };
            int total = 0;

            foreach (string pack in packFolders)
            {
                string packPath = $"{TEMPLATE_PATH}/{pack}";
                if (!Directory.Exists(packPath))
                    continue;

                Debug.Log($"\n--- {pack} ---");

                string[] monsterFolders = Directory.GetDirectories(packPath);
                foreach (string monsterFolder in monsterFolders)
                {
                    string monsterType = Path.GetFileName(monsterFolder);
                    string[] assetFiles = Directory.GetFiles(monsterFolder, "*.asset");

                    List<string> variants = new List<string>();
                    foreach (string assetFile in assetFiles)
                    {
                        if (!assetFile.EndsWith(".meta"))
                        {
                            variants.Add(Path.GetFileNameWithoutExtension(assetFile));
                            total++;
                        }
                    }

                    if (variants.Count > 0)
                    {
                        Debug.Log($"  {monsterType}: {string.Join(", ", variants)}");
                    }
                }
            }

            Debug.Log($"\n총 {total}개의 몬스터 에셋 발견");
        }
    }
}
#endif
