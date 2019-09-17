using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 地图数据
/// </summary>
public struct Data   : IComponentData
{
    /// <summary>
    /// X方向的单元数量
    /// </summary>
    public int CellCountX;
    /// <summary>
    /// Z方向的单元数量
    /// </summary>
    public int CellCountZ;
    /// <summary>
    /// X方向的地图块数量
    /// </summary>
    public int ChunkCountX;

    #region Plants

    public Entity PalmTree;
    public Entity Grass;
    public Entity Stumb;
    public Entity Pine_002_L;
    public Entity Pine_002_M2;
    public Entity Pine_002_M3;
    public Entity Pine_002_M;
    public Entity Pine_002_S2;
    public Entity Pine_002_U;
    public Entity Pine_002_U2;
    public Entity Pine_002_XL;
    public Entity Pine_002_XXL;
    public Entity Pine_004_01;
    public Entity Pine_004_02;
    public Entity Pine_004_03;
    public Entity Pine_004_04;
    public Entity Pine_004_05;
    public Entity Pine_004_06;
    public Entity Pine_004_Clump01A;
    public Entity Pine_004_Clump01B;
    public Entity Pine_004_Clump02A;
    public Entity Pine_004_Clump02B;
    public Entity Pine_004_Clump02C;
    public Entity Pine_005_01;
    public Entity Pine_005_02;
    public Entity Pine_006_01;
    public Entity Pine_006_02;
    public Entity Pine_006_03;
    public Entity Pine_006_04;
    public Entity Pine_007_01;
    public Entity Pine_007_RootStump;
    public Entity PineDead_02;
    public Entity PineDead_03;
    public Entity TreeDead_01;
    public Entity Broadleaf_Shrub_01_Var1_Prefab;
    public Entity Broadleaf_Shrub_01_Var2_Prefab;
    public Entity Broadleaf_Shrub_01_Var3_Prefab;
    public Entity Broadleaf_Shrub_01_Var4_Prefab;
    public Entity Broadleaf_Shrub_01_Var5_Prefab;
    public Entity Broadleaf_Shrub_01_Var6_Prefab;
    public Entity Bush_Twig_01_Var3_Prefab;
    public Entity Bush_Twig_01_Var4_Prefab;
    public Entity Clover_01_Var1_Prefab;
    public Entity Clover_01_Var2_Prefab;
    public Entity Clover_01_Var3_Prefab;
    public Entity Clover_01_Var4_Prefab;
    public Entity Fern_var01_Prefab;
    public Entity Fern_var02_Prefab;
    public Entity Fern_var03_Prefab;
    public Entity GreenBush_Var01_Prefab;
    public Entity Juniper_Bush_01_Var1;
    public Entity Juniper_Bush_01_Var2;
    public Entity Juniper_Bush_01_Var3;
    public Entity Juniper_Bush_01_Var4;
    public Entity Juniper_Bush_01_Var5;
    public Entity Meadow_Grass_01_Var1;
    public Entity Meadow_Grass_01_Var2;
    public Entity Meadow_Grass_01_Var3;
    public Entity Meadow_Grass_01_Var4;
    public Entity Meadow_Grass_01_Var5;
    public Entity Meadow_Grass_01_Var6;
    public Entity PineGroundScatter01_Var1_Prefab;
    public Entity PineGroundScatter01_Var2_Prefab;
    public Entity RedBush_Var1_Prefab;
    public Entity Bush_b1_4x4x4_PF;
    public Entity Bush_b1_6x8x6_PF;
    public Entity Bush_qilgP2_6x6x4_PF;
    public Entity Bush_qilgY2_2x2x4_PF;
    public Entity GrassGreen_qheqG2_01;
    public Entity GrassGreen_qheqG2_02;
    public Entity GrassGreen_qheqG2_03;
    public Entity GrassGreen_qheqG2_04;
    public Entity PH_Plant_Perennials_a2_1x1x2_A_Prefab;
    public Entity PH_Plant_Perennials_a2_1x1x2_B_Prefab;
    public Entity PH_Plant_Perennials_a2_1x1x2_C_Prefab;
    public Entity PH_Plant_Perennials_a2_1x1x2_Prefab;
    public Entity PH_Plant_Perennials_a4_1x1x0_PF;
    public Entity Rock_Granite_rcCwC_Prefab;
    public Entity Rock_Granite_reFto_brighter;
    public Entity Aset_rock_granite_M_rgAsy;
    public Entity Rock_Sandstone_plras;
    public Entity Wood_Branch_pjxuR_Prefab;
    public Entity Wood_branch_S_pcyeE_Prefab;
    public Entity Wood_log_M_qdtdP_Prefab;
    public Entity Wood_Log_qdhxa_Prefab;
    public Entity wood_log_M_rfgxx_Prefab;
    public Entity Aset_wood_log_M_rfixH_prefab;
    public Entity Wood_Log_rhfdj;
    public Entity Wood_Root_rkswd_Prefab;
    public Entity Rock_Passagecave_A;
    public Entity FlatRock_01;
    public Entity Rock_06;
    public Entity Rock_06_B;
    public Entity Rock_31;
    public Entity Rock_31_B;
    public Entity Rock_31_Darker;
    public Entity RockSlussen_01;
    public Entity SmallCliff_01_partA;
    public Entity SmallCliff_A;
    public Entity SmallCliff_A_Brown;
    public Entity Cliff_01_Curved_A_Prefab;
    public Entity Cliff_01_Prefab;
    public Entity HE_bark_strukture_A02_Prefab;
    public Entity HE_bark_strukture_A05_Prefab;
    public Entity HE_Portal_Modul_A_Prefab;
    public Entity HE_Portal_Modul_C_Prefab;
    public Entity HE_Portal_Modul_D_Prefab;
    public Entity sticks_debris_00_prefab;
    public Entity Tree_type_003;
    public Entity Tree_type_004;
    public Entity Tree_type_005;
    #endregion

