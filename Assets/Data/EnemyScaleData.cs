using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LostSpells.Data
{
    /// <summary>
    /// 적 크기 데이터를 관리하는 클래스
    /// </summary>
    [Serializable]
    public class EnemyScaleDataContainer
    {
        public float defaultScale = 1.0f;
        public float defaultHealthBarHeight = 1.5f;
        public Dictionary<string, float> enemyScales = new Dictionary<string, float>();
        public Dictionary<string, float> enemyHealthBarHeights = new Dictionary<string, float>();
    }

    public static class EnemyScaleData
    {
        private static EnemyScaleDataContainer _data;
        private static bool _isLoaded = false;

        private const string RESOURCE_PATH = "GameData/EnemyScaleData";
        private const string FILE_PATH = "Assets/Data/Resources/GameData/EnemyScaleData.json";

        /// <summary>
        /// 적 크기 데이터 로드
        /// </summary>
        public static void Load()
        {
            if (_isLoaded) return;

            TextAsset textAsset = Resources.Load<TextAsset>(RESOURCE_PATH);
            if (textAsset != null)
            {
                _data = JsonUtility.FromJson<EnemyScaleDataWrapper>(textAsset.text).ToContainer();
                _isLoaded = true;
                Debug.Log($"[EnemyScaleData] 로드 완료: {_data.enemyScales.Count}개 적 크기 데이터");
            }
            else
            {
                Debug.LogWarning("[EnemyScaleData] 데이터 파일을 찾을 수 없습니다. 기본값 사용.");
                _data = new EnemyScaleDataContainer();
                _isLoaded = true;
            }
        }

        /// <summary>
        /// 적 이름으로 크기 가져오기
        /// </summary>
        public static float GetScale(string enemyName)
        {
            if (!_isLoaded) Load();

            if (_data.enemyScales.TryGetValue(enemyName, out float scale))
            {
                return scale;
            }
            return _data.defaultScale;
        }

        /// <summary>
        /// 적 크기 설정
        /// </summary>
        public static void SetScale(string enemyName, float scale)
        {
            if (!_isLoaded) Load();
            _data.enemyScales[enemyName] = scale;
        }

        /// <summary>
        /// 기본 크기 가져오기
        /// </summary>
        public static float GetDefaultScale()
        {
            if (!_isLoaded) Load();
            return _data.defaultScale;
        }

        /// <summary>
        /// 적 체력바 높이 가져오기
        /// </summary>
        public static float GetHealthBarHeight(string enemyName)
        {
            if (!_isLoaded) Load();

            if (_data.enemyHealthBarHeights.TryGetValue(enemyName, out float height))
            {
                return height;
            }
            return _data.defaultHealthBarHeight;
        }

        /// <summary>
        /// 적 체력바 높이 설정
        /// </summary>
        public static void SetHealthBarHeight(string enemyName, float height)
        {
            if (!_isLoaded) Load();
            _data.enemyHealthBarHeights[enemyName] = height;
        }

        /// <summary>
        /// 기본 체력바 높이 가져오기
        /// </summary>
        public static float GetDefaultHealthBarHeight()
        {
            if (!_isLoaded) Load();
            return _data.defaultHealthBarHeight;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 데이터 저장 (에디터 전용)
        /// </summary>
        public static void Save()
        {
            if (!_isLoaded) Load();

            var wrapper = EnemyScaleDataWrapper.FromContainer(_data);
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(FILE_PATH, json);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"[EnemyScaleData] 저장 완료: {FILE_PATH}");
        }

        /// <summary>
        /// 데이터 다시 로드 (에디터 전용)
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            Load();
        }
#endif
    }

    /// <summary>
    /// JsonUtility용 래퍼 클래스 (Dictionary 직렬화 지원)
    /// </summary>
    [Serializable]
    public class EnemyScaleDataWrapper
    {
        public float defaultScale = 1.0f;
        public float defaultHealthBarHeight = 1.5f;
        public List<EnemyScaleEntry> enemyScales = new List<EnemyScaleEntry>();

        public EnemyScaleDataContainer ToContainer()
        {
            var container = new EnemyScaleDataContainer
            {
                defaultScale = defaultScale,
                defaultHealthBarHeight = defaultHealthBarHeight,
                enemyScales = new Dictionary<string, float>(),
                enemyHealthBarHeights = new Dictionary<string, float>()
            };
            foreach (var entry in enemyScales)
            {
                container.enemyScales[entry.enemyName] = entry.scale;
                container.enemyHealthBarHeights[entry.enemyName] = entry.healthBarHeight;
            }
            return container;
        }

        public static EnemyScaleDataWrapper FromContainer(EnemyScaleDataContainer container)
        {
            var wrapper = new EnemyScaleDataWrapper
            {
                defaultScale = container.defaultScale,
                defaultHealthBarHeight = container.defaultHealthBarHeight,
                enemyScales = new List<EnemyScaleEntry>()
            };
            foreach (var kvp in container.enemyScales)
            {
                float healthBarHeight = container.enemyHealthBarHeights.TryGetValue(kvp.Key, out float h)
                    ? h : container.defaultHealthBarHeight;
                wrapper.enemyScales.Add(new EnemyScaleEntry
                {
                    enemyName = kvp.Key,
                    scale = kvp.Value,
                    healthBarHeight = healthBarHeight
                });
            }
            // 종류별로 그룹화하여 정렬 (Bear, Wolf, Dragon 등)
            wrapper.enemyScales.Sort((a, b) =>
            {
                string typeA = GetEnemyType(a.enemyName);
                string typeB = GetEnemyType(b.enemyName);
                int typeCompare = string.Compare(typeA, typeB);
                if (typeCompare != 0) return typeCompare;
                // 같은 종류면 전체 이름으로 정렬
                return string.Compare(a.enemyName, b.enemyName);
            });
            return wrapper;
        }

        /// <summary>
        /// 적 이름에서 기본 종류 추출 (예: BlackBearEnemy -> Bear)
        /// </summary>
        private static string GetEnemyType(string enemyName)
        {
            // "Enemy" 접미사 제거
            string name = enemyName.Replace("Enemy", "");

            // 색상/설명 접두사 목록
            string[] prefixes = {
                // 색상
                "Black", "Blue", "Brown", "Green", "Grey", "Purple", "Red", "Yellow", "Golden", "Pink",
                // 설명
                "Abyss", "Hell", "Fire", "Ice", "Flame", "Earth", "Royal", "Flying", "Death", "Dark", "Bloody", "Mega"
            };

            // 접두사 제거
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix))
                {
                    name = name.Substring(prefix.Length);
                    break;
                }
            }

            // 숫자 접미사 제거 (Gorgon1 -> Gorgon)
            name = Regex.Replace(name, @"\d+$", "");

            return name;
        }
    }

    [Serializable]
    public class EnemyScaleEntry
    {
        public string enemyName;
        public float scale;
        public float healthBarHeight = 1.5f;
    }
}
