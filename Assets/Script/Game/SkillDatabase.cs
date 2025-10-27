using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Scriptable Objects/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    [SerializeField] List<GameObject> skilllist;
    public List<GameObject> Skilllist => skilllist;
}