    #region Buildings
    public Entity P_OBJ_Bench_01;
    public Entity P_OBJ_flower;
    public Entity P_OBJ_fountain_001;
    public Entity P_OBJ_gear_shop;
    public Entity P_OBJ_house_001;
    public Entity P_OBJ_house_002;
    public Entity P_OBJ_item_shop;
    public Entity P_OBJ_pillar_001;
    public Entity P_OBJ_pillar_002;
    public Entity P_OBJ_pillar_003;
    public Entity P_OBJ_sailboat_01;
    public Entity P_OBJ_sailboat_dock_001;
    public Entity P_OBJ_streetlight_001;
    public Entity P_OBJ_streetlight_002;
    public Entity P_OBJ_streetlight_003;
    public Entity P_OBJ_windmill_01;
    public Entity P_OBJ_windmill_02;
    #endregion
}

/// <summary>
/// 新数据标签
/// </summary>
public struct NewDataTag : IComponentData { }

public struct RiverRenderTag : IComponentData { }

/// <summary>
/// 单元的更新数据
/// </summary>
public struct UpdateData : IComponentData
{
    /// <summary>
    /// 单元的索引
    /// </summary>
    public int CellIndex;
    /// <summary>
    /// 新的颜色
    /// </summary>
    public Color NewColor;

    /// <summary>
    /// 海拔
    /// </summary>
    public int Elevation;
    //需要更新的相邻单元索引
    public int NEIndex;
    public int EIndex;
    public int SEIndex;
    public int SWIndex;
    public int WIndex;
    public int NWIndex;
}

/// <summary>
/// 地图块数据
/// </summary>
public struct ChunkData : IComponentData
{

    /// <summary>
    /// 地图块编号
    /// </summary>
    public int ChunkId;

    /// <summary>
    /// 地图块内索引
    /// </summary>
    public int ChunkIndex;

    /// <summary>
    /// 单元索引
    /// </summary>
    public int CellIndex;

}

/// <summary>
/// 单元数据
/// </summary>
public struct Cell : IComponentData
{
    /// <summary>
    /// 单元索引
    /// </summary>
    public int Index;

    /// <summary>
    /// 位置
    /// </summary>
    public Vector3 Position;
    /// <summary>
    /// 颜色
    /// </summary>
    public Color Color;

    /// <summary>
    /// 当前单元的海拔
    /// </summary>
    public int Elevation;

    /// <summary>
    /// 水体高度
    /// </summary>
    public float WaterLevel;

    /// <summary>
    /// 当前单元是否有河流
    /// </summary>
    public bool HasRiver;

