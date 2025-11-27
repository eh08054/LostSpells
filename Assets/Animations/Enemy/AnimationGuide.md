# Enemy Animation Guide

## 개요
Dragon 스프라이트에는 8가지 애니메이션이 포함되어 있습니다.
이 문서는 각 애니메이션을 적용하는 방법을 설명합니다.

## 사용 가능한 애니메이션

### 1. Idle (대기) ✅ 적용됨
- **프레임**: Idle_0, Idle_1, Idle_2, Idle_3 (4프레임)
- **샘플레이트**: 6 fps
- **파일**: `BlueDragon_Idle.anim`
- **조건**: Speed < 0.1

### 2. Run (이동) ✅ 적용됨
- **프레임**: Run_0, Run_1, Run_2, Run_3, Run_4, Run_5 (6프레임)
- **샘플레이트**: 12 fps
- **파일**: `BlueDragon_Run.anim`
- **조건**: Speed > 0.1

### 3. Attack (공격) ⏳ 미적용
- **프레임**: Attack_0 ~ Attack_5 (6프레임)
- **추천 샘플레이트**: 10-12 fps
- **용도**: 적이 플레이어를 공격할 때

### 4. Death (사망) ⏳ 미적용
- **프레임**: Death_0 ~ Death_5 (6프레임)
- **추천 샘플레이트**: 8 fps
- **용도**: 적이 죽을 때 (한 번만 재생)

### 5. Fire (불 공격) ⏳ 미적용
- **프레임**: Fire_0 ~ Fire_5 (6프레임)
- **추천 샘플레이트**: 10 fps
- **용도**: 원거리 불 공격 시

### 6. Burn (불타는 상태) ⏳ 미적용
- **프레임**: Burn_0 ~ Burn_5 (6프레임)
- **추천 샘플레이트**: 8 fps
- **용도**: 화염 상태이상에 걸렸을 때

### 7. Ready (준비) ⏳ 미적용
- **프레임**: Ready_0 ~ Ready_3 (4프레임)
- **추천 샘플레이트**: 6 fps
- **용도**: 전투 준비 자세

### 8. Projectile (투사체) ⏳ 미적용
- **프레임**: Projectile_0, Projectile_1 (2프레임)
- **추천 샘플레이트**: 6 fps
- **용도**: 불덩이 같은 투사체 애니메이션

---

## 새로운 애니메이션 추가 방법

### Step 1: 애니메이션 클립 생성

`Assets/Animations/Enemy/BlueDragon_[AnimationName].anim` 파일 생성:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: BlueDragon_[AnimationName]
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
    # 각 프레임 추가 (예: Attack 6프레임)
    - time: 0
      value: {fileID: [Attack_0의 internalID], guid: 8c818873fb8d4ad3bb40ebb62d906a1d, type: 3}
    - time: 0.083333336
      value: {fileID: [Attack_1의 internalID], guid: 8c818873fb8d4ad3bb40ebb62d906a1d, type: 3}
    # ... 나머지 프레임들
    attribute: m_Sprite
    path: Square
    classID: 212
    script: {fileID: 0}
    flags: 2
  m_SampleRate: 12  # fps 조정
  m_WrapMode: 0
  m_Bounds:
    m_Center: {x: 0, y: 0, z: 0}
    m_Extent: {x: 0, y: 0, z: 0}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 855858692  # "Square"의 해시값
      attribute: 0
      script: {fileID: 0}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
    # 각 프레임의 fileID 매핑
    - {fileID: [Attack_0의 internalID], guid: 8c818873fb8d4ad3bb40ebb62d906a1d, type: 3}
    # ... 나머지
  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {fileID: 0}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: 0.5  # 총 재생 시간
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 1  # 반복 여부 (Death는 0으로)
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
```

**중요한 값들:**
- `m_SampleRate`: 애니메이션 fps (높을수록 빠름)
- `m_LoopTime`: 1 = 반복, 0 = 한 번만 재생 (Death 등)
- `path: Square`: Animator가 Enemy에 있고 SpriteRenderer가 Square 자식에 있으므로
- `path: 855858692`: "Square"의 경로 해시값

### Step 2: Meta 파일 생성

`BlueDragon_[AnimationName].anim.meta`:

```yaml
fileFormatVersion: 2
guid: [고유한 GUID 생성]
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 7400000
  userData:
  assetBundleName:
  assetBundleVariant:
```

### Step 3: Animator Controller에 상태 추가

`EnemyAnimator.controller`에 새로운 AnimatorState 추가:

```yaml
--- !u!1102 &[고유한 fileID]
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: [AnimationName]
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions:
  - {fileID: [전환 fileID]}
  m_StateMachineBehaviours: []
  m_Position: {x: 50, y: 50, z: 0}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {fileID: 7400000, guid: [애니메이션 클립의 GUID], type: 2}
  m_Tag:
  m_SpeedParameter:
  m_MirrorParameter:
  m_CycleOffsetParameter:
  m_TimeParameter:
