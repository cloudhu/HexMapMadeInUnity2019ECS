using System;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

//public struct MeshColliderData : ISharedComponentData, IEquatable<MeshColliderData> {
//    public MeshCollider HexMeshCollider;

//    public bool Equals(MeshColliderData other)
//    {
//        return HexMeshCollider==other.HexMeshCollider;
//    }

//    public override int GetHashCode()
//    {
//        int hash = 0;
//        if (!ReferenceEquals(HexMeshCollider, null)) hash ^= HexMeshCollider.GetHashCode();
//        return hash;
//    }
//}

public struct HexMeshTag : IComponentData { }

[RequiresEntityConversion]
public class HexMapEntity : MonoBehaviour, IConvertGameObjectToEntity
{
    //public MeshCollider HexMeshCollider=null;
    public Mesh HexMesh;
    public MeshRenderer MeshRenderer;
    public MeshFilter MeshFilter;
    public Material Material;

    void Awake()
    {
        MeshFilter = gameObject.AddComponent<MeshFilter>();
        MeshFilter.mesh = HexMesh = new Mesh();
        HexMesh.name = "Hex Mesh";
        //HexMeshCollider = gameObject.AddComponent<MeshCollider>();
        MeshRenderer = gameObject.AddComponent<MeshRenderer>();
        MeshRenderer.material = Material;
        MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent(entity, typeof(HexMeshTag));
        dstManager.AddComponent(entity, typeof(Parent));
        //dstManager.SetSharedComponentData(entity, new RenderMesh
        //{
        //    mesh=HexMesh,
        //    material=MeshRenderer.material,
        //    castShadows=MeshRenderer.shadowCastingMode
        //});

        //dstManager.AddSharedComponentData(entity, new MeshColliderData
        //{
        //    HexMeshCollider=this.HexMeshCollider
        //});
    }
}