    /// <summary>
    /// 当前单元是否有路
    /// </summary>
    public bool HasRoad;

    /// <summary>
    /// 是否被水淹没
    /// </summary>
    public bool IsUnderWater;

    /// <summary>
    /// 当前单元是否有围墙
    /// </summary>
    public bool HasWall;

    /// <summary>
    /// 绿化等级
    /// </summary>
    public int GreenLvl;

    public int FarmLv1;

    public int CityLvl;
}

/// <summary>
/// 河流数据
/// </summary>
public struct River : IComponentData
{
    /// <summary>
    /// 是否有河流进入
    /// </summary>
    public bool HasIncomingRiver;

    /// <summary>
    /// 是否有河流出
    /// </summary>
    public bool HasOutgoingRiver;

    /// <summary>
    /// 流入方向
    /// </summary>
    public int IncomingRiver;

    /// <summary>
    /// 流出方向
    /// </summary>
    public int OutgoingRiver;
}

public struct Neighbors : IComponentData
{
    //六个方向相邻单元的颜色
    public Color NEColor;
    public Color EColor;
    public Color SEColor;
    public Color SWColor;
    public Color WColor;
    public Color NWColor;
    //六个方向相邻单元的海拔
    public int NEElevation;
    public int EElevation;
    public int SEElevation;
    public int SWElevation;
    public int WElevation;
    public int NWElevation;
    //索引
    public int NEIndex;
    public int EIndex;
    public int SEIndex;
    public int SWIndex;
    public int WIndex;
    public int NWIndex;
    //6个方向的位置
    public Vector3 NEPosition;
    public Vector3 EPosition;
    public Vector3 SEPosition;
    public Vector3 SWPosition;
    public Vector3 WPosition;
    public Vector3 NWPosition;
    //6个方向相邻的单元是否处于水下
    public bool NEIsUnderWater;
    public bool EIsUnderWater;
    public bool SEIsUnderWater;
    public bool SWIsUnderWater;
    public bool WIsUnderWater;
    public bool NWIsUnderWater;
    //6个方向相邻的单元是否有墙体
    public bool NEHasWall;
    public bool EHasWall;
    public bool SEHasWall;
    public bool SWHasWall;
    public bool WHasWall;
    public bool NWHasWall;
    // 单元的六个方向是否有道路通过
    public bool NEHasRoad;
    public bool EHasRoad;
    public bool SEHasRoad;
    public bool SWHasRoad;
    public bool WHasRoad;
    public bool NWHasRoad;
}

/// <summary>
/// 单元的预设
/// </summary>
public struct Prefabs : IComponentData {

    #region Plants

