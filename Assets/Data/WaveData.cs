using UnityEngine;
using System;

namespace LostSpells.Data
{
    /// <summary>
    /// 스폰 방향
    /// </summary>
    public enum SpawnSide
    {
        Left,   // 왼쪽에서 스폰
        Right,  // 오른쪽에서 스폰
        Both    // 양쪽에서 번갈아 스폰
    }

    /// <summary>
    /// 개별 적 스폰 정보
    /// </summary>
    [Serializable]
    public class EnemySpawnInfo
    {
        [Tooltip("적 프리팹 이름 (Resources/Enemies/ 폴더 내)")]
        public string enemyPrefabName;

        [Tooltip("스폰 방향")]
        public SpawnSide spawnSide = SpawnSide.Right;

        [Tooltip("스폰 수량")]
        [Range(1, 20)]
        public int count = 2;

        [Tooltip("스폰 간격 (초)")]
        [Range(0.1f, 5f)]
        public float spawnInterval = 1f;

        [Tooltip("체력 보너스 (웨이브당 추가)")]
        public int healthBonus = 0;

        [Tooltip("속도 보너스 (웨이브당 추가)")]
        public float speedBonus = 0f;

        /// <summary>
        /// Resources에서 프리팹 로드
        /// </summary>
        public GameObject LoadPrefab()
        {
            if (string.IsNullOrEmpty(enemyPrefabName))
                return null;
            return Resources.Load<GameObject>($"Enemies/{enemyPrefabName}");
        }
    }

    /// <summary>
    /// 웨이브 데이터
    /// </summary>
    [Serializable]
    public class WaveInfo
    {
        [Tooltip("웨이브 번호")]
        public int waveNumber = 1;

        [Tooltip("웨이브 시작 전 대기 시간")]
        public float startDelay = 2f;

        [Tooltip("이 웨이브에 등장하는 적들")]
        public EnemySpawnInfo[] enemies;
    }

    // ChapterWaveData는 ChapterWaveData.cs로 분리됨
    // Unity ScriptableObject는 클래스명과 파일명이 일치해야 함
}
