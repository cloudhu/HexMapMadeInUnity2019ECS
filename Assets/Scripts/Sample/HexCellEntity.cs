using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// E:六边形单元
/// </summary>
[RequiresEntityConversion]
public class HexCellEntity : MonoBehaviour,IConvertGameObjectToEntity {

    /// <summary>
    /// 三维坐标
    /// </summary>
    public int X;
    public int Y;
    public int Z;

    /// <summary>
    /// 颜色
    /// </summary>
    public Color Color;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        //数据交给C保存
        dstManager.AddComponentData(entity, new HexCellData
        {
            X=this.X,
            Y=this.Y,
            Z=this.Z,
            color=Color,

        });

        //添加父组件
        //dstManager.AddComponent(entity, typeof(Parent));
        //添加相对父类的本地位置组件
        //dstManager.AddComponent(entity, typeof(LocalToParent));
        //dstManager.AddComponent(entity, typeof(Rotation));
    }

}