    public Entity PalmTree;
    public Entity Grass;
    public Entity Stumb;
    public Entity Pine_002_L;
    public Entity Pine_002_M2;
    public Entity Pine_002_M3;
    public Entity Pine_002_M;
    public Entity Pine_002_S2;
    public Entity Pine_002_U;
    public Entity Pine_002_U2;
    public Entity Pine_002_XL;
    public Entity Pine_002_XXL;
    public Entity Pine_004_01;
    public Entity Pine_004_02;
    public Entity Pine_004_03;
    public Entity Pine_004_04;
    public Entity Pine_004_05;
    public Entity Pine_004_06;
    public Entity Pine_004_Clump01A;
    public Entity Pine_004_Clump01B;
    public Entity Pine_004_Clump02A;
    public Entity Pine_004_Clump02B;
    public Entity Pine_004_Clump02C;
    public Entity Pine_005_01;
    public Entity Pine_005_02;
    public Entity Pine_006_01;
    public Entity Pine_006_02;
    public Entity Pine_006_03;
    public Entity Pine_006_04;
    public Entity Pine_007_01;
    public Entity Pine_007_RootStump;
    public Entity PineDead_02;
    public Entity PineDead_03;
    public Entity TreeDead_01;
    public Entity Broadleaf_Shrub_01_Var1_Prefab;
    public Entity Broadleaf_Shrub_01_Var2_Prefab;
    public Entity Broadleaf_Shrub_01_Var3_Prefab;
    public Entity Broadleaf_Shrub_01_Var4_Prefab;
    public Entity Broadleaf_Shrub_01_Var5_Prefab;
    public Entity Broadleaf_Shrub_01_Var6_Prefab;
    public Entity Bush_Twig_01_Var3_Prefab;
    public Entity Bush_Twig_01_Var4_Prefab;
    public Entity Clover_01_Var1_Prefab;
    public Entity Clover_01_Var2_Prefab;
    public Entity Clover_01_Var3_Prefab;
    public Entity Clover_01_Var4_Prefab;
    public Entity Fern_var01_Prefab;
    public Entity Fern_var02_Prefab;
    public Entity Fern_var03_Prefab;
    public Entity GreenBush_Var01_Prefab;
    public Entity Juniper_Bush_01_Var1;
    public Entity Juniper_Bush_01_Var2;
    public Entity Juniper_Bush_01_Var3;
    public Entity Juniper_Bush_01_Var4;
    public Entity Juniper_Bush_01_Var5;
    public Entity Meadow_Grass_01_Var1;
    public Entity Meadow_Grass_01_Var2;
    public Entity Meadow_Grass_01_Var3;
    public Entity Meadow_Grass_01_Var4;
    public Entity Meadow_Grass_01_Var5;
    public Entity Meadow_Grass_01_Var6;
    public Entity PineGroundScatter01_Var1_Prefab;
    public Entity PineGroundScatter01_Var2_Prefab;
    public Entity RedBush_Var1_Prefab;
    public Entity Bush_b1_4x4x4_PF;
    public Entity Bush_b1_6x8x6_PF;
    public Entity Bush_qilgP2_6x6x4_PF;
    public Entity Bush_qilgY2_2x2x4_PF;
    public Entity GrassGreen_qheqG2_01;
    public Entity GrassGreen_qheqG2_02;
    public Entity GrassGreen_qheqG2_03;
    public Entity GrassGreen_qheqG2_04;
    public Entity PH_Plant_Perennials_a2_1x1x2_A_Prefab;
    public Entity PH_Plant_Perennials_a2_1x1x2_B_Prefab;
    public Entity PH_Plant_Perennials_a2_1x1x2_C_Prefab;
    public Entity PH_Plant_Perennials_a2_1x1x2_Prefab;
    public Entity PH_Plant_Perennials_a4_1x1x0_PF;
    public Entity Rock_Granite_rcCwC_Prefab;
    public Entity Rock_Granite_reFto_brighter;
    public Entity Aset_rock_granite_M_rgAsy;
    public Entity Rock_Sandstone_plras;
    public Entity Wood_Branch_pjxuR_Prefab;
    public Entity Wood_branch_S_pcyeE_Prefab;
    public Entity Wood_log_M_qdtdP_Prefab;
    public Entity Wood_Log_qdhxa_Prefab;
    public Entity wood_log_M_rfgxx_Prefab;
    public Entity Aset_wood_log_M_rfixH_prefab;
    public Entity Wood_Log_rhfdj;
    public Entity Wood_Root_rkswd_Prefab;
    public Entity Rock_Passagecave_A;
    public Entity FlatRock_01;
    public Entity Rock_06;
    public Entity Rock_06_B;
    public Entity Rock_31;
    public Entity Rock_31_B;
    public Entity Rock_31_Darker;
    public Entity RockSlussen_01;
    public Entity SmallCliff_01_partA;
    public Entity SmallCliff_A;
    public Entity SmallCliff_A_Brown;
    public Entity Cliff_01_Curved_A_Prefab;
    public Entity Cliff_01_Prefab;
    public Entity HE_bark_strukture_A02_Prefab;
    public Entity HE_bark_strukture_A05_Prefab;
    public Entity HE_Portal_Modul_A_Prefab;
    public Entity HE_Portal_Modul_C_Prefab;
    public Entity HE_Portal_Modul_D_Prefab;
    public Entity sticks_debris_00_prefab;
    public Entity Tree_type_003;
    public Entity Tree_type_004;
    public Entity Tree_type_005;
    #endregion

