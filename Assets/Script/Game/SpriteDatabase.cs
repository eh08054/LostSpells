using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteDatabase", menuName = "Scriptable Objects/SpriteDatabase")]
public class SpriteDatabase : ScriptableObject
{
    public List<SpriteText> spriteDict;

    public Sprite GetSprite(string Text)
    {
        if(Text == "" || Text == null) return null;
        return spriteDict.Find(elem=>elem.name == Text).sprite;
    }
}
[Serializable]
public class SpriteText
{
    public string name;
    public Sprite sprite;
}
