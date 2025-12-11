using System;
using System.Collections.Generic;

namespace LostSpells.Data
{
    /// <summary>
    /// 속성별 스킬 변형 데이터
    /// </summary>
    [Serializable]
    public class ElementVariant
    {
        public string name;
        public string effectPrefab;
    }

    /// <summary>
    /// 스킬 데이터 (JSON 직렬화 가능)
    /// </summary>
    [Serializable]
    public class SkillData
    {
        // 기본 정보
        public string skillId;
        public string skillName;        // 한국어 이름
        public string skillNameEn;      // 영어 이름
        public string skillDescription; // 한국어 설명
        public string skillDescriptionEn; // 영어 설명

        // 스킬 타입
        public SkillType skillType = SkillType.Attack;

        // 능력치
        public float cooldown = 5f;
        public float manaCost = 10f;
        public float damage = 50f;
        public float range = 5f;

        // 투사체 관련
        public float projectileSpeed = 10f;
        public float projectileLifetime = 3f;
        public int pierceCount = 0; // 관통 횟수 (0 = 첫 적에서 사라짐, -1 = 무한 관통)

        // 비주얼 (경로 문자열로 저장)
        public string iconPath;
        public string effectPrefabPath;

        // 음성 인식용 키워드 (예: "미사일", "방패", "베기")
        public string voiceKeyword;

        // 음성 인식용 별칭 (예: ["구슬", "매직 미사일"])
        public string[] voiceAliases;

        // 일반 스킬 여부 (피치 기반 속성 적용 대상)
        public bool isGenericSkill = false;

        // 속성별 스킬 변형 (일반 스킬인 경우)
        // 키: "Fire", "Ice", "Electric", "Earth", "Holy", "Void"
        public Dictionary<string, ElementVariant> elementVariants;

        /// <summary>
        /// 현재 언어에 맞는 스킬 이름 반환
        /// </summary>
        public string GetLocalizedName()
        {
            var currentLanguage = LostSpells.Systems.LocalizationManager.Instance.CurrentLanguage;
            if (currentLanguage == LostSpells.Systems.Language.Korean)
                return !string.IsNullOrEmpty(skillName) ? skillName : skillNameEn;
            else
                return !string.IsNullOrEmpty(skillNameEn) ? skillNameEn : skillName;
        }

        /// <summary>
        /// 현재 언어에 맞는 스킬 설명 반환
        /// </summary>
        public string GetLocalizedDescription()
        {
            var currentLanguage = LostSpells.Systems.LocalizationManager.Instance.CurrentLanguage;
            if (currentLanguage == LostSpells.Systems.Language.Korean)
                return !string.IsNullOrEmpty(skillDescription) ? skillDescription : skillDescriptionEn;
            else
                return !string.IsNullOrEmpty(skillDescriptionEn) ? skillDescriptionEn : skillDescription;
        }
    }

    /// <summary>
    /// 스킬 타입
    /// </summary>
    [Serializable]
    public enum SkillType
    {
        Attack,         // 공격
        Defense,        // 방어
        Fireball,       // 화염구
        IceSpike,       // 얼음 가시
        Lightning,      // 번개
        EarthRock,      // 대지의 바위
        HolyLight,      // 신성한 빛
        VoidBlast       // 암흑 폭발
    }
}
