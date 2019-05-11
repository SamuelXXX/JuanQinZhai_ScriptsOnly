using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRole",menuName = "DrawUpAsset")]
public class DrawUpAsset : ScriptableObject {
    public string roleName;
    public List<RoleImage> roleExpressions=new List<RoleImage>();
    public bool roleExpressionsFolded = true;
    public List<RoleConversation> roleConverstions=new List<RoleConversation>();
    public bool roleConversationFolded = true;

    public RoleConversation FindConversation(string synopsis)
    {
        return roleConverstions.Find(a => a.synopsis == synopsis);
    }

    public RoleImage FindFaceExpression(string emotion)
    {
        return roleExpressions.Find(a => a.emotion == emotion);
    }
}

[System.Serializable]
public class RoleImage
{
    public string emotion;
    public Sprite image;
    public bool folded;
}

[System.Serializable]
public class RoleConversation
{
    public string synopsis;
    public int emotion;
    public string content;
    public bool folded;
}


