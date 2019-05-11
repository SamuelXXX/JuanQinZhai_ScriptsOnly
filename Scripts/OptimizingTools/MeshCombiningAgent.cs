using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCombiningAgent : MonoBehaviour
{
    #region Definations
    public enum OperationAfterCombine
    {
        DoNothing = 0,
        DestroyGameObject,
        DestroyRenderer,
        HideRenderer,

    }
    public class RawBuildingData
    {
        //Core data
        public Mesh mesh;
        public Material material;
        public Texture2D mainTexture;
        public Texture2D normalTexture;

        //related data
        public MeshRenderer meshRenderer;
        public GameObject gameObject;

        //other settings
        public OperationAfterCombine operationAfterCombine = OperationAfterCombine.DestroyGameObject;
    }


    public class Texture2DMapItem
    {
        public Texture2D mainTexture;
        public Texture2D normalTexture;
    }

    public class MaterialMapItem
    {
        public Material material;
        public Texture2DMapItem refTexture;
    }

    public class MeshMapItem
    {
        public Mesh mesh;
        public MaterialMapItem refMaterial;

        //related data
        public MeshRenderer meshRenderer;
        public GameObject gameObject;
    }


    public class CombiningOutput
    {
        public Mesh mesh;
        public Material material;
        public Texture2D mainTexture;
        public Texture2D normalTexture;

        public MeshFilter generatedMeshFilter;
        public MeshRenderer generatedMeshRenderer;
        public GameObject generatedGameObject;
    }

    #endregion

    #region Run-time data
    /// <summary>
    /// output data of first stage
    /// </summary>
    public List<RawBuildingData> allRawBuildingData = new List<RawBuildingData>();


    //Output data of second stage to build a reference network
    public List<Texture2DMapItem> clampTextureSet = new List<Texture2DMapItem>();
    public List<Texture2DMapItem> repeatTextureSet = new List<Texture2DMapItem>();

    public List<MaterialMapItem> clampMaterialSet = new List<MaterialMapItem>();
    public List<MaterialMapItem> repeatMaterialSet = new List<MaterialMapItem>();

    public List<MeshMapItem> clampMeshSet = new List<MeshMapItem>();
    public List<MeshMapItem> repeatMeshSet = new List<MeshMapItem>();

    //Output data of third stage
    public List<CombiningOutput> clampCombineOutputs = new List<CombiningOutput>();
    public List<CombiningOutput> repeatCombineOutputs = new List<CombiningOutput>();
    #endregion

    #region Operation Pipe Line
    /// <summary>
    /// Collect all combineing source data from children objects
    /// </summary>
    public void AssembleBuildData()
    {
        MeshFilter[] allFilters = GetComponentsInChildren<MeshFilter>(false);
        allRawBuildingData.Clear();
        foreach (var f in allFilters)
        {
            RawBuildingData rawData = new RawBuildingData();
            if (f.GetComponent<CombineRejectMarker>())
            {
                var crm = f.GetComponent<CombineRejectMarker>();
                if (crm.operationAfterCombine == OperationAfterCombine.DoNothing)
                    continue;

                rawData.operationAfterCombine = crm.operationAfterCombine;
            }

            if (f.sharedMesh == null)
            {
                Debug.Log("A mesh filter with no filter detected! The combining process will ignore this mesh filter");
                continue;
            }
            var renderer = f.GetComponent<MeshRenderer>();
            if (renderer == null)
                continue;

            if (renderer.sharedMaterial == null)
            {
                Debug.Log("A renderer with no material detected! The combining process will ignore this renderer");
                continue;
            }


            rawData.meshRenderer = renderer;
            rawData.gameObject = renderer.gameObject;

            rawData.mesh = f.sharedMesh;
            rawData.material = renderer.sharedMaterial;
            rawData.mainTexture = rawData.material.mainTexture as Texture2D;
            rawData.normalTexture = rawData.material.GetTexture("_BumpMap") as Texture2D;
            if (rawData.mainTexture == null && rawData.normalTexture == null)
            {
                Debug.Log("A mesh target with no texture input is detected! The Combining process will ignore it");
                continue;
            }
            allRawBuildingData.Add(rawData);
        }
    }

    /// <summary>
    /// Build all resources referencing network
    /// </summary>
    public void BuildMappingNetwork()
    {
        //Build texture set
        clampTextureSet.Clear();
        repeatTextureSet.Clear();
        foreach (var m in allRawBuildingData)
        {
            Texture2D mainTexture = m.mainTexture;
            Texture2D normalTexture = m.normalTexture;
            TextureWrapMode texWrapMode;
            Texture2DMapItem mapItem = new Texture2DMapItem();
            mapItem.mainTexture = mainTexture;
            mapItem.normalTexture = normalTexture;

            if (mainTexture)
            {
                texWrapMode = mainTexture.wrapMode;
            }
            else
            {
                texWrapMode = normalTexture.wrapMode;
            }

            if (texWrapMode == TextureWrapMode.Clamp)
            {
                if (clampTextureSet.Find(a => { return a.mainTexture == mainTexture && a.normalTexture == normalTexture; }) != null)
                {
                    continue;
                }
                else
                {
                    clampTextureSet.Add(mapItem);
                }
            }
            else
            {
                if (repeatTextureSet.Find(a => { return a.mainTexture == mainTexture && a.normalTexture == normalTexture; }) != null)
                {
                    continue;
                }
                else
                {
                    repeatTextureSet.Add(mapItem);
                }
            }
        }

        //Build material set
        clampMaterialSet.Clear();
        repeatMaterialSet.Clear();
        foreach (var m in allRawBuildingData)
        {
            Material material = m.material;
            Texture2D mainTexture = m.mainTexture;
            Texture2D normalTexture = m.normalTexture;

            Texture2DMapItem refTexture;
            TextureWrapMode texWrapMode;

            MaterialMapItem mapItem = new MaterialMapItem();

            if (mainTexture)
            {
                texWrapMode = mainTexture.wrapMode;
            }
            else
            {
                texWrapMode = normalTexture.wrapMode;
            }

            if (texWrapMode == TextureWrapMode.Clamp)
            {
                if (clampMaterialSet.Find(a => { return a.material == material; }) != null)
                {
                    continue;
                }
                else
                {
                    refTexture = clampTextureSet.Find(
                        a =>
                        {
                            return a.mainTexture == mainTexture && a.normalTexture == normalTexture;
                        }
                    );
                    mapItem.material = material;
                    mapItem.refTexture = refTexture;
                    clampMaterialSet.Add(mapItem);
                }
            }
            else
            {
                if (repeatMaterialSet.Find(a => { return a.material == material; }) != null)
                {
                    continue;
                }
                else
                {
                    refTexture = repeatTextureSet.Find(
                        a =>
                        {
                            return a.mainTexture == mainTexture && a.normalTexture == normalTexture;
                        }
                    );
                    mapItem.material = material;
                    mapItem.refTexture = refTexture;
                    repeatMaterialSet.Add(mapItem);
                }
            }
        }

        //Build mesh set
        clampMeshSet.Clear();
        repeatMeshSet.Clear();
        foreach (var m in allRawBuildingData)
        {
            Material material = m.material;
            Mesh mesh = m.mesh;
            Texture2D mainTexture = m.mainTexture;
            Texture2D normalTexture = m.normalTexture;

            MaterialMapItem refMaterial;
            TextureWrapMode texWrapMode;

            MeshMapItem mapItem = new MeshMapItem();
            mapItem.mesh = mesh;
            mapItem.meshRenderer = m.meshRenderer;
            mapItem.gameObject = m.gameObject;

            if (mainTexture)
            {
                texWrapMode = mainTexture.wrapMode;
            }
            else
            {
                texWrapMode = normalTexture.wrapMode;
            }

            if (texWrapMode == TextureWrapMode.Clamp)
            {
                refMaterial = clampMaterialSet.Find(
                    a =>
                    {
                        return a.material == material;
                    }
                );
                mapItem.refMaterial = refMaterial;
                clampMeshSet.Add(mapItem);
            }
            else
            {
                refMaterial = repeatMaterialSet.Find(
                    a =>
                    {
                        return a.material == material;
                    }
                );
                mapItem.refMaterial = refMaterial;
                repeatMeshSet.Add(mapItem);
            }
        }
    }

    public void CombineMeshAndTexture()
    {

    }
    #endregion

    #region Tool Methods
    //protected Texture2D CombineTexture(List<Texture2D> textures,out Rect[] textureBlockDescriptor)
    //{
        
    //}
    #endregion
}
