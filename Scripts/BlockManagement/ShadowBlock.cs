using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowBlock : MonoBehaviour
{

    BlockManager manager;
    // Use this for initialization
    void Start()
    {
        manager = GetComponentInParent<BlockManager>();
    }

    MeshRenderer mMeshRenderer = null;

    MeshRenderer meshRenderer
    {
        get
        {
            if (mMeshRenderer == null)
                mMeshRenderer = GetComponent<MeshRenderer>();

            return mMeshRenderer;
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (manager == null)
            return;

        if (!meshRenderer)
            return;

        if (manager.status == BlockCombinationStatus.Combine)
        {
            meshRenderer.enabled = true;
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }
}
