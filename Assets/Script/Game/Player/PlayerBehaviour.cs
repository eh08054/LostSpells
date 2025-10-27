using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] SkillDatabase database;
    [SerializeField] Transform skillTr;
    public void Initialise()
    {

    }

    public void UseSkill(int damage, SkillType skillType, SkillThemeType skillThemeType)
    {
        if(skillType == SkillType.skill1)
        {

        }
        else if (skillType == SkillType.skill2)
        {

        }
        else if (skillType == SkillType.skill3)
        {

        }

        if(skillThemeType == SkillThemeType.fire)
        {

        }
        else if (skillThemeType == SkillThemeType.ice)
        {

        }
        else if (skillThemeType == SkillThemeType.poison)
        {

        }
        else if (skillThemeType == SkillThemeType.lighting)
        {

        }
        SkillBehaviour skill = GameObject.Instantiate(database.Skilllist[0]).GetComponent<SkillBehaviour>();
        skill.transform.position = skillTr.position;
        skill.Initialise(1f, damage, skillType, skillThemeType);
    }

    public void loadingSkill()
    {
        SkillBehaviour skill = GameObject.Instantiate(database.Skilllist[0],transform.parent).GetComponent<SkillBehaviour>();
        skill.transform.position = skillTr.position;
        skill.Initialise(1000f, Random.Range(1,10), SkillType.skill1, SkillThemeType.fire);
    }

    void Update()
    {
        
    }
}
