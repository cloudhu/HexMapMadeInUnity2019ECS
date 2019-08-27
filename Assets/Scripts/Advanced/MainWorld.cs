using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 主世界
/// </summary>
public class MainWorld : MonoBehaviour
{
    /// <summary>
    /// 地图材质
    /// </summary>
    //public Material material;

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
    //private Entity m_Mesh;
    private Entity m_Builder;
    private Mesh m_Mesh;
    private MeshCollider m_MeshCollider;

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
        EntityArchetype builderArchetype = m_EntityManager.CreateArchetype(typeof(Data),typeof(OnCreateTag));
        m_Builder = m_EntityManager.CreateEntity(builderArchetype);
        //3.Setup Map;  Todo:get map data from server and SetupMap,now we just use default data
        SetupMap(MapWidth, MapHeight, defaultColor);

        //4.Create Mesh entity for map and setup RenderMesh
        GetComponent<MeshFilter>().mesh = m_Mesh = new Mesh();
        m_Mesh.name = "Hex Mesh";
        m_MeshCollider=gameObject.AddComponent<MeshCollider>();
        //5.Create System to spawn cells
        m_CellSpawnSystem = m_HexMapWorld.CreateSystem<CellSpawnSystem>();
    }

    #endregion

    #region Public Function公共方法

    /// <summary>
    /// 渲染地图
    /// </summary>
    public void RenderMesh()
    {

        //暴力获取所有实体，如果有系统外的实体就糟糕了，Todo：只获取Cell单元实体
        NativeArray<Entity> entities = m_EntityManager.GetAllEntities();
        if (entities.Length < HexMetrics.HexCelllCount) return;
        StartCoroutine(RenderHexMap());
    }

    IEnumerator RenderHexMap()
    {
        yield return new WaitForSeconds(0.02f);
        NativeArray<Entity> entities = m_EntityManager.GetAllEntities();
        int totalCount = HexMetrics.HexCelllCount * HexMetrics.CellVerticesCount;
        NativeList<Vector3> Vertices = new NativeList<Vector3>(totalCount, Allocator.Temp);
        NativeList<int> Triangles = new NativeList<int>(totalCount, Allocator.Temp);
        NativeList<Color> Colors = new NativeList<Color>(totalCount, Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            //0.取出实体，如果实体的索引为m_Builder则跳过
            Entity entity = entities[i];
            if (m_EntityManager.HasComponent<OnCreateTag>(entity)) continue;
            if (entity.Index == m_Builder.Index)
            {
                continue;
            }
            DynamicBuffer<ColorBuffer> colorBuffer = m_EntityManager.GetBuffer<ColorBuffer>(entity);
            DynamicBuffer<VertexBuffer> vertexBuffer = m_EntityManager.GetBuffer<VertexBuffer>(entity);
            if (colorBuffer.Length > 0)
            {
                for (int j = 0; j < colorBuffer.Length; j++)
                {
                    Triangles.Add(Vertices.Length);
                    Colors.Add(colorBuffer[j]);
                    Vertices.Add(vertexBuffer[j]);
                }
            }

            colorBuffer.Clear();
            vertexBuffer.Clear();
        }

        m_Mesh.vertices = Vertices.ToArray();
        m_Mesh.triangles = Triangles.ToArray();
        m_Mesh.colors = Colors.ToArray();
        m_Mesh.RecalculateNormals();
        m_Mesh.RecalculateBounds();
        m_MeshCollider.sharedMesh = m_Mesh;
        Vertices.Dispose();
        Triangles.Dispose();
        Colors.Dispose();
    } 

    /// <summary>
    /// 设置地图
    /// </summary>
    /// <param name="width">宽</param>
    /// <param name="height">高</param>
    /// <param name="color">颜色</param>
    public void SetupMap(int width, int height, Color color)
    {
        //Store the cell count for use
        HexMetrics.HexCelllCount = width * height;
        HexMetrics.MapWidth = width;
        m_EntityManager.SetComponentData(m_Builder, new Data
        {
            Width = width,
            Height = height
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

    //public void GetColorBuff(ref NativeArray<Color> colors)
    //{
    //    Debug.Log("GetColorBuff");
    //    DynamicBuffer<ColorBuff> buffs = m_EntityManager.GetBuffer<ColorBuff>(m_Builder);

    //    for (int i = 0; i < buffs.Length; i++)
    //    {
    //        colors[i] = buffs[i].Value;
    //    }

    //    buffs.Clear();
        
    //    m_EntityManager.RemoveComponent<OnCreateTag>(m_Builder);
    //}

    #endregion

}
