using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/// <summary>
/// 主世界
/// </summary>
public class MainWorld : MonoBehaviour {
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
        //EntityArchetype builderArchetype = m_EntityManager.CreateArchetype(typeof(Data));
        //m_Builder = m_EntityManager.CreateEntity(builderArchetype);
        //3.Setup Map;  Todo:get map data from server and SetupMap,now we just use default data
        //Called from OOP HexGrid to separate it from ECS 
        m_RefreshQueue = new Queue<int>();
        //River Entity
        //EntityArchetype riverArchetype = m_EntityManager.CreateArchetype(typeof(RenderMesh));
        //m_River = m_EntityManager.CreateEntity(riverArchetype);
        FindBuilderEntity();
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
        if (FindBuilderEntity())
        {
            //Store the cell count for use
            m_TotalCellCount = cellCountX * cellCountZ;

            this.m_CellCountX = cellCountX;
            this.m_CellCountZ = cellCountZ;
            if (!m_EntityManager.HasComponent<Data>(m_Builder))
            {
                m_EntityManager.AddComponent<Data>(m_Builder);
            }
            Data data = m_EntityManager.GetComponentData<Data>(m_Builder);
            m_EntityManager.SetComponentData(m_Builder,new Data
            {
                CellCountX=cellCountX,
                CellCountZ=cellCountZ,
                ChunkCountX=chunkCountX,
                PalmTree=data.PalmTree,
                Grass=data.Grass,
                Stumb = data.Stumb,
                Pine_002_L = data.Pine_002_L,
                Pine_002_M2 = data.Pine_002_M2,
                Pine_002_M3 = data.Pine_002_M3,
                Pine_002_M = data.Pine_002_M,
                Pine_002_S2 = data.Pine_002_S2,
                Pine_002_U2 = data.Pine_002_U2,
                Pine_002_U = data.Pine_002_U,
                Pine_002_XL = data.Pine_002_XL,
                Pine_002_XXL = data.Pine_002_XXL,
                Pine_004_01 = data.Pine_004_01,
                Pine_004_02 = data.Pine_004_02,
                Pine_004_03 = data.Pine_004_03,
                Pine_004_04 = data.Pine_004_04,
                Pine_004_05 = data.Pine_004_05,
                Pine_004_06 = data.Pine_004_06,
                Pine_004_Clump01A = data.Pine_004_Clump01A,
                Pine_004_Clump01B = data.Pine_004_Clump01B,
                Pine_004_Clump02A = data.Pine_004_Clump02A,
                Pine_004_Clump02B = data.Pine_004_Clump02B,
                Pine_004_Clump02C = data.Pine_004_Clump02C,
                Pine_005_01 = data.Pine_005_01,
                Pine_005_02 = data.Pine_005_02,
                Pine_006_01 = data.Pine_006_01,
                Pine_006_02 = data.Pine_006_02,
                Pine_006_03 = data.Pine_006_03,
                Pine_006_04 = data.Pine_006_04,
                Pine_007_01 = data.Pine_007_01,
                Pine_007_RootStump = data.Pine_007_RootStump,
                PineDead_02 = data.PineDead_02,
                PineDead_03 = data.PineDead_03,
                TreeDead_01 = data.TreeDead_01,
                Broadleaf_Shrub_01_Var1_Prefab = data.Broadleaf_Shrub_01_Var1_Prefab,
                Broadleaf_Shrub_01_Var2_Prefab = data.Broadleaf_Shrub_01_Var2_Prefab,
                Broadleaf_Shrub_01_Var3_Prefab = data.Broadleaf_Shrub_01_Var3_Prefab,
                Broadleaf_Shrub_01_Var4_Prefab = data.Broadleaf_Shrub_01_Var4_Prefab,
                Broadleaf_Shrub_01_Var5_Prefab = data.Broadleaf_Shrub_01_Var5_Prefab,
                Broadleaf_Shrub_01_Var6_Prefab = data.Broadleaf_Shrub_01_Var6_Prefab,
                Bush_Twig_01_Var3_Prefab = data.Bush_Twig_01_Var3_Prefab,
                Bush_Twig_01_Var4_Prefab = data.Bush_Twig_01_Var4_Prefab,
                Clover_01_Var1_Prefab = data.Clover_01_Var1_Prefab,
                Clover_01_Var2_Prefab = data.Clover_01_Var2_Prefab,
                Clover_01_Var3_Prefab = data.Clover_01_Var3_Prefab,
                Clover_01_Var4_Prefab = data.Clover_01_Var4_Prefab,
                Fern_var01_Prefab = data.Fern_var01_Prefab,
                Fern_var02_Prefab = data.Fern_var02_Prefab,
                Fern_var03_Prefab = data.Fern_var03_Prefab,
                GreenBush_Var01_Prefab = data.GreenBush_Var01_Prefab,
                Juniper_Bush_01_Var1 = data.Juniper_Bush_01_Var1,
                Juniper_Bush_01_Var2 = data.Juniper_Bush_01_Var2,
                Juniper_Bush_01_Var3 = data.Juniper_Bush_01_Var3,
                Juniper_Bush_01_Var4 = data.Juniper_Bush_01_Var4,
                Juniper_Bush_01_Var5 = data.Juniper_Bush_01_Var5,
                Meadow_Grass_01_Var1 = data.Meadow_Grass_01_Var1,
                Meadow_Grass_01_Var2 = data.Meadow_Grass_01_Var2,
                Meadow_Grass_01_Var3 = data.Meadow_Grass_01_Var3,
                Meadow_Grass_01_Var4 = data.Meadow_Grass_01_Var4,
                Meadow_Grass_01_Var5 = data.Meadow_Grass_01_Var5,
                Meadow_Grass_01_Var6 = data.Meadow_Grass_01_Var6,
                PineGroundScatter01_Var1_Prefab = data.PineGroundScatter01_Var1_Prefab,
                PineGroundScatter01_Var2_Prefab = data.PineGroundScatter01_Var2_Prefab,
                RedBush_Var1_Prefab = data.RedBush_Var1_Prefab,
                Bush_b1_4x4x4_PF = data.Bush_b1_4x4x4_PF,
                Bush_b1_6x8x6_PF = data.Bush_b1_6x8x6_PF,
                Bush_qilgP2_6x6x4_PF = data.Bush_qilgP2_6x6x4_PF,
                Bush_qilgY2_2x2x4_PF = data.Bush_qilgY2_2x2x4_PF,
                GrassGreen_qheqG2_01 = data.GrassGreen_qheqG2_01,
                GrassGreen_qheqG2_02 = data.GrassGreen_qheqG2_02,
                GrassGreen_qheqG2_03 = data.GrassGreen_qheqG2_03,
                GrassGreen_qheqG2_04 = data.GrassGreen_qheqG2_04,
                PH_Plant_Perennials_a2_1x1x2_A_Prefab = data.PH_Plant_Perennials_a2_1x1x2_A_Prefab,
                PH_Plant_Perennials_a2_1x1x2_B_Prefab = data.PH_Plant_Perennials_a2_1x1x2_B_Prefab,
                PH_Plant_Perennials_a2_1x1x2_C_Prefab = data.PH_Plant_Perennials_a2_1x1x2_C_Prefab,
                PH_Plant_Perennials_a2_1x1x2_Prefab = data.PH_Plant_Perennials_a2_1x1x2_Prefab,
                PH_Plant_Perennials_a4_1x1x0_PF = data.PH_Plant_Perennials_a4_1x1x0_PF,
                Rock_Granite_rcCwC_Prefab = data.Rock_Granite_rcCwC_Prefab,
                Rock_Granite_reFto_brighter = data.Rock_Granite_reFto_brighter,
                Aset_rock_granite_M_rgAsy = data.Aset_rock_granite_M_rgAsy,
                Rock_Sandstone_plras = data.Rock_Sandstone_plras,
                Wood_Branch_pjxuR_Prefab = data.Wood_Branch_pjxuR_Prefab,
                Wood_branch_S_pcyeE_Prefab = data.Wood_branch_S_pcyeE_Prefab,
                Wood_log_M_qdtdP_Prefab = data.Wood_log_M_qdtdP_Prefab,
                Wood_Log_qdhxa_Prefab = data.Wood_Log_qdhxa_Prefab,
                Wood_Log_rhfdj = data.Wood_Log_rhfdj,
                Wood_Root_rkswd_Prefab = data.Wood_Root_rkswd_Prefab,
                wood_log_M_rfgxx_Prefab = data.wood_log_M_rfgxx_Prefab,
                Aset_wood_log_M_rfixH_prefab = data.Aset_wood_log_M_rfixH_prefab,
                Rock_Passagecave_A = data.Rock_Passagecave_A,
                FlatRock_01 = data.FlatRock_01,
                Rock_06 = data.Rock_06,
                Rock_06_B = data.Rock_06_B,
                Rock_31 = data.Rock_31,
                Rock_31_B = data.Rock_31_B,
                Rock_31_Darker = data.Rock_31_Darker,
                RockSlussen_01 = data.RockSlussen_01,
                SmallCliff_01_partA = data.SmallCliff_01_partA,
                SmallCliff_A = data.SmallCliff_A,
                SmallCliff_A_Brown = data.SmallCliff_A_Brown,
                Cliff_01_Curved_A_Prefab = data.Cliff_01_Curved_A_Prefab,
                Cliff_01_Prefab = data.Cliff_01_Prefab,
                HE_bark_strukture_A02_Prefab = data.HE_bark_strukture_A02_Prefab,
                HE_bark_strukture_A05_Prefab = data.HE_bark_strukture_A05_Prefab,
                HE_Portal_Modul_A_Prefab = data.HE_Portal_Modul_A_Prefab,
                HE_Portal_Modul_C_Prefab = data.HE_Portal_Modul_C_Prefab,
                HE_Portal_Modul_D_Prefab = data.HE_Portal_Modul_D_Prefab,
                sticks_debris_00_prefab = data.sticks_debris_00_prefab,
                Tree_type_003 = data.Tree_type_003,
                Tree_type_004 = data.Tree_type_004,
                Tree_type_005 = data.Tree_type_005,
                P_OBJ_Bench_01 = data.P_OBJ_Bench_01,
                P_OBJ_flower = data.P_OBJ_flower,
                P_OBJ_fountain_001 = data.P_OBJ_fountain_001,
                P_OBJ_gear_shop = data.P_OBJ_gear_shop,
                P_OBJ_house_001 = data.P_OBJ_house_001,
                P_OBJ_house_002 = data.P_OBJ_house_002,
                P_OBJ_item_shop = data.P_OBJ_item_shop,
                P_OBJ_pillar_001 = data.P_OBJ_pillar_001,
                P_OBJ_pillar_002 = data.P_OBJ_pillar_002,
                P_OBJ_pillar_003 = data.P_OBJ_pillar_003,
                P_OBJ_sailboat_01 = data.P_OBJ_sailboat_01,
                P_OBJ_sailboat_dock_001 = data.P_OBJ_sailboat_dock_001,
                P_OBJ_streetlight_001 = data.P_OBJ_streetlight_001,
                P_OBJ_streetlight_002 = data.P_OBJ_streetlight_002,
                P_OBJ_streetlight_003 = data.P_OBJ_streetlight_003,
                P_OBJ_windmill_01 = data.P_OBJ_windmill_01,
                P_OBJ_windmill_02 = data.P_OBJ_windmill_02
            });
            if (!m_EntityManager.HasComponent<NewDataTag>(m_Builder))
            {
                m_EntityManager.AddComponent<NewDataTag>(m_Builder);
            }
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

    bool FindBuilderEntity()
    {
        if (m_EntityManager.Exists(m_Builder))
        {
            return true;
        }
        NativeArray<Entity> entities = m_EntityManager.GetAllEntities();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (m_EntityManager.HasComponent<Data>(entity))
            {
                m_Builder = entity;
                return true;
            }
        }

        return false;
    }

    //public Entity GetRiver()
    //{
    //    return m_River;
    //}
    #endregion

}
