using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Content accessor of draw-up assets database
/// </summary>
public sealed class DrawUpContentProvider
{
    #region Singleton
    static DrawUpContentProvider singleton;
    public static DrawUpContentProvider Singleton//lazy singleton
    {
        get
        {
            if (singleton == null)
            {
                singleton = new DrawUpContentProvider();
                singleton.currentConversation = "";
                singleton.currentSprite = null;
            }
            return singleton;
        }
    }
    private DrawUpContentProvider()
    {

    }
    #endregion

    #region Contents Holder
    public Sprite currentSprite;
    public string currentConversation;
    #endregion

    #region Ext-Control interface
    Dictionary<string, DrawUpAsset> drawUpAssetCache = new Dictionary<string, DrawUpAsset>();

    public void SetDrawUpContent(string roleName, string roleConversationSynopsis)
    {
        DrawUpAsset asset = null;
        if (drawUpAssetCache.ContainsKey(roleName))
        {
            asset = drawUpAssetCache[roleName];
        }
        else
        {
            asset = (DrawUpAsset)Resources.Load(roleName);
            if (asset != null)
            {
                drawUpAssetCache.Add(roleName, asset);
            }
        }
        if (asset == null)
            return;

        RoleConversation conv = asset.FindConversation(roleConversationSynopsis);
        if (conv != null)
        {
            currentSprite = asset.roleExpressions[conv.emotion].image;
            currentConversation = conv.content;
        }
        else
        {
            currentSprite = asset.roleExpressions[0].image;
            currentConversation = roleConversationSynopsis;
        }

    }

    public Sprite GetSprite(string roleName, string roleImageName)
    {
        DrawUpAsset asset = null;
        if (drawUpAssetCache.ContainsKey(roleName))
        {
            asset = drawUpAssetCache[roleName];
        }
        else
        {
            asset = (DrawUpAsset)Resources.Load(roleName);
            if (asset != null)
            {
                drawUpAssetCache.Add(roleName, asset);
            }
        }
        if (asset == null)
            return null;

        RoleImage face = asset.FindFaceExpression(roleImageName);
        if (face != null)
        {
            return face.image;
        }

        return null;
    }
    #endregion
}