```

### Step 4: 파라미터 및 전환(Transition) 설정

필요한 경우 새로운 파라미터를 추가하고 전환 조건을 설정:

**예: Attack 애니메이션**
- 파라미터: `IsAttacking` (Bool)
- 전환: Idle/Run → Attack (IsAttacking == true)
- 전환: Attack → Idle (IsAttacking == false, Exit Time 체크)

### Step 5: EnemyComponent 스크립트 수정

`Assets/Core/Components/Enemy/EnemyComponent.cs`에서 파라미터 제어:

```csharp
// Attack 예시
public void StartAttack()
{
    if (animator != null && animator.runtimeAnimatorController != null)
    {
        animator.SetBool("IsAttacking", true);
    }
}

public void EndAttack()
{
    if (animator != null && animator.runtimeAnimatorController != null)
    {
        animator.SetBool("IsAttacking", false);
    }
}

// Death 예시
private void Die()
{
    if (animator != null && animator.runtimeAnimatorController != null)
    {
        animator.SetTrigger("Die");
        // 애니메이션 길이만큼 대기 후 삭제
        Destroy(gameObject, 0.75f); // Death 애니메이션 길이
    }
    else
    {
        Destroy(gameObject);
    }
}
```

---

## Sprite Frame ID 참조표

스프라이트 프레임의 internalID는 `BlueDragon.png.meta` 파일에서 확인:

```yaml
# Idle
- Idle_0: 4723107735738678597
- Idle_1: -7385789195546760866
- Idle_2: 3234629978573100161
- Idle_3: 6373048116405451881

# Run
- Run_0: 1856677538
- Run_1: -1905163377
- Run_2: -1963957374
- Run_3: 1965609826
- Run_4: 1998491554
- Run_5: -1062428018

# Attack
- Attack_0: 1071925984
- Attack_1: -845476358
- Attack_2: 2009228482
- Attack_3: 590074039
- Attack_4: -1708985022
- Attack_5: -1479297190

# Death
- Death_0: 1297146405
- Death_1: -1074240218
- Death_2: -1904038070
- Death_3: -958000844
- Death_4: 1318854042
- Death_5: 1448594100

# Fire
- Fire_0: 870851061
- Fire_1: 2041340130
- Fire_2: 1326509612
- Fire_3: 1798496547
- Fire_4: -473751976
- Fire_5: -447831967

# Burn
- Burn_0: -17514963
- Burn_1: 1110528672
- Burn_2: -265684251
- Burn_3: -1041255580
- Burn_4: 1188342725
- Burn_5: 2085800660

# Ready
- Ready_0: -2978808618200810919
- Ready_1: 7683567802946276634
- Ready_2: 4149154237482698375
- Ready_3: -7736129105856362428

# Projectile
- Projectile_0: -341134305
- Projectile_1: 1710350945
```

---

## 현재 Animator Controller 구조

```
EnemyAnimator
├── Parameters
│   └── Speed (Float)
│
└── Base Layer
    ├── Idle (기본 상태)
    │   └── Transition to Run: Speed > 0.1
    │
    └── Run
        └── Transition to Idle: Speed < 0.1
```

---

## 추가 애니메이션 적용 시 권장 구조

### Attack 추가 시:
```
Parameters:
  - Speed (Float)
  - IsAttacking (Bool)

States:
  - Idle → Attack (IsAttacking == true)
  - Run → Attack (IsAttacking == true)
  - Attack → Idle (IsAttacking == false, Has Exit Time)
```

### Death 추가 시:
```
Parameters:
  - Speed (Float)
  - Die (Trigger)

States:
  - Any State → Death (Die 트리거)
  - Death (Loop: false, Exit Time 없음)
```

---

## 체크리스트

새 애니메이션 추가 시 확인사항:

- [ ] .anim 파일 생성
- [ ] .anim.meta 파일 생성 (고유 GUID)
- [ ] EnemyAnimator.controller에 State 추가
- [ ] 필요한 Parameter 추가
- [ ] Transition 설정
- [ ] EnemyComponent.cs에 제어 코드 추가
- [ ] Unity에서 테스트

---

## 참고사항

1. **경로 해시값**: "Square"의 경로 해시는 `855858692`입니다.
2. **Animator 위치**: Enemy 루트 오브젝트에 있음
3. **SpriteRenderer 위치**: Square 자식 오브젝트에 있음
4. **GUID 형식**: 32자리 16진수 (0-9, a-f만 사용)

---

생성일: 2025-11-24
최종 수정: 2025-11-24
