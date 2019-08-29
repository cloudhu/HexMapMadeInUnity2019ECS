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
    /// 噪声采样纹理图
    /// </summary>
    public Texture2D noiseSource;

    /// <summary>
    /// 地图颜色
    /// </summary>
    //[SerializeField] private Color defaultColor = Color.white;

    #region Private Var

    private World m_HexMapWorld;
    private CellSpawnSystem m_CellSpawnSystem;
    private EntityManager m_EntityManager;
    private Entity m_Builder;
    private Mesh m_Mesh;
    private MeshCollider m_MeshCollider;
    //上一次点击的单元索引
    private int m_PrevClickCell = -1;
    //上一次选择的颜色
    private Color m_PrevSelect = Color.black;
    //上一次设置的海拔
    private int m_PrevElevation = 0;

    #endregion


    #region Mono
    //单例模式
    public static MainWorld Instance = null;
    //Make this single
    private void Awake()
    {
        Instance = this;
        //初始化
        Initialize();
    }

    void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
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
        EntityArchetype builderArchetype = m_EntityManager.CreateArchetype(typeof(Data),typeof(NewDataTag));
        m_Builder = m_EntityManager.CreateEntity(builderArchetype);
        //3.Setup Map;  Todo:get map data from server and SetupMap,now we just use default data
        SetupMap(MapWidth, MapHeight);

        //4.Create Mesh entity for map and setup RenderMesh
        GetComponent<MeshFilter>().mesh = m_Mesh = new Mesh();
        m_Mesh.name = "Hex Mesh";
        m_Mesh.MarkDynamic();
        m_MeshCollider=gameObject.AddComponent<MeshCollider>();
        //5.Create System to spawn cells
        m_CellSpawnSystem = m_HexMapWorld.CreateSystem<CellSpawnSystem>();

        HexMetrics.noiseSource = noiseSource;
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
            if (m_EntityManager.HasComponent<NewDataTag>(entity)) continue;
            if (!m_EntityManager.HasComponent<Cell>(entity)) continue;
            DynamicBuffer<ColorBuffer> colorBuffer = m_EntityManager.GetBuffer<ColorBuffer>(entity);
            DynamicBuffer<VertexBuffer> vertexBuffer = m_EntityManager.GetBuffer<VertexBuffer>(entity);

            if (colorBuffer.Length > 0)
            {
                for (int j = 0; j < colorBuffer.Length; j++)
                {
                    Triangles.Add(Vertices.Length);
                    Colors.Add(colorBuffer[j]);
                    Vector3 vertex = Perturb(vertexBuffer[j]);
                    vertex.y += (HexMetrics.SampleNoise(vertex).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                    Vertices.Add(vertex);
                }
            }

            colorBuffer.Clear();
            vertexBuffer.Clear();
        }

        Debug.Log("-----------------------------------------------------------------------------------------");
        Debug.Log("Vertices=" +Vertices.Length + "----Triangles="+ Triangles.Length+ "----Colors="+ Colors.Length);
        Debug.Log(Vertices.Length/ HexMetrics.HexCelllCount);
        if (Vertices.Length>1)
        {
            m_Mesh.Clear();
            m_Mesh.vertices = Vertices.ToArray();
            m_Mesh.triangles = Triangles.ToArray();
            m_Mesh.colors = Colors.ToArray();
            m_Mesh.RecalculateNormals();
            m_Mesh.Optimize();
            m_MeshCollider.sharedMesh = m_Mesh;
        }
        Vertices.Dispose();
        Triangles.Dispose();
        Colors.Dispose();
    }

    /// <summary>
    /// 噪声干扰
    /// </summary>
    /// <param name="position">顶点位置</param>
    /// <returns>被干扰的位置</returns>
    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }

    /// <summary>
    /// 设置地图
    /// </summary>
    /// <param name="width">宽</param>
    /// <param name="height">高</param>
    public void SetupMap(int width, int height)
    {
        //Store the cell count for use
        HexMetrics.HexCelllCount = width * height;
        HexMetrics.MapWidth = width;
        MapWidth = width;
        MapHeight = height;
        m_EntityManager.SetComponentData(m_Builder, new Data
        {
            Width = width,
            Height = height
        });
        m_EntityManager.AddComponent<NewDataTag>(m_Builder);
    }

    /// <summary>
    /// 染色指定位置的六边形单元
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="color">颜色</param>
    public void ColorCell(Vector3 position, Color color, int activeElevation)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * MapWidth + coordinates.Z / 2;
        if (index==m_PrevClickCell && color==m_PrevSelect && m_PrevElevation==activeElevation)
        {//避免玩家重复操作
            return;
        }

        m_PrevClickCell = index;
        m_PrevSelect = color;
        m_PrevElevation = activeElevation;
        StartCoroutine(UpdateCellColor(index,color,activeElevation));
    }

    /// <summary>
    /// 更新单元的颜色
    /// </summary>
    /// <param name="cellIndex">单元索引</param>
    /// <param name="color">颜色</param>
    /// <returns></returns>
    IEnumerator UpdateCellColor(int cellIndex,Color color,int elevation)
    {
        yield return null;
        NativeArray<Entity> entities = m_EntityManager.GetAllEntities();
        if (entities.Length < HexMetrics.HexCelllCount) yield break;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!m_EntityManager.HasComponent<Cell>(entity)) continue;
            m_EntityManager.AddComponentData(entity,new UpdateData
            {
                CellIndex=cellIndex,
                NewColor=color,
                Width=MapWidth,
                Elevation=elevation
            });
        }
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

    #endregion

}
