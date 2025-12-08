using UnityEngine;
using UnityEditor;
using System.IO;

namespace LostSpells.Editor
{
    /// <summary>
    /// VFX 텍스처를 Resources 폴더로 복사하는 에디터 스크립트
    /// </summary>
    public static class VFXSetup
    {
        private const string TextureSourcePath = "Assets/Templates/Pixel Art/PixelArtRPGVFX/Textures";
        private const string DestPath = "Assets/Resources/VFX";

        // 복사할 VFX 텍스처 목록 (카테고리/텍스처명)
        private static readonly string[] VFXToCopy = new string[]
        {
            // Fire (불)
            "Fire/FireBall",
            "Fire/FireShield",
            "Fire/FireTornado",
            "Fire/FireSlash",
            "Fire/FireExplosion1",
            "Fire/FireExplosion2",
            // Ice (얼음)
            "Ice/IceBall",
            "Ice/IceShield",
            "Ice/IceSpike",
            "Ice/IceSlash",
            "Ice/IceProjectile",
            "Ice/IceClaw",
            // Electricity (번개)
            "Electricity/ElectricBall",
            "Electricity/ElectricShield",
            "Electricity/ElectricTornado",
            "Electricity/ElectricSlash",
            "Electricity/ElectricExplosion",
            "Electricity/ElectricLighting1",
            // Earth (대지)
            "Earth/EarthBall",
            "Earth/EarthShield",
            "Earth/EarthRock",
            "Earth/EarthLava",
            "Earth/EarthSpin",
            // Holy (신성)
            "Holy/HolyBall",
            "Holy/HolyShield",
            "Holy/HolyCross",
            "Holy/HolySlash",
            "Holy/HolyProjectile",
            "Holy/HolyBlessing",
            // Void (암흑)
            "Void/VoidBall",
            "Void/VoidShield",
            "Void/VoidBlackHole",
            "Void/VoidSlash",
            "Void/VoidPortal",
            "Void/VoidExplosion1"
        };

        [MenuItem("LostSpells/Setup VFX Sprites")]
        public static void SetupVFXSprites()
        {
            // Resources/VFX 폴더 생성
            CreateDirectoryIfNeeded("Assets/Resources");
            CreateDirectoryIfNeeded(DestPath);

            int copiedCount = 0;
            int skippedCount = 0;

            foreach (var vfxPath in VFXToCopy)
            {
                string sourceTexture = $"{TextureSourcePath}/{vfxPath}.png";
                string category = Path.GetDirectoryName(vfxPath);
                string textureName = Path.GetFileName(vfxPath);

                // 카테고리 폴더 생성
                string destCategory = $"{DestPath}/{category}";
                CreateDirectoryIfNeeded(destCategory);

                string destTexture = $"{destCategory}/{textureName}.png";

                // 소스 파일 존재 확인
                if (!File.Exists(sourceTexture))
                {
                    Debug.LogWarning($"[VFXSetup] 소스 파일을 찾을 수 없음: {sourceTexture}");
                    skippedCount++;
                    continue;
                }

                // 이미 존재하면 스킵
                if (File.Exists(destTexture))
                {
                    Debug.Log($"[VFXSetup] 이미 존재함: {destTexture}");
                    skippedCount++;
                    continue;
                }

                // 텍스처 복사 (메타 파일도 함께 복사됨)
                if (AssetDatabase.CopyAsset(sourceTexture, destTexture))
                {
                    Debug.Log($"[VFXSetup] 복사됨: {sourceTexture} -> {destTexture}");
                    copiedCount++;
                }
                else
                {
                    Debug.LogError($"[VFXSetup] 복사 실패: {sourceTexture}");
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[VFXSetup] 완료! 복사됨: {copiedCount}, 스킵됨: {skippedCount}");
            EditorUtility.DisplayDialog("VFX Setup",
                $"VFX 스프라이트 설정 완료!\n복사됨: {copiedCount}\n스킵됨: {skippedCount}",
                "확인");
        }

        private static void CreateDirectoryIfNeeded(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        [MenuItem("LostSpells/Check VFX Sprites")]
        public static void CheckVFXSprites()
        {
            Debug.Log("[VFXSetup] VFX 스프라이트 확인 중...");

            int foundCount = 0;
            int missingCount = 0;

            foreach (var vfxPath in VFXToCopy)
            {
                string resourcePath = $"VFX/{vfxPath}";
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);

                if (sprites != null && sprites.Length > 0)
                {
                    Debug.Log($"[VFXSetup] OK: {resourcePath} ({sprites.Length}개 스프라이트)");
                    foundCount++;
                }
                else
                {
                    Debug.LogWarning($"[VFXSetup] 누락: {resourcePath}");
                    missingCount++;
                }
            }

            Debug.Log($"[VFXSetup] 확인 완료! 발견: {foundCount}, 누락: {missingCount}");

            if (missingCount > 0)
            {
                EditorUtility.DisplayDialog("VFX Check",
                    $"일부 VFX 스프라이트가 누락되었습니다!\n발견: {foundCount}\n누락: {missingCount}\n\n'LostSpells/Setup VFX Sprites' 메뉴를 실행하세요.",
                    "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("VFX Check",
                    $"모든 VFX 스프라이트가 준비되었습니다!\n총 {foundCount}개",
                    "확인");
            }
        }
    }
}
