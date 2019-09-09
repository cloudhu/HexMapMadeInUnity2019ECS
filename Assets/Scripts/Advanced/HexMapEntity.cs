using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 这里只负责预设的转化，数据还是由Hex Grid来提供，后面会迭代由服务器提供数据
/// </summary>
[RequiresEntityConversion]
public class HexMapEntity : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{

    public GameObject PalmTree;
    public GameObject Grass;
    public GameObject PalmTrees;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new Data
        {
            PalmTree=conversionSystem.GetPrimaryEntity(PalmTree),
            Grass= conversionSystem.GetPrimaryEntity(Grass),
            PalmTrees = conversionSystem.GetPrimaryEntity(PalmTrees)
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(PalmTree);
        referencedPrefabs.Add(Grass);
        referencedPrefabs.Add(PalmTrees);
    }

}
