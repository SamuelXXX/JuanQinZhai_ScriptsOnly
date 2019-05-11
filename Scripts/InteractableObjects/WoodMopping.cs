using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodMopping : SkinPeeler
{
    protected Material m_Material;
    protected Material mMaterial
    {
        get
        {
            if (m_Material == null)
            {
                m_Material = GetComponentInChildren<MeshRenderer>().material;
            }

            return m_Material;
        }
    }

    int playCount = 0;
    protected override void OnUpdateProgress(float newProgress)
    {
        Vector2 offset = mMaterial.GetTextureOffset("_MainTex");
        offset.y = (newProgress - 1) / 2f;
        mMaterial.SetTextureOffset("_MainTex", offset);
    }
}