    #region Buildings
    public Entity P_OBJ_Bench_01;
    public Entity P_OBJ_flower;
    public Entity P_OBJ_fountain_001;
    public Entity P_OBJ_gear_shop;
    public Entity P_OBJ_house_001;
    public Entity P_OBJ_house_002;
    public Entity P_OBJ_item_shop;
    public Entity P_OBJ_pillar_001;
    public Entity P_OBJ_pillar_002;
    public Entity P_OBJ_pillar_003;
    public Entity P_OBJ_sailboat_01;
    public Entity P_OBJ_sailboat_dock_001;
    public Entity P_OBJ_streetlight_001;
    public Entity P_OBJ_streetlight_002;
    public Entity P_OBJ_streetlight_003;
    public Entity P_OBJ_windmill_01;
    public Entity P_OBJ_windmill_02;
    #endregion
}

/// <summary>
/// 动态缓存：颜色
/// </summary>
public struct ColorBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Color(ColorBuffer e) { return e.Value; }
    public static implicit operator ColorBuffer(Color e) { return new ColorBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Color Value;
}

/// <summary>
/// 顶点
/// </summary>
public struct VertexBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(VertexBuffer e) { return e.Value; }
    public static implicit operator VertexBuffer(Vector3 e) { return new VertexBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

/// <summary>
/// UV动态缓存
/// </summary>
public struct UvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(UvBuffer e) { return e.Value; }
    public static implicit operator UvBuffer(Vector2 e) { return new UvBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector2 Value;
}

/// <summary>
/// 河流
/// </summary>
public struct RiverBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(RiverBuffer e) { return e.Value; }
    public static implicit operator RiverBuffer(Vector3 e) { return new RiverBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

/// <summary>
/// 道路顶点动态缓存
/// </summary>
public struct RoadBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(RoadBuffer e) { return e.Value; }
    public static implicit operator RoadBuffer(Vector3 e) { return new RoadBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

public struct RoadUvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(RoadUvBuffer e) { return e.Value; }
    public static implicit operator RoadUvBuffer(Vector2 e) { return new RoadUvBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector2 Value;
}

/// <summary>
/// 水体顶点
/// </summary>
public struct WaterBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(WaterBuffer e) { return e.Value; }
    public static implicit operator WaterBuffer(Vector3 e) { return new WaterBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

/// <summary>
/// 水岸顶点
/// </summary>
public struct WaterShoreBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(WaterShoreBuffer e) { return e.Value; }
    public static implicit operator WaterShoreBuffer(Vector3 e) { return new WaterShoreBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

/// <summary>
/// 水岸UV
/// </summary>
public struct ShoreUvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(ShoreUvBuffer e) { return e.Value; }
    public static implicit operator ShoreUvBuffer(Vector2 e) { return new ShoreUvBuffer { Value = e }; }

    // Actual value each buffer element will store. 
    public Vector2 Value;
}

/// <summary>
/// 河口顶点动态缓存
/// </summary>
public struct EstuaryBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(EstuaryBuffer e) { return e.Value; }
    public static implicit operator EstuaryBuffer(Vector3 e) { return new EstuaryBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

public struct EstuaryUvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(EstuaryUvBuffer e) { return e.Value; }
    public static implicit operator EstuaryUvBuffer(Vector2 e) { return new EstuaryUvBuffer { Value = e }; }

    // Actual value each buffer element will store. Estuary
    public Vector2 Value;
}

/// <summary>
/// 河口UV动态缓存
/// </summary>
public struct EstuaryUvsBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(EstuaryUvsBuffer e) { return e.Value; }
    public static implicit operator EstuaryUvsBuffer(Vector2 e) { return new EstuaryUvsBuffer { Value = e }; }

    // Actual value each buffer element will store. Estuary
    public Vector2 Value;
}

/// <summary>
/// 调试动态缓存
/// </summary>
public struct DebugBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator int(DebugBuffer e) { return e.Value; }
    public static implicit operator DebugBuffer(int e) { return new DebugBuffer { Value = e }; }

    // Actual value each buffer element will store. 
    public int Value;
}

/// <summary>
/// 预设动态缓存
/// </summary>
public struct PrefabBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Entity(PrefabBuffer e) { return e.Value; }
    public static implicit operator PrefabBuffer(Entity e) { return new PrefabBuffer { Value = e }; }

    // Actual value each buffer element will store. 
    public Entity Value;
}

/// <summary>
/// 墙体顶点动态缓存
/// </summary>
public struct WallBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(WallBuffer e) { return e.Value; }
    public static implicit operator WallBuffer(Vector3 e) { return new WallBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}