using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainDoorBlock : MonoBehaviour
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

        if (manager.status == BlockCombinationStatus.Combine || manager.status == BlockCombinationStatus.Divide || manager.status == BlockCombinationStatus.CombineInsidePreview)
        {
            meshRenderer.enabled = false;
        }
        else
        {
            meshRenderer.enabled = true;
        }
    }
}
