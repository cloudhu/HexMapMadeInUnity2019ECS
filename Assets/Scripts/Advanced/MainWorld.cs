using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/// <summary>
/// 主世界
/// </summary>
public class MainWorld : MonoBehaviour
{
    public HexGrid HexGrid;
    //public Material RiverMaterial;
    #region Private Var

    private World m_HexMapWorld;
    private EntityManager m_EntityManager;
    private Entity m_Builder;

    //private Entity m_River;

    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    private int m_CellCountX;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    private int m_CellCountZ;

    private int m_TotalCellCount=10;
    //上一次点击的单元索引
    private int m_PrevClickCell = -1;
    //上一次选择的颜色
    private Color m_PrevSelect = Color.black;
    //上一次设置的海拔
    private int m_PrevElevation = 0;
    //声明一个数组来保存chunk和Cell的对应关系
    private int[] m_ChunkMap;
    /// <summary>
    /// 需要刷新的地图块队列
    /// </summary>
    private Queue<int> m_RefreshQueue;

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
        //Called from OOP HexGrid to separate it from ECS 
        m_RefreshQueue = new Queue<int>();
        //River Entity
        //EntityArchetype riverArchetype = m_EntityManager.CreateArchetype(typeof(RenderMesh));
        //m_River = m_EntityManager.CreateEntity(riverArchetype);
        //m_EntityManager.SetSharedComponentData(m_River,new RenderMesh
        //{
        //    mesh=new Mesh(),
        //    material=RiverMaterial,
        //    castShadows= UnityEngine.Rendering.ShadowCastingMode.Off
        //});
    }

    private void OnDestroy()
    {
        m_RefreshQueue=null;
        m_ChunkMap = null;
    }
    #endregion

    #region Public Function公共方法

    /// <summary>
    /// 渲染地图
    /// </summary>
    public void RenderMesh()
    {
        Debug.Log(m_RefreshQueue.Count);
        if(m_RefreshQueue.Count>0)
        {
            HexGrid.Refresh(m_RefreshQueue.Dequeue());
        }
        else
        {
            StartCoroutine(RenderHexMap());
        }
    }

    IEnumerator RenderHexMap()
    {
        yield return null;//new WaitForSeconds(0.01f);
        NativeArray<Entity> entities = m_EntityManager.GetAllEntities();
        m_ChunkMap = new int[m_TotalCellCount];
        for (int i = 0; i < entities.Length; i++)
        {
            //取出实体，如果实体不满足条件则跳过
            Entity entity = entities[i];
            if (m_EntityManager.HasComponent<NewDataTag>(entity)) continue;
            if (!m_EntityManager.HasComponent<ChunkData>(entity)) continue;
            ChunkData chunkData = m_EntityManager.GetComponentData<ChunkData>(entity);
            HexGrid.AddCellToChunk(chunkData.ChunkId, chunkData.ChunkIndex,chunkData.CellIndex, entity);
            //Debug.Log(chunkData.CellIndex + "====" + chunkData.ChunkId);
            m_ChunkMap[chunkData.CellIndex] = chunkData.ChunkId;
        }

        entities.Dispose();
    }

    /// <summary>
    /// 设置地图
    /// </summary>
    /// <param name="cellCountX">地图X方向的单元数量</param>
    /// <param name="cellCountZ">地图Z方向的单元数量</param>
    public void SetupMap(int cellCountX, int cellCountZ,int chunkCountX)
    {
        //Store the cell count for use
        m_TotalCellCount = cellCountX * cellCountZ;

        this.m_CellCountX = cellCountX;
        this.m_CellCountZ = cellCountZ;
        m_EntityManager.SetComponentData(m_Builder, new Data
        {
            CellCountX = cellCountX,
            CellCountZ = cellCountZ,
            ChunkCountX=chunkCountX
        });
        if (!m_EntityManager.HasComponent<NewDataTag>(m_Builder))
        {
            m_EntityManager.AddComponent<NewDataTag>(m_Builder);
        }
    }

    /// <summary>
    /// 染色指定位置的六边形单元
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="color">颜色</param>
    public void EditCell(Vector3 position, Color color, int activeElevation,int brushSize)
    {
        m_RefreshQueue.Clear();
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * m_CellCountX + coordinates.Z / 2;
        if (index == m_PrevClickCell && color == m_PrevSelect && m_PrevElevation == activeElevation)
        {//避免玩家重复操作
            return;
        }
        Debug.Log("EditCell:" + index);
        m_PrevClickCell = index;
        m_PrevSelect = color;
        m_PrevElevation = activeElevation;
        HexGrid.UpdateChunk(GetChunkId(index), index ,color, activeElevation,false,brushSize);
    }

    /// <summary>
    /// 获取地图块编号
    /// </summary>
    /// <param name="cellIndex">单元索引</param>
    /// <returns>地图块编号</returns>
    int GetChunkId(int cellIndex)
    {
        for (int i = 0; i < m_TotalCellCount; i++)
        {
            if (i == cellIndex)
            {
                int chunkId = m_ChunkMap[i];        
                Debug.Log("GetChunkId:" + chunkId);
                if (!m_RefreshQueue.Contains(chunkId))
                {
                    m_RefreshQueue.Enqueue(chunkId);
                }
                return chunkId;
            }
        }

        return int.MinValue;
    }

    /// <summary>
    /// 被影响的地图块
    /// </summary>
    /// <param name="cellIndex">单元索引</param>
    public void AffectedChunk(int cellIndex,int dstIndex,int brushSize,bool affected)
    {
        if (cellIndex==int.MinValue)
        {
            return;
        }
        Debug.Log("AffectedChunkcellIndex:" + cellIndex);
        int chunkId= GetChunkId(cellIndex);

        if (chunkId!=int.MinValue)
        {
            Debug.Log("AffectedChunkId:" + chunkId);
            HexGrid.UpdateChunk(chunkId, dstIndex, m_PrevSelect, m_PrevElevation, affected, brushSize);
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

    //public Entity GetRiver()
    //{
    //    return m_River;
    //}
    #endregion

}
