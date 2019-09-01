using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 主世界
/// </summary>
public class MainWorld : MonoBehaviour
{
    public HexGrid HexGrid; 
    #region Private Var

    private World m_HexMapWorld;
    private CellSpawnSystem m_CellSpawnSystem;
    private EntityManager m_EntityManager;
    private Entity m_Builder;

    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    private int cellCountX;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    private int cellCountZ;

    private int totalCellCount=10;
    //上一次点击的单元索引
    private int m_PrevClickCell = -1;
    //上一次选择的颜色
    private Color m_PrevSelect = Color.black;
    //上一次设置的海拔
    private int m_PrevElevation = 0;
    //声明一个数组来保存chunk和Cell的对应关系
    private int[] chunkMap;
    /// <summary>
    /// 需要刷新的地图块队列
    /// </summary>
    private Queue<int> RefreshQueue;
    /// <summary>
    /// 如果是全新的地图，则需要全部渲染，否则只需局部渲染
    /// </summary>
    private bool bIsBrandNew = false;

    /// <summary>
    /// 第一次刷新自身，第二次刷新受影响的地图块
    /// </summary>
    private bool bIsSecondRefresh = false;
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
        //Called from OOP HexGrid to separate it from ECS 
        RefreshQueue = new Queue<int>();
    }

    private void OnDestroy()
    {
        //chunkMap.Dispose();
    }
    #endregion

    #region Public Function公共方法

    /// <summary>
    /// 渲染地图
    /// </summary>
    public void RenderMesh()
    {
        Debug.Log(RefreshQueue.Count);
        if (bIsBrandNew)
        {
            //暴力获取所有实体，如果有系统外的实体就糟糕了，Todo：只获取Cell单元实体
            NativeArray<Entity> entities = m_EntityManager.GetAllEntities();
            if (entities.Length < totalCellCount) return;
            //Debug.Log("RenderMesh:"+ entities.Length);
            StartCoroutine(RenderHexMap());
            bIsBrandNew = false;
            entities.Dispose();
        }
        else if(RefreshQueue.Count>0)
        {
            int refreshId = RefreshQueue.Dequeue();
            Debug.Log("refreshId=" + refreshId);
            HexGrid.Refresh(refreshId);
            if (bIsSecondRefresh)
            {
                for (int i = RefreshQueue.Count; i >0; i--)
                {
                    HexGrid.Refresh(RefreshQueue.Dequeue());
                }

                bIsSecondRefresh = false;
            }

            if (RefreshQueue.Count > 1)
            {
                bIsSecondRefresh = true;
            }
        }
    }

    IEnumerator RenderHexMap()
    {
        yield return new WaitForSeconds(0.01f);
        NativeArray<Entity> entities = m_EntityManager.GetAllEntities();
        chunkMap = new int[totalCellCount];
        for (int i = 0; i < entities.Length; i++)
        {
            //取出实体，如果实体不满足条件则跳过
            Entity entity = entities[i];
            if (m_EntityManager.HasComponent<NewDataTag>(entity)) continue;
            if (!m_EntityManager.HasComponent<ChunkData>(entity)) continue;
            ChunkData chunkData = m_EntityManager.GetComponentData<ChunkData>(entity);
            HexGrid.AddCellToChunk(chunkData.ChunkId, chunkData.ChunkIndex,chunkData.CellIndex, entity);
            //Debug.Log(chunkData.CellIndex + "====" + chunkData.ChunkId);
            chunkMap[chunkData.CellIndex] = chunkData.ChunkId;
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
        totalCellCount = cellCountX * cellCountZ;

        this.cellCountX = cellCountX;
        this.cellCountZ = cellCountZ;
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
        bIsBrandNew = true;
        //Create System to spawn cells
        m_CellSpawnSystem = m_HexMapWorld.GetOrCreateSystem<CellSpawnSystem>();
    }

    /// <summary>
    /// 染色指定位置的六边形单元
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="color">颜色</param>
    public void EditCell(Vector3 position, Color color, int activeElevation)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        if (index == m_PrevClickCell && color == m_PrevSelect && m_PrevElevation == activeElevation)
        {//避免玩家重复操作
            return;
        }
        Debug.Log("EditCell:" + index);
        m_PrevClickCell = index;
        m_PrevSelect = color;
        m_PrevElevation = activeElevation;
        HexGrid.UpdateChunk(GetChunkId(index), index ,color, activeElevation);
    }

    /// <summary>
    /// 获取地图块编号
    /// </summary>
    /// <param name="cellIndex">单元索引</param>
    /// <returns>地图块编号</returns>
    int GetChunkId(int cellIndex)
    {
        for (int i = 0; i < totalCellCount; i++)
        {
            if (i == cellIndex)
            {
                int chunkId = chunkMap[i];        
                Debug.Log("GetChunkId:" + chunkId);
                if (!RefreshQueue.Contains(chunkId))
                {
                    RefreshQueue.Enqueue(chunkId);
                    return chunkId;
                }
            }
        }

        return int.MinValue;
    }

    /// <summary>
    /// 被影响的地图块
    /// </summary>
    /// <param name="cellIndex">单元索引</param>
    public void AffectedChunk(int cellIndex)
    {
        Debug.Log("AffectedChunkcellIndex:" + cellIndex);
        int chunkId= GetChunkId(cellIndex);

        if (chunkId!=int.MinValue)
        {
            Debug.Log("AffectedChunkId:" + chunkId);
            HexGrid.UpdateChunk(chunkId, m_PrevClickCell, m_PrevSelect, m_PrevElevation, true);
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
