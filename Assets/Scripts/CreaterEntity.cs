using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct MapData : IComponentData {
    public int Width;
    public int Height;
    public Entity Prefab;
    public Color Color;
    public bool bIsNewMap;
}

[RequiresEntityConversion]
public class CreaterEntity : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    /// <summary>
    /// 六边形单元预设
    /// </summary>
    public GameObject HexCellPrefab;

    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    public int MapWidth=6;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    public int MapHeight=6;

    /// <summary>
    /// 地图颜色
    /// </summary>
    public Color defaultColor = Color.white;

    /// <summary>
    /// 是否生成新的地图
    /// </summary>
    public bool bCreatNewMap = true;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        HexMetrics.totalVertices = MapWidth * MapHeight * 18;
        dstManager.AddComponentData(entity, new MapData
        {
            Width=MapWidth,
            Height=MapHeight,
            Prefab = conversionSystem.GetPrimaryEntity(HexCellPrefab),
            Color=defaultColor,
            bIsNewMap=bCreatNewMap
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(HexCellPrefab);
    }
}
