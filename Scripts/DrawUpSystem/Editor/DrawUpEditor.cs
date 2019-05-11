using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(DrawUpAsset))]
public class DrawUpEditor : Editor
{
    public DrawUpAsset Target
    {
        get
        {
            return target as DrawUpAsset;
        }
    }

    Texture2D backGroundTexNor ;
    bool NorCreated = false;
    Texture2D backGroundTexAct ;
    bool ActCreated = false;


    public override void OnInspectorGUI()
    {
        if(!NorCreated)
        {
            backGroundTexNor = new Texture2D(128, 128);
            for(int i=0;i<backGroundTexNor.width;i++)
            {
                for(int j=0;j<backGroundTexNor.height;j++)
                {
                    backGroundTexNor.SetPixel(i, j, Color.white);
                }
            }
            NorCreated = true;
        }
        if(!ActCreated)
        {
            backGroundTexAct = new Texture2D(128, 128);
            for (int i = 0; i < backGroundTexAct.width; i++)
            {
                for (int j = 0; j < backGroundTexAct.height; j++)
                {
                    backGroundTexAct.SetPixel(i, j, Color.red);
                }
            }
            ActCreated = true;
        }

        Target.roleName = EditorGUILayout.TextField("Role name", Target.roleName);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(Target.roleExpressionsFolded ? "+" : "-", GUILayout.Height(15f), GUILayout.Width(18f)))
        {
            Target.roleExpressionsFolded = !Target.roleExpressionsFolded;
        }
        EditorGUILayout.LabelField("Role Image Settings:");
        EditorGUILayout.EndHorizontal();

        if (!Target.roleExpressionsFolded)
        {
            for (int i = 0; i < Target.roleExpressions.Count; i++)
            {
                var r = Target.roleExpressions[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(r.folded ? "+" : "-", GUILayout.Height(15f), GUILayout.Width(18f)))
                {
                    r.folded = !r.folded;
                }
                EditorGUILayout.LabelField(r.emotion);
                EditorGUILayout.EndHorizontal();

                if (!r.folded)
                {
                    r.emotion = EditorGUILayout.TextField("Emotion", r.emotion);
                    r.image = (Sprite)EditorGUILayout.ObjectField(r.image, typeof(Sprite));
                }
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                var r = new RoleImage();
                r.emotion = "hehe";
                r.image = null;
                Target.roleExpressions.Add(r);
            }
            if (GUILayout.Button("Remove"))
            {
                Target.roleExpressions.RemoveAt(Target.roleExpressions.Count - 1);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(Target.roleConversationFolded ? "+" : "-", GUILayout.Height(15f), GUILayout.Width(18f)))
        {
            Target.roleConversationFolded = !Target.roleConversationFolded;
        }
        EditorGUILayout.LabelField("Role Conversation Settings:");
        EditorGUILayout.EndHorizontal();

        if (!Target.roleConversationFolded)
        {
            for (int i = 0; i < Target.roleConverstions.Count; i++)
            {
                var r = Target.roleConverstions[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(r.folded ? "+" : "-", GUILayout.Height(15f), GUILayout.Width(18f)))
                {
                    r.folded = !r.folded;
                }
                EditorGUILayout.LabelField(r.synopsis);
                EditorGUILayout.EndHorizontal();

                if (!r.folded)
                {
                    r.synopsis = EditorGUILayout.TextField("Synopsis", r.synopsis);
                    string[] displayOptions = new string[Target.roleExpressions.Count];
                    int[] intValue=new int[Target.roleExpressions.Count];

                    for(int j=0;j<Target.roleExpressions.Count;j++)
                    {
                        displayOptions[j] = Target.roleExpressions[j].emotion;
                        intValue[j] = j;
                    }
                    r.emotion = EditorGUILayout.IntPopup("Emotion",r.emotion, displayOptions, intValue);
                    GUIStyle style = new GUIStyle();
                    style.wordWrap = true;
                    style.padding = new RectOffset(5, 5, 5, 5);

                    GUIStyleState ss = new GUIStyleState();
                    ss.textColor = Color.black;
                    ss.background = backGroundTexNor;

                    GUIStyleState ass = new GUIStyleState();
                    ass.textColor = Color.black;
                    ass.background = backGroundTexAct;
                    
                    
                    style.normal = ss;
                    style.focused = ass;
                    style.active = ass;
                    
                    
                    r.content = EditorGUILayout.TextArea(r.content,style,GUILayout.Height(50f));
                    
                }
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                var r = new RoleConversation();
                r.emotion = 0;
                r.synopsis = "Empty Conversation";
                r.content = "";
                Target.roleConverstions.Add(r);
            }
            if (GUILayout.Button("Remove"))
            {
                Target.roleConverstions.RemoveAt(Target.roleConverstions.Count - 1);
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
