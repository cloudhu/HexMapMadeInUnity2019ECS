using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public struct HexCellData : IComponentData
{
    public int X;
    public int Y;
    public int Z;
    public Color color;
}

[RequiresEntityConversion]
public class HexCellEntity : MonoBehaviour,IConvertGameObjectToEntity {
    public int X;
    public int Y;
    public int Z;
    public Color Color;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new HexCellData
        {
            X=this.X,
            Y=this.Y,
            Z=this.Z,
            color=Color
        });
        dstManager.AddComponent(entity, typeof(Parent));
        dstManager.AddComponent(entity, typeof(LocalToParent));
    }

}
