using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class MainWorld : MonoBehaviour
{
    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    public int MapWidth = 6;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    public int MapHeight = 6;

    /// <summary>
    /// 地图颜色
    /// </summary>
    public Color defaultColor = Color.white;

    private MainWorld() { }
    public static MainWorld Instance = null;
    private World m_HexMapWorld;
    private CreateHexCellSystem createHexCellSystem;
    private EntityManager entityManager;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_HexMapWorld = World.Active != null ? World.Active : new World("HexMap");
        createHexCellSystem = m_HexMapWorld.GetOrCreateSystem<CreateHexCellSystem>();
        entityManager = m_HexMapWorld.EntityManager;
        //EntityArchetype hexmapCreater = entityManager.CreateArchetype(typeof(CreaterData),typeof(SwitchCreateCellData));
        Entity creater = createHexCellSystem.GetSingletonEntity<CreaterData>();
        //Todo:从服务器获取玩家上次离开游戏时的位置，并以该位置为中心查找一定范围内的地图数据。
        entityManager.SetComponentData(creater, new CreaterData
        {
            Width= MapWidth,
            Height = MapHeight,
            Color = defaultColor
           
        });

        entityManager.SetComponentData(creater, new SwitchCreateCellData
        {
            bIfNewMap = true,
        });

    }

    // Update is called once per frame
    void Update()
    {
        createHexCellSystem.Update();
    }
}
