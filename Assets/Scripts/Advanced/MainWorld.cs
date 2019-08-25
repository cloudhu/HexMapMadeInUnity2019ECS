using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 主世界
/// </summary>
public class MainWorld : MonoBehaviour
{
    /// <summary>
    /// 地图材质
    /// </summary>
    public Material material;
    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    [SerializeField]
    private int MapWidth = 6;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    [SerializeField] private int MapHeight = 6;

    /// <summary>
    /// 地图颜色
    /// </summary>
    [SerializeField] private Color defaultColor = Color.white;

    //单例模式
    public static MainWorld Instance = null;
    private World m_HexMapWorld;
    private CellSpawnSystem m_CellSpawnSystem;
    private EntityManager m_EntityManager;
    private Entity m_Mesh;
    private Entity m_Builder;

    #region Mono

    //Make this single
    private void Awake()
    {
        Instance = this;
        //初始化
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        m_CellSpawnSystem.Update();
    }

    #endregion

    #region Init初始化
    /// <summary>
    /// 构造函数
    /// </summary>
    private MainWorld() { }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Initialize()
    {
        //0.get the active world or new one
        m_HexMapWorld = World.Active != null ? World.Active : new World("HexMap");
        //1.get the entity Manager
        m_EntityManager = m_HexMapWorld.EntityManager;
        //2.Create Builder Entity;
        EntityArchetype builderArchetype = m_EntityManager.CreateArchetype(typeof(Data));
        m_Builder = m_EntityManager.CreateEntity(builderArchetype);
        //3.Setup Map;  Todo:get map data from server and SetupMap,now we just use default data
        SetupMap(MapWidth, MapHeight, defaultColor);

        //4.Create Mesh entity for map and setup RenderMesh
        EntityArchetype hexMeshArchetype = m_EntityManager.CreateArchetype(typeof(RenderMesh), typeof(MapMesh));
        m_Mesh = m_EntityManager.CreateEntity(hexMeshArchetype);
        m_EntityManager.SetSharedComponentData(m_Mesh, new RenderMesh
        {
            mesh=new Mesh(),
            material = this.material,
            castShadows = UnityEngine.Rendering.ShadowCastingMode.On,
            receiveShadows = true
        });
        //m_EntityManager.AddBuffer<ColorBuffer>(m_Mesh);
        //m_EntityManager.AddBuffer<VertexBuffer>(m_Mesh);
        //m_EntityManager.AddBuffer<TriangleBuffer>(m_Mesh);
        //5.Store the cell count for use
        HexMetrics.HexCelllCount = MapWidth * MapHeight;
        HexMetrics.MapWidth = MapWidth;
        //6.Create System to spawn cells
        m_CellSpawnSystem = m_HexMapWorld.CreateSystem<CellSpawnSystem>();
    }

    #endregion

    #region Public Function公共方法

    /// <summary>
    /// 设置地图
    /// </summary>
    /// <param name="width">宽</param>
    /// <param name="height">高</param>
    /// <param name="color">颜色</param>
    public void SetupMap(int width, int height, Color color)
    {

        m_EntityManager.SetComponentData(m_Builder, new Data
        {
            Width = width,
            Height = height,
            BIfNewMap = true
        });
    }

    public World GetWorld()
    {
        return m_HexMapWorld;
    }

    public EntityManager GetEntityManager()
    {
        return m_EntityManager;
    }

    public CellSpawnSystem GetCellSpawnSystem()
    {
        return m_CellSpawnSystem;
    }

    public Entity GetBuilderEntity()
    {
        return m_Builder;
    }

    public Entity GetMeshEntity()
    {
        return m_Mesh;
    }

    public DynamicBuffer<ColorBuff> GetColorBuff()
    {
        return m_EntityManager.GetBuffer<ColorBuff>(m_Builder);
    }

    //public DynamicBuffer<ColorBuffer> GetColorBuffer()
    //{
    //    return m_EntityManager.GetBuffer<ColorBuffer>(m_Mesh);
    //}
    //public DynamicBuffer<VertexBuffer> GetVertexBuffer()
    //{
    //    return m_EntityManager.GetBuffer<VertexBuffer>(m_Mesh);
    //}
    //public DynamicBuffer<TriangleBuffer> GetTriangleBuffer()
    //{
    //    return m_EntityManager.GetBuffer<TriangleBuffer>(m_Mesh);
    //}
    #endregion

}
