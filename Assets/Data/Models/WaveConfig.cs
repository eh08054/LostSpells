using System;
using System.Collections.Generic;

namespace LostSpells.Data
{
    /// <summary>
    /// 웨이브 설정 데이터 (JSON 직렬화 가능)
    /// </summary>
    [Serializable]
    public class WaveConfig
    {
        // 웨이브 정보
        public int waveNumber = 1;
        public string waveDescription;

        // 몬스터 스폰 설정
        public List<MonsterSpawn> monsterSpawns = new List<MonsterSpawn>();

        // 웨이브 시간 설정
        public float prepareTime = 5f;
        public float timeLimit = 0f;
    }

    /// <summary>
    /// 몬스터 스폰 정보
    /// </summary>
    [Serializable]
    public class MonsterSpawn
    {
        // 스폰할 몬스터 ID (MonsterData의 monsterId 참조)
        public string monsterId;

        // 스폰 개수
        public int spawnCount = 1;

        // 스폰 딜레이 (초) - 각 몬스터 사이의 간격
        public float spawnDelay = 1f;

        // 스폰 시작 시간 (웨이브 시작 후 몇 초 뒤)
        public float startTime = 0f;
    }
}
