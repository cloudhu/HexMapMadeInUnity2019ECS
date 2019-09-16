using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 六边形单元系统
/// </summary>
//[DisableAutoCreation]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public class CellSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    private EntityQuery m_CellGroup;
    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        var query = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<Cell>(), ComponentType.ReadOnly<NewDataTag>(),ComponentType.ReadOnly<Neighbors>(), ComponentType.ReadOnly<River>(), ComponentType.ReadOnly<RoadBools>()},
            None= new ComponentType[] { ComponentType.ReadOnly<UpdateData>() }
        };
        m_CellGroup = GetEntityQuery(query);
    }

    /// <summary>
    /// 计算六边形单元的顶点和颜色
    /// </summary>
    //[BurstCompile]//Unity2019.1.14f1会报错，Unity2019.1.12f1则不会
    struct CalculateJob : IJobForEachWithEntity<Cell,NewDataTag,Neighbors,River,RoadBools> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index,[ReadOnly] ref Cell cellData,[ReadOnly]ref NewDataTag tag, [ReadOnly]ref Neighbors neighbors,[ReadOnly] ref River river,[ReadOnly] ref RoadBools roadBools)
        {
            #region InitData初始化数据

            //0.添加动态缓存
            DynamicBuffer<ColorBuffer> colorBuffer = CommandBuffer.AddBuffer<ColorBuffer>(index, entity);
            DynamicBuffer<VertexBuffer> vertexBuffer = CommandBuffer.AddBuffer<VertexBuffer>(index, entity);
            //用于河流的动态缓存
            DynamicBuffer<UvBuffer> riverUvBuffer = CommandBuffer.AddBuffer<UvBuffer>(index, entity);
            DynamicBuffer<RiverBuffer> riverBuffers = CommandBuffer.AddBuffer<RiverBuffer>(index, entity);

            //用于道路的动态缓存
            DynamicBuffer<RoadBuffer> roadBuffers = CommandBuffer.AddBuffer<RoadBuffer>(index, entity);
            DynamicBuffer<RoadUvBuffer> roadUvs= CommandBuffer.AddBuffer<RoadUvBuffer>(index, entity);
            //水体
            DynamicBuffer<WaterBuffer> waterBuffers = CommandBuffer.AddBuffer<WaterBuffer>(index, entity);
            DynamicBuffer<WaterShoreBuffer> shoreBuffers = CommandBuffer.AddBuffer<WaterShoreBuffer>(index, entity);
            DynamicBuffer<ShoreUvBuffer> shoreUvs = CommandBuffer.AddBuffer<ShoreUvBuffer>(index, entity);
            DynamicBuffer<EstuaryBuffer> estuaryBuffers = CommandBuffer.AddBuffer<EstuaryBuffer>(index, entity);
            DynamicBuffer<EstuaryUvBuffer> estuaryUvs = CommandBuffer.AddBuffer<EstuaryUvBuffer>(index, entity);
            DynamicBuffer<EstuaryUvsBuffer> estuaryUvs2 = CommandBuffer.AddBuffer<EstuaryUvsBuffer>(index, entity);
            //1.获取当前单元的中心位置
            Vector3 currCellCenter = cellData.Position;
            //缓存当前单元的颜色
            Color currCellColor = cellData.Color;
            //当前单元的海拔
            int elevation = cellData.Elevation;
            ////保存需要混合的颜色，使用数组[]是为了方便循环
            Color[] blendColors = new Color[6];
            blendColors[0] = neighbors.NEColor;
            blendColors[1] = neighbors.EColor;
            blendColors[2] = neighbors.SEColor;
            blendColors[3] = neighbors.SWColor;
            blendColors[4] = neighbors.WColor;
            blendColors[5] = neighbors.NWColor;
            //6个方向相邻单元的索引
            int[] directionIndex = new int[6];
            directionIndex[0] = neighbors.NEIndex;
            directionIndex[1] = neighbors.EIndex;
            directionIndex[2] = neighbors.SEIndex;
            directionIndex[3] = neighbors.SWIndex;
            directionIndex[4] = neighbors.WIndex;
            directionIndex[5] = neighbors.NWIndex;
            //6个方向相邻单元的海拔
            int[] elevations = new int[6];
            elevations[0] = neighbors.NEElevation;
            elevations[1] = neighbors.EElevation;
            elevations[2] = neighbors.SEElevation;
            elevations[3] = neighbors.SWElevation;
            elevations[4] = neighbors.WElevation;
            elevations[5] = neighbors.NWElevation;
            //6个方向上的道路
            bool[] roads = new bool[6];
            roads[0] = roadBools.NEHasRoad;
            roads[1] = roadBools.EHasRoad;
            roads[2] = roadBools.SEHasRoad;
            roads[3] = roadBools.SWHasRoad;
            roads[4] = roadBools.WHasRoad;
            roads[5] = roadBools.NWHasRoad;
            //6个方向的邻居位置
            Vector3[] positions = new Vector3[6];
            positions[0] = neighbors.NEPosition;
            positions[1] = neighbors.EPosition;
            positions[2] = neighbors.SEPosition;
            positions[3] = neighbors.SWPosition;
            positions[4] = neighbors.WPosition;
            positions[5] = neighbors.NWPosition;
            //6个方向是否在水下
            bool[] neighborIsUnderWater = new bool[6];
            neighborIsUnderWater[0] = neighbors.NEIsUnderWater;
            neighborIsUnderWater[1] = neighbors.EIsUnderWater;
            neighborIsUnderWater[2] = neighbors.SEIsUnderWater;
            neighborIsUnderWater[3] = neighbors.SWIsUnderWater;
            neighborIsUnderWater[4] = neighbors.WIsUnderWater;
            neighborIsUnderWater[5] = neighbors.NWIsUnderWater;
            //植物数组
            Entity[][] entities = new Entity[3][];
            entities[0] = new Entity[14];
            entities[1] = new Entity[14];
            entities[2] = new Entity[14];
            entities[0][0] = cellData.PalmTree;
            entities[0][1] = cellData.Grass;
            entities[0][2] = cellData.Stumb;
            entities[0][3] = cellData.Pine_002_L;
            entities[0][4] = cellData.Pine_002_M2;
            entities[0][5] = cellData.Pine_002_M3;
            entities[0][6] = cellData.Pine_002_M;
            entities[0][7] = cellData.Pine_002_S2;
            entities[0][8] = cellData.Pine_002_U2;
            entities[0][9] = cellData.Pine_002_U;
            entities[0][10] = cellData.Pine_002_XL;
            entities[0][11] = cellData.Pine_002_XXL;
            entities[0][12] = cellData.Pine_004_01;
            entities[0][13] = cellData.Pine_004_02;
            entities[1][0] = cellData.Pine_004_03;
            entities[1][1] = cellData.Pine_004_04;
            entities[1][2] = cellData.Pine_004_05;
            entities[1][3] = cellData.Pine_004_06;
            entities[1][4] = cellData.Pine_004_Clump01A;
            entities[1][5] = cellData.Pine_004_Clump01B;
            entities[1][6] = cellData.Pine_004_Clump02A;
            entities[1][7] = cellData.Pine_004_Clump02B;
            entities[1][8] = cellData.Pine_004_Clump02C;
            entities[1][9] = cellData.Pine_005_01;
            entities[1][10] = cellData.Pine_005_02;
            entities[1][11] = cellData.Pine_006_01;
            entities[1][12] = cellData.Pine_006_02;
            entities[1][13] = cellData.Pine_006_03;
            entities[2][0] = cellData.Pine_006_04;
            entities[2][1] = cellData.Pine_007_01;
            entities[2][2] = cellData.Pine_007_RootStump;
            entities[2][3] = cellData.PineDead_02;
            entities[2][4] = cellData.PineDead_03;
            entities[2][5] = cellData.TreeDead_01;
            entities[2][6] = cellData.Broadleaf_Shrub_01_Var1_Prefab;
            entities[2][7] = cellData.Broadleaf_Shrub_01_Var2_Prefab;
            entities[2][8] = cellData.Broadleaf_Shrub_01_Var3_Prefab;
            entities[2][9] = cellData.Broadleaf_Shrub_01_Var4_Prefab;
            entities[2][10] = cellData.Broadleaf_Shrub_01_Var5_Prefab;
            entities[2][11] = cellData.Broadleaf_Shrub_01_Var6_Prefab;
            entities[2][12] = cellData.Bush_Twig_01_Var3_Prefab;
            entities[2][13] = cellData.Bush_Twig_01_Var4_Prefab;
            //农场数组
            Entity[][] farmEntities = new Entity[3][];
            farmEntities[0] = new Entity[14];
            farmEntities[1] = new Entity[14];
            farmEntities[2] = new Entity[14];
            farmEntities[0][0] = cellData.Clover_01_Var1_Prefab;
            farmEntities[0][1] = cellData.Clover_01_Var2_Prefab;
            farmEntities[0][2] = cellData.Clover_01_Var3_Prefab; 
            farmEntities[0][3] = cellData.Clover_01_Var4_Prefab;
            farmEntities[0][4] = cellData.Fern_var01_Prefab;
            farmEntities[0][5] = cellData.Fern_var02_Prefab;
            farmEntities[0][6] = cellData.Fern_var03_Prefab;
            farmEntities[0][7] = cellData.GreenBush_Var01_Prefab;
            farmEntities[0][8] = cellData.Juniper_Bush_01_Var1;
            farmEntities[0][9] = cellData.Juniper_Bush_01_Var2;
            farmEntities[0][10] = cellData.Juniper_Bush_01_Var3;
            farmEntities[0][11] = cellData.Juniper_Bush_01_Var4;
            farmEntities[0][12] = cellData.Juniper_Bush_01_Var5;
            farmEntities[0][13] = cellData.Meadow_Grass_01_Var1;
            farmEntities[1][0] = cellData.Meadow_Grass_01_Var2;
            farmEntities[1][1] = cellData.Meadow_Grass_01_Var3;
            farmEntities[1][2] = cellData.Meadow_Grass_01_Var4;
            farmEntities[1][3] = cellData.Meadow_Grass_01_Var5;
            farmEntities[1][4] = cellData.Meadow_Grass_01_Var6;
            farmEntities[1][5] = cellData.PineGroundScatter01_Var1_Prefab;
            farmEntities[1][6] = cellData.PineGroundScatter01_Var2_Prefab;
            farmEntities[1][7] = cellData.RedBush_Var1_Prefab;
            farmEntities[1][8] = cellData.Bush_b1_4x4x4_PF;
            farmEntities[1][9] = cellData.Bush_b1_6x8x6_PF;
            farmEntities[1][10] = cellData.Bush_qilgP2_6x6x4_PF;
            farmEntities[1][11] = cellData.Bush_qilgY2_2x2x4_PF;
            farmEntities[1][12] = cellData.GrassGreen_qheqG2_01;
            farmEntities[1][13] = cellData.GrassGreen_qheqG2_02;
            farmEntities[2][0] = cellData.GrassGreen_qheqG2_03;
            farmEntities[2][1] = cellData.GrassGreen_qheqG2_04;
            farmEntities[2][2] = cellData.PH_Plant_Perennials_a2_1x1x2_A_Prefab;
            farmEntities[2][3] = cellData.PH_Plant_Perennials_a2_1x1x2_B_Prefab;
            farmEntities[2][4] = cellData.PH_Plant_Perennials_a2_1x1x2_C_Prefab;
            farmEntities[2][5] = cellData.PH_Plant_Perennials_a2_1x1x2_Prefab;
            farmEntities[2][6] = cellData.PH_Plant_Perennials_a4_1x1x0_PF;
            farmEntities[2][7] = cellData.Rock_Granite_rcCwC_Prefab;
            farmEntities[2][8] = cellData.Rock_Granite_reFto_brighter;
            farmEntities[2][9] = cellData.Aset_rock_granite_M_rgAsy;
            farmEntities[2][10] = cellData.Rock_Sandstone_plras;
            farmEntities[2][11] = cellData.Wood_Branch_pjxuR_Prefab;
            farmEntities[2][12] = cellData.Wood_branch_S_pcyeE_Prefab;
            farmEntities[2][13] = cellData.Wood_log_M_qdtdP_Prefab;
            //建筑数组
            Entity[][] cityEntities = new Entity[3][];
            cityEntities[0] = new Entity[14];
            cityEntities[1] = new Entity[14];
            cityEntities[2] = new Entity[14];
            cityEntities[0][0] = cellData.Wood_Log_qdhxa_Prefab;
            cityEntities[0][1] = cellData.Wood_Log_rhfdj;
            cityEntities[0][2] = cellData.Wood_Root_rkswd_Prefab;
            cityEntities[0][3] = cellData.Rock_Passagecave_A;
            cityEntities[0][4] = cellData.FlatRock_01;
            cityEntities[0][5] = cellData.Rock_06;
            cityEntities[0][6] = cellData.Rock_06_B;
            cityEntities[0][7] = cellData.Rock_31;
            cityEntities[0][8] = cellData.Rock_31_B;
            cityEntities[0][9] = cellData.Rock_31_Darker;
            cityEntities[0][10] = cellData.RockSlussen_01;
            cityEntities[0][11] = cellData.SmallCliff_01_partA;
            cityEntities[0][12] = cellData.SmallCliff_A;
            cityEntities[0][13] = cellData.SmallCliff_A_Brown;
            cityEntities[1][0] = cellData.Cliff_01_Curved_A_Prefab;
            cityEntities[1][1] = cellData.Cliff_01_Prefab;
            cityEntities[1][2] = cellData.HE_bark_strukture_A02_Prefab;
            cityEntities[1][3] = cellData.HE_bark_strukture_A05_Prefab;
            cityEntities[1][4] = cellData.HE_Portal_Modul_A_Prefab;
            cityEntities[1][5] = cellData.HE_Portal_Modul_C_Prefab;
            cityEntities[1][6] = cellData.HE_Portal_Modul_D_Prefab;
            cityEntities[1][7] = cellData.Tree_type_003;
            cityEntities[1][8] = cellData.Tree_type_004;
            cityEntities[1][9] = cellData.Tree_type_005;
            cityEntities[1][10] = cellData.P_OBJ_Bench_01;
            cityEntities[1][11] = cellData.P_OBJ_flower;
            cityEntities[1][12] = cellData.P_OBJ_gear_shop;
            cityEntities[1][13] = cellData.P_OBJ_fountain_001;
            cityEntities[2][0] = cellData.P_OBJ_house_001;
            cityEntities[2][1] = cellData.P_OBJ_house_002;
            cityEntities[2][2] = cellData.P_OBJ_item_shop;
            cityEntities[2][3] = cellData.P_OBJ_pillar_001;
            cityEntities[2][4] = cellData.P_OBJ_pillar_002;
            cityEntities[2][5] = cellData.P_OBJ_pillar_003;
            cityEntities[2][6] = cellData.P_OBJ_sailboat_01;
            cityEntities[2][7] = cellData.P_OBJ_sailboat_dock_001;
            cityEntities[2][8] = cellData.P_OBJ_streetlight_001;
            cityEntities[2][9] = cellData.P_OBJ_streetlight_002;
            cityEntities[2][10] = cellData.P_OBJ_streetlight_003;
            cityEntities[2][11] = cellData.P_OBJ_windmill_01;
            cityEntities[2][12] = cellData.P_OBJ_windmill_02;
            cityEntities[2][13] = cellData.sticks_debris_00_prefab;

            //没有水/河/路，添加随机地貌
            if (!cellData.IsUnderWater && !cellData.HasRiver && !cellData.HasRoad)
            {
                AddFeature(entities, farmEntities, cityEntities, currCellCenter, index, cellData.GreenLvl, cellData.FarmLv1, cellData.CityLvl);
            }

            #endregion

            //添加六边形单元六个方向的顶点、三角和颜色
            for (int j = 0; j < 6; j++)
            {
                #region Triangulate三角化
                //1.添加中心区域的3个顶点
                int next = (j + 1) > 5 ? 0 : (j + 1);
                EdgeVertices e = new EdgeVertices((currCellCenter + HexMetrics.SolidCorners[j]), (currCellCenter + HexMetrics.SolidCorners[next]));

                int prev = (j - 1) < 0 ? 5 : (j - 1);
                int next2 = (j + 2) <= 5 ? (j + 2) : (j - 4);
                int prev2 = (j - 2) >= 0 ? (j - 2) : (j + 4);
                //是否有河流通过
                bool hasRiverThroughEdge = HasRiverThroughEdge(river, directionIndex[j]);
                float RiverSurfaceY = (elevation + HexMetrics.WaterSurfaceElevationOffset) * HexMetrics.ElevationStep;
                bool hasRoad = roads[j];
                #region River 河流

                //如果有河流通过，则降低海拔来创造河道
                if (cellData.HasRiver)
                {
                    if (hasRiverThroughEdge)
                    {
                        e.v3.y = (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
                        //TriangulateWithRiverBeginOrEnd三角化河流的开始或结束
                        if (river.HasOutgoingRiver != river.HasIncomingRiver)
                        {
                            EdgeVertices m = new EdgeVertices(Vector3.Lerp(currCellCenter, e.v1, 0.5f), Vector3.Lerp(currCellCenter, e.v5, 0.5f));
                            m.v3.y = e.v3.y;
                            if (hasRoad)
                            {
                                TriangulateRoadSegment(m.v2, m.v3, m.v4, e.v2, e.v3, e.v4, ref roadBuffers, ref roadUvs);
                            }
                            TriangulateEdgeStrip(m, currCellColor, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                            TriangulateEdgeFan(currCellCenter, m, currCellColor, ref colorBuffer, ref vertexBuffer);
                            //隐藏处于水下的河流
                            if (!cellData.IsUnderWater)
                            {
                                TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, RiverSurfaceY, RiverSurfaceY, 0.6f, ref riverUvBuffer, ref riverBuffers, river.HasIncomingRiver);
                                Vector3 center = currCellCenter;
                                center.y = m.v2.y = m.v4.y = RiverSurfaceY;
                                AddTriangle(center, m.v2, m.v4, ref riverBuffers);
                                if (river.HasIncomingRiver)
                                {
                                    AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f), ref riverUvBuffer);
                                }
                                else
                                {
                                    AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f), ref riverUvBuffer);
                                }
                            }
                        }
                        else
                        {
                            //TriangulateWithRiver 三角化河段
                            Vector3 centerL, centerR;
                            //获取当前方向的相反方向，并判断是否有河流经过
                            if (HasRiverThroughEdge(river, directionIndex[OppositeDirection(j)]))
                            {
                                centerL = currCellCenter + HexMetrics.GetFirstSolidCorner(prev) * 0.25f;
                                centerR = currCellCenter + HexMetrics.GetSecondSolidCorner(next) * 0.25f;
                            }
                            else if (HasRiverThroughEdge(river, directionIndex[next]))
                            {
                                centerL = currCellCenter;
                                centerR = Vector3.Lerp(currCellCenter, e.v5, 2f / 3f);
                            }
                            else if (HasRiverThroughEdge(river, directionIndex[prev]))
                            {
                                centerL = Vector3.Lerp(currCellCenter, e.v1, 2f / 3f);
                                centerR = currCellCenter;
                            }
                            else if (HasRiverThroughEdge(river, directionIndex[next2]))
                            {
                                centerL = currCellCenter;
                                centerR = currCellCenter + HexMetrics.GetSolidEdgeMiddle(next) * (0.5f * HexMetrics.InnerToOuter);
                            }
                            else
                            {
                                centerL = currCellCenter + HexMetrics.GetSolidEdgeMiddle(prev) * (0.5f * HexMetrics.InnerToOuter);
                                centerR = currCellCenter;
                            }

                            Vector3 center = Vector3.Lerp(centerL, centerR, 0.5f);
                            EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f), Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);
                            m.v3.y = center.y = e.v3.y;
                            if (hasRoad)
                            {
                                TriangulateRoadSegment(m.v2, m.v3, m.v4, e.v2, e.v3, e.v4, ref roadBuffers, ref roadUvs);
                            }
                            TriangulateEdgeStrip(m, currCellColor, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddTriangle(centerL, currCellColor, m.v1, currCellColor, m.v2, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddQuad(centerL, currCellColor, center, currCellColor, m.v2, currCellColor, m.v3, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddQuad(center, currCellColor, centerR, currCellColor, m.v3, currCellColor, m.v4, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddTriangle(centerR, currCellColor, m.v4, currCellColor, m.v5, currCellColor, ref colorBuffer, ref vertexBuffer);
                            //隐藏处于水下的河流
                            if (!cellData.IsUnderWater)
                            {
                                bool reversed = river.IncomingRiver == directionIndex[j];
                                TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, RiverSurfaceY, RiverSurfaceY, 0.4f, ref riverUvBuffer, ref riverBuffers, reversed);
                                TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, RiverSurfaceY, RiverSurfaceY, 0.6f, ref riverUvBuffer, ref riverBuffers, reversed);
                            }
                        }
                    }
                    else
                    {
                        #region TriangulateAdjacentToRiver三角化河岸

                        bool prevHasRiverThrougEdge = HasRiverThroughEdge(river, directionIndex[prev]);
                        bool nextHasRiverThrougEdge = HasRiverThroughEdge(river, directionIndex[next]);
                        Vector3 center = currCellCenter;
                        if (nextHasRiverThrougEdge)
                        {
                            if (prevHasRiverThrougEdge)
                            {
                                center += HexMetrics.GetSolidEdgeMiddle(j) * (HexMetrics.InnerToOuter * 0.5f);
                            }
                            else if (HasRiverThroughEdge(river, directionIndex[prev2]))
                            {
                                center += HexMetrics.GetFirstSolidCorner(j) * 0.25f;
                            }
                        }
                        else if (prevHasRiverThrougEdge && HasRiverThroughEdge(river, directionIndex[next2]))
                        {
                            center += HexMetrics.GetSecondSolidCorner(j) * 0.25f;
                        }
                        EdgeVertices m = new EdgeVertices(
                            Vector3.Lerp(center, e.v1, 0.5f),
                            Vector3.Lerp(center, e.v5, 0.5f)
                        );
                        if (hasRoad)
                        {
                            TriangulateRoadSegment(m.v2, m.v3, m.v4, e.v2, e.v3, e.v4, ref roadBuffers, ref roadUvs);
                        }
                        else if (!cellData.IsUnderWater)
                        {
                            AddFeature(entities, farmEntities, cityEntities, (currCellCenter + e.v1 + e.v5) * (1f / 3f), index,cellData.GreenLvl, cellData.FarmLv1, cellData.CityLvl);
                        }
                        TriangulateEdgeStrip(m, currCellColor, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                        TriangulateEdgeFan(center, m, currCellColor, ref colorBuffer, ref vertexBuffer);

                        #region TriangulateRoadAdjacentToRiver三角化滨河路

                        if (hasRoad)
                        {
                            Vector2 interpolators = GetRoadInterpolators(roads[j], roads[prev], roads[next]);
                            Vector3 roadCenter = center;
                            int outgoingDirection = river.OutgoingRiver;
                            int incomingDirection = river.IncomingRiver;

                            for (int i = 0; i < 6; i++)
                            {
                                if (incomingDirection == directionIndex[i])
                                {
                                    incomingDirection = i;
                                }

                                if (outgoingDirection == directionIndex[i])
                                {
                                    outgoingDirection = i;
                                }
                            }

                            for (int i = 0; i < 1; i++)
                            {
                                int direction = river.HasIncomingRiver ? incomingDirection : outgoingDirection;
                                if (river.HasIncomingRiver != river.HasOutgoingRiver)
                                {
                                    roadCenter += HexMetrics.GetSolidEdgeMiddle(OppositeDirection(direction)) * (1f / 3f);
                                }
                                else if (incomingDirection == OppositeDirection(outgoingDirection))
                                {
                                    Vector3 corner = Vector3.zero;
                                    if (prevHasRiverThrougEdge)
                                    {
                                        if (!roads[j] && !roads[next])
                                        {
                                            continue;
                                        }
                                        corner = HexMetrics.GetSecondSolidCorner(j);
                                    }
                                    else
                                    {
                                        if (!roads[j] && !roads[prev])
                                        {
                                            continue;
                                        }
                                        corner = HexMetrics.GetFirstSolidCorner(j);
                                    }
                                    roadCenter += corner * 0.5f;
                                    center += corner * 0.25f;
                                }
                                else if (incomingDirection == (outgoingDirection - 1 < 0 ? 5 : outgoingDirection - 1))
                                {
                                    roadCenter -= HexMetrics.GetSecondCorner(incomingDirection) * 0.2f;
                                }
                                else if (incomingDirection == (outgoingDirection + 1 > 5 ? 0 : outgoingDirection + 1))
                                {
                                    roadCenter -= HexMetrics.GetFirstCorner(incomingDirection) * 0.2f;
                                }
                                else if (prevHasRiverThrougEdge && nextHasRiverThrougEdge)
                                {
                                    if (!hasRiverThroughEdge)
                                    {
                                        continue;
                                    }
                                    Vector3 offset = HexMetrics.GetSolidEdgeMiddle(j) * HexMetrics.InnerToOuter;
                                    roadCenter += offset * 0.7f;
                                    center += offset * 0.5f;
                                }
                                else
                                {
                                    int middle;
                                    if (prevHasRiverThrougEdge)
                                    {
                                        middle = next;
                                    }
                                    else if (nextHasRiverThrougEdge)
                                    {
                                        middle = prev;
                                    }
                                    else
                                    {
                                        middle = j;
                                    }
                                    if (!roads[middle] && !roads[(middle - 1 < 0 ? 5 : middle - 1)] && !roads[(middle + 1 > 5 ? 0 : middle + 1)])
                                    {
                                        continue;
                                    }
                                    roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
                                }
                                Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
                                Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
                                TriangulateRoad(roadCenter, mL, mR, e, ref roadBuffers, ref roadUvs, roads[j]);
                                if (prevHasRiverThrougEdge)
                                {
                                    TriangulateRoadEdge(roadCenter, center, mL, ref roadBuffers, ref roadUvs);
                                }
                                if (nextHasRiverThrougEdge)
                                {
                                    TriangulateRoadEdge(roadCenter, mR, center, ref roadBuffers, ref roadUvs);
                                }
                            }
                        }

                        #endregion
                        #endregion

                    }
                }
                else
                {
                    TriangulateEdgeFan(currCellCenter, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                    if (hasRoad)
                    {
                        Vector2 interpolators = GetRoadInterpolators(roads[j], roads[prev], roads[next]);

                        TriangulateRoad(currCellCenter, Vector3.Lerp(currCellCenter, e.v1, interpolators.x), Vector3.Lerp(currCellCenter, e.v5, interpolators.y), e, ref roadBuffers, ref roadUvs, roads[j]);
                    }
                    else if (!cellData.IsUnderWater)
                    {
                        AddFeature(entities, farmEntities, cityEntities, (currCellCenter + e.v1 + e.v5) * (1f / 3f), index,cellData.GreenLvl, cellData.FarmLv1, cellData.CityLvl);
                    }
                }

                #endregion

                //Connection Between 2 cells
                #region  Bridge=桥=》TriangulateConnection三角化桥连接
                //桥只连接前三个方向相邻的单元，从而避免重复连接
                if (j <= 2)
                {
                    if (directionIndex[j] == int.MinValue)
                    {//如果没有相邻的单元，则跳过循环
                        continue;
                    }

                    //添加外围桥接区域的顶点
                    Vector3 bridge = (HexMetrics.GetBridge(j));

                    bridge.y = (elevations[j] - elevation) * HexMetrics.ElevationStep;
                    EdgeVertices e2 = new EdgeVertices(e.v1 + bridge, e.v5 + bridge);
                    if (hasRiverThroughEdge)
                    {
                        float neighborRiverSurfaceY = (elevations[j] + HexMetrics.WaterSurfaceElevationOffset) * HexMetrics.ElevationStep;
                        e2.v3.y = (elevations[j] + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
                        if (!cellData.IsUnderWater)
                        {
                            if (!neighborIsUnderWater[j])
                            {
                                TriangulateRiverQuad(e.v2, e.v4, e2.v2, e2.v4, RiverSurfaceY, neighborRiverSurfaceY, 0.8f, ref riverUvBuffer, ref riverBuffers, river.HasIncomingRiver && river.IncomingRiver == directionIndex[j]);
                            }
                            else if (elevation > elevations[j]+HexMetrics.WaterLevelOffset)
                            {
                                //TriangulateWaterfallInWater三角化瀑布
                                Vector3 v1 = e.v2;
                                Vector3 v2 = e.v4;
                                Vector3 v3 = e2.v2;
                                Vector3 v4 = e2.v4;
                                v1.y = v2.y = RiverSurfaceY;
                                v3.y = v4.y = neighborRiverSurfaceY;
                                float waterY= (elevations[j]+ HexMetrics.WaterLevelOffset + HexMetrics.WaterSurfaceElevationOffset) * HexMetrics.ElevationStep;
                                float t = (waterY - neighborRiverSurfaceY) / (RiverSurfaceY - neighborRiverSurfaceY);
                                v3 = Vector3.Lerp(v3, v1, t);
                                v4 = Vector3.Lerp(v4, v2, t);
                                AddQuad(v1, v2, v3, v4,ref riverBuffers);
                                AddQuadUV(0f, 1f, 0.8f, 1f,ref riverUvBuffer);
                            }
                        }
                        else if (!neighborIsUnderWater[j] && elevation > elevations[j] + HexMetrics.WaterLevelOffset)
                        {
                            //TriangulateWaterfallInWater三角化瀑布
                            Vector3 v1 = e2.v4;
                            Vector3 v2 = e2.v2;
                            Vector3 v3 = e.v4;
                            Vector3 v4 = e.v2;
                            v1.y = v2.y = neighborRiverSurfaceY;
                            v3.y = v4.y = RiverSurfaceY;
                            float waterY = (cellData.WaterLevel + HexMetrics.WaterSurfaceElevationOffset) * HexMetrics.ElevationStep;
                            float t = (waterY - RiverSurfaceY) / (neighborRiverSurfaceY - RiverSurfaceY);
                            v3 = Vector3.Lerp(v3, v1, t);
                            v4 = Vector3.Lerp(v4, v2, t);
                            AddQuad(v1, v2, v3, v4, ref riverBuffers);
                            AddQuadUV(0f, 1f, 0.8f, 1f, ref riverUvBuffer);
                        }

                    }
                    #region 桥面
                    //判断当前单元与相邻单元的海拔高低差，如果是斜坡，则添加阶梯，平面和峭壁则无需阶梯
                    if (HexMetrics.GetEdgeType(elevation, elevations[j]) == HexMetrics.HexEdgeType.Slope)
                    {
                        TriangulateEdgeTerraces(e, e2, currCellColor, blendColors[j], ref colorBuffer, ref vertexBuffer, roads[j], ref roadBuffers, ref roadUvs);
                    }
                    else
                    {
                        Color bridgeColor = (currCellColor + blendColors[j]) * 0.5f;
                        if (roads[j])
                        {
                            TriangulateRoadSegment(e.v2, e.v3, e.v4, e2.v2, e2.v3, e2.v4, ref roadBuffers, ref roadUvs);
                        }
                        TriangulateEdgeStrip(e, currCellColor, e2, blendColors[j], ref colorBuffer, ref vertexBuffer);
                    }

                    #endregion

                    #region 桥洞

                    //添加外圈区域三向颜色混合

                    if (j <= 1 && directionIndex[next] != int.MinValue)
                    {
                        //下一个相邻单元的海拔
                        int nextElevation = elevations[next];
                        Vector3 vertex5 = e.v5 + HexMetrics.GetBridge(next);
                        vertex5.y = nextElevation * HexMetrics.ElevationStep;
                        //判断相邻的三个六边形单元的高低关系，按照最低（Bottom），左（Left），右（Right）的顺序进行三角化处理
                        if (elevation <= elevations[j])
                        {
                            if (elevation <= nextElevation)
                            {
                                //当前单元海拔最低
                                TriangulateCorner(e.v5, currCellColor, e2.v5, blendColors[j], vertex5, blendColors[next], ref colorBuffer, ref vertexBuffer, elevation, elevations[j], nextElevation);
                            }
                            else
                            {
                                TriangulateCorner(vertex5, blendColors[next], e.v5, currCellColor, e2.v5, blendColors[j], ref colorBuffer, ref vertexBuffer, nextElevation, elevation, elevations[j]);
                            }
                        }
                        else if (elevations[j] <= nextElevation)
                        {
                            TriangulateCorner(e2.v5, blendColors[j], vertex5, blendColors[next], e.v5, currCellColor, ref colorBuffer, ref vertexBuffer, elevations[j], nextElevation, elevation);
                        }
                        else
                        {
                            TriangulateCorner(vertex5, blendColors[next], e.v5, currCellColor, e2.v5, blendColors[j], ref colorBuffer, ref vertexBuffer, nextElevation, elevation, elevations[j]);
                        }

                    }

                    #endregion

                }

                #endregion

                #region TriangulateWater三角化水体

                if (cellData.IsUnderWater)
                {
                    currCellCenter = cellData.Position;
                    float WaterSurfaceY = (cellData.WaterLevel + HexMetrics.WaterSurfaceElevationOffset) * HexMetrics.ElevationStep; ;
                    currCellCenter.y = WaterSurfaceY;
                    if (directionIndex[j] != int.MinValue && elevations[j] > cellData.WaterLevel)
                    {
                        #region TriangulateWaterShore三角化水岸

                        EdgeVertices e1 = new EdgeVertices(
                            currCellCenter + HexMetrics.GetFirstWaterCorner(j),
                            currCellCenter + HexMetrics.GetSecondWaterCorner(j)
                        );
                        AddTriangle(currCellCenter, e1.v1, e1.v2,ref waterBuffers);
                        AddTriangle(currCellCenter, e1.v2, e1.v3, ref waterBuffers);
                        AddTriangle(currCellCenter, e1.v3, e1.v4, ref waterBuffers);
                        AddTriangle(currCellCenter, e1.v4, e1.v5, ref waterBuffers);

                        Vector3 center2 = positions[j];
                        center2.y = currCellCenter.y;
                        EdgeVertices e2 = new EdgeVertices(
                            center2 + HexMetrics.GetSecondSolidCorner(OppositeDirection(j)),
                            center2 + HexMetrics.GetFirstSolidCorner(OppositeDirection(j))
                        );

                        if (hasRiverThroughEdge)
                        {
                            #region TriangulateEstuary三角化河口
                            AddTriangle(e2.v1, e1.v2, e1.v1, ref shoreBuffers);
                            AddTriangle(e2.v5, e1.v5, e1.v4, ref shoreBuffers);
                            AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), ref shoreUvs);
                            AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), ref shoreUvs);

                            AddQuad(e2.v1, e1.v2, e2.v2, e1.v3,ref estuaryBuffers);
                            AddTriangle(e1.v3, e2.v2, e2.v4, ref estuaryBuffers);
                            AddQuad(e1.v3, e1.v4, e2.v4, e2.v5, ref estuaryBuffers);

                            AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f),ref estuaryUvs);
                            AddTriangleUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f), ref estuaryUvs);
                            AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), ref estuaryUvs);

                            if (river.HasIncomingRiver && river.IncomingRiver == directionIndex[j])
                            {
                                AddQuadUV2(new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f), ref estuaryUvs2);
                                AddTriangleUV2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f), ref estuaryUvs2);
                                AddQuadUV2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f), ref estuaryUvs2);
                            }
                            else
                            {
                                AddQuadUV2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f), new Vector2(0f, 0f), new Vector2(0.5f, -0.3f), ref estuaryUvs2);
                                AddTriangleUV2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f), ref estuaryUvs2);
                                AddQuadUV2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f), new Vector2(1f, 0f), new Vector2(1.5f, -0.2f), ref estuaryUvs2);
                            }

                            #endregion
                        }
                        else
                        {
                            AddQuad(e1.v1, e1.v2, e2.v1, e2.v2,ref shoreBuffers);
                            AddQuad(e1.v2, e1.v3, e2.v2, e2.v3, ref shoreBuffers);
                            AddQuad(e1.v3, e1.v4, e2.v3, e2.v4, ref shoreBuffers);
                            AddQuad(e1.v4, e1.v5, e2.v4, e2.v5, ref shoreBuffers);
                            AddQuadUV(0f, 0f, 0f, 1f,ref shoreUvs);
                            AddQuadUV(0f, 0f, 0f, 1f, ref shoreUvs);
                            AddQuadUV(0f, 0f, 0f, 1f, ref shoreUvs);
                            AddQuadUV(0f, 0f, 0f, 1f, ref shoreUvs);
                        }

                        if (directionIndex[next] != int.MinValue)
                        {
                            Vector3 v3 = positions[next] + (elevations[next]>cellData.WaterLevel?
                                             HexMetrics.GetFirstWaterCorner(prev) :
                                             HexMetrics.GetFirstSolidCorner(prev));
                            v3.y = currCellCenter.y;
                            AddTriangle(e1.v5, e2.v5, v3,ref shoreBuffers);
                            AddTriangleUV(
                                new Vector2(0f, 0f),
                                new Vector2(0f, 1f),
                                new Vector2(0f, elevations[next] > cellData.WaterLevel ? 0f : 1f), ref shoreUvs
                            );
                        }

                        #endregion
                    }
                    else
                    {
                        #region TriangulateOpenWater三角化开阔水面

                        Vector3 c1 = currCellCenter + HexMetrics.GetFirstWaterCorner(j);
                        Vector3 c2 = currCellCenter + HexMetrics.GetSecondWaterCorner(j);

                        AddTriangle(currCellCenter, c1, c2, ref waterBuffers);

                        if (j <= 2 && directionIndex[j] != int.MinValue)
                        {
                            Vector3 bridge = HexMetrics.GetWaterBridge(j);
                            Vector3 e1 = c1 + bridge;
                            Vector3 e2 = c2 + bridge;

                            AddQuad(c1, c2, e1, e2, ref waterBuffers);

                            if (j <= 1)
                            {

                                if (directionIndex[next] != int.MinValue || elevations[next] > cellData.WaterLevel)
                                {
                                    continue;
                                }
                                AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(next), ref waterBuffers);
                            }
                        }

                        #endregion
                    }

                }

                #endregion

                #endregion

            }
            //4.turn off cell system by remove NewDataTag
            CommandBuffer.RemoveComponent<NewDataTag>(index,entity);
        }

        #region HexFeatureManager地貌特征管理 

        /// <summary>
        /// 添加地貌特征物
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="jobIndex">任务索引</param>
        void AddFeature(Entity[][] platEntities, Entity[][] farmEntities, Entity[][] cityEntities, Vector3 position,int jobIndex,int greenLvl,int farmLvl,int cityLvl)
        {
            HexHash hash = HexMetrics.SampleHashGrid(position);
            Entity entity=Entity.Null;
            Entity entityA = Entity.Null;
            //找出绿色植物的预设实体
            if (greenLvl > 0)
            {
                float[] thresholds = HexMetrics.GetFeatureThresholds(greenLvl - 1);
                for (int i = 0; i < thresholds.Length; i++)
                {
                    if (hash.a < thresholds[i])
                    {
                        entity= platEntities[i][(int)(hash.d * platEntities[i].Length)];
                    }
                }
            }
            //找出农场的预设实体
            if (farmLvl>0)
            {
                float[] thresholdsFarm = HexMetrics.GetFeatureThresholds(farmLvl - 1);
                for (int i = 0; i < thresholdsFarm.Length; i++)
                {
                    if (hash.b < thresholdsFarm[i])
                    {
                        entityA = farmEntities[i][(int)(hash.d * farmEntities[i].Length)];
                    }
                }
            }
            //对农场和绿化的哈希进行比较，找出随机预设
            float usedHash = hash.a;
            if (entity != Entity.Null)
            {
                if (entityA!=Entity.Null && hash.b < hash.a)
                {
                    entity = entityA;
                    usedHash = hash.b;
                }
            }
            else if (entityA!=Entity.Null)
            {
                entity = entityA;
                usedHash = hash.b;
            }
            //找出城市建筑
            if (cityLvl > 0)
            {
                float[] thresholdsCity = HexMetrics.GetFeatureThresholds(cityLvl - 1);
                for (int i = 0; i < thresholdsCity.Length; i++)
                {
                    if (hash.c < thresholdsCity[i])
                    {
                        entityA = cityEntities[i][(int)(hash.d * cityEntities[i].Length)];
                    }
                }
            }

            if (entity != Entity.Null)
            {
                if (entityA != Entity.Null && hash.c < usedHash)
                {
                    entity = entityA;
                }
            }
            else if (entityA != Entity.Null)
            {
                entity = entityA;
            }
            else
            {
                return;
            }

            Entity instance =CommandBuffer.Instantiate(jobIndex, entity);
            //position.y += position.y * 0.01f;
            CommandBuffer.SetComponent(jobIndex, instance, new Translation {
                Value = new float3(position)
            });
            CommandBuffer.SetComponent(jobIndex, instance, new Rotation
            {
                Value = quaternion.Euler(0f, 360f * hash.e, 0f)
            });
        }

        #endregion

        #region Water水体

        //为水体添加三角
        void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, ref DynamicBuffer<WaterBuffer> waterBuffers)
        {
            waterBuffers.Add((v1));
            waterBuffers.Add((v2));
            waterBuffers.Add((v3));
        }

        void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, ref DynamicBuffer<WaterBuffer> waterBuffers)
        {
            AddTriangle(v1, v3, v2, ref waterBuffers);
            AddTriangle(v2, v3, v4, ref waterBuffers);
        }

        void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, ref DynamicBuffer<WaterShoreBuffer> waterBuffers)
        {
            waterBuffers.Add((v1));
            waterBuffers.Add((v2));
            waterBuffers.Add((v3));
        }

        void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, ref DynamicBuffer<WaterShoreBuffer> waterBuffers)
        {
            AddTriangle(v1, v3, v2, ref waterBuffers);
            AddTriangle(v2, v3, v4, ref waterBuffers);
        }

        //添加三角区UV
        void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, ref DynamicBuffer<ShoreUvBuffer> uvs)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
        }

        //添加矩形UV
        void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, ref DynamicBuffer<ShoreUvBuffer> uvs)
        {
            AddTriangleUV(uv1, uv3, uv2, ref uvs);
            AddTriangleUV(uv2, uv3, uv4, ref uvs);
        }

        private void AddQuadUV(float uMin, float uMax, float vMin, float vMax, ref DynamicBuffer<ShoreUvBuffer> uvs)
        {
            AddQuadUV(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax), ref uvs);
        }

        void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, ref DynamicBuffer<EstuaryBuffer> waterBuffers)
        {
            waterBuffers.Add((v1));
            waterBuffers.Add((v2));
            waterBuffers.Add((v3));
        }

        void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, ref DynamicBuffer<EstuaryBuffer> waterBuffers)
        {
            AddTriangle(v1, v3, v2, ref waterBuffers);
            AddTriangle(v2, v3, v4, ref waterBuffers);
        }

        //添加三角区UV
        void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, ref DynamicBuffer<EstuaryUvBuffer> uvs)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
        }

        //添加矩形UV
        void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, ref DynamicBuffer<EstuaryUvBuffer> uvs)
        {
            AddTriangleUV(uv1, uv3, uv2, ref uvs);
            AddTriangleUV(uv2, uv3, uv4, ref uvs);
        }

        private void AddQuadUV(float uMin, float uMax, float vMin, float vMax, ref DynamicBuffer<EstuaryUvBuffer> uvs)
        {
            AddQuadUV(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax), ref uvs);
        }

        //添加三角区UV
        void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, ref DynamicBuffer<EstuaryUvsBuffer> uvs)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
        }

        //添加矩形UV
        void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, ref DynamicBuffer<EstuaryUvsBuffer> uvs)
        {
            AddTriangleUV2(uv1, uv3, uv2, ref uvs);
            AddTriangleUV2(uv2, uv3, uv4, ref uvs);
        }

        private void AddQuadUV2(float uMin, float uMax, float vMin, float vMax, ref DynamicBuffer<EstuaryUvsBuffer> uvs)
        {
            AddQuadUV2(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax), ref uvs);
        }
        #endregion

        #region River

        /// <summary>
        /// 是否有河流通过当前单元的方向
        /// </summary>
        /// <param name="river">河流</param>
        /// <param name="direction">方向</param>
        /// <returns></returns>
        bool HasRiverThroughEdge(River river, int direction)
        {
            return (river.HasIncomingRiver && river.IncomingRiver == direction) ||
                   (river.HasOutgoingRiver && river.OutgoingRiver == direction);
        }

        //三角化河流
        void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, ref DynamicBuffer<UvBuffer> uvs, ref DynamicBuffer<RiverBuffer> riverBuffers, bool reversed)
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            AddQuad(v1, v2, v3, v4, ref riverBuffers);
            if (reversed)
            {
                AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v, ref uvs);
            }
            else
            {
                AddQuadUV(0f, 1f, v, v + 0.2f, ref uvs);
            }
        }

        //为河流添加矩形三角顶点
        void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, ref DynamicBuffer<RiverBuffer> riverBuffers)
        {
            AddTriangle(v1, v3, v2, ref riverBuffers);
            AddTriangle(v2, v3, v4, ref riverBuffers);
        }

        //为河流添加三角
        void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, ref DynamicBuffer<RiverBuffer> riverBuffers)
        {
            riverBuffers.Add((v1));
            riverBuffers.Add((v2));
            riverBuffers.Add((v3));
        }

        //添加三角区UV
        void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, ref DynamicBuffer<UvBuffer> uvs)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
        }

        //添加矩形UV
        void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, ref DynamicBuffer<UvBuffer> uvs)
        {
            AddTriangleUV(uv1, uv3, uv2, ref uvs);
            AddTriangleUV(uv2, uv3, uv4, ref uvs);
        }

        //添加河流区域的UV
        private void AddQuadUV(float uMin, float uMax, float vMin, float vMax, ref DynamicBuffer<UvBuffer> uvs)
        {
            AddQuadUV(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax), ref uvs);
        }

        #endregion

        #region Road
        //获取道路插值
        Vector2 GetRoadInterpolators(bool currRoad,bool prevRoad,bool nextRoad)
        {
            Vector2 interpolators;
            if (currRoad)
            {
                interpolators.x = interpolators.y = 0.5f;
            }
            else
            {
                interpolators.x = prevRoad? 0.5f : 0.25f;
                interpolators.y =nextRoad ? 0.5f : 0.25f;
            }
            return interpolators;
        }

        //三角化路边
        void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR, ref DynamicBuffer<RoadBuffer> roadBuffers, ref DynamicBuffer<RoadUvBuffer> uvs)
        {
            AddTriangle(center, mL, mR,ref roadBuffers);
            AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),ref uvs
            );
        }

        //三角化道路
        void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, ref DynamicBuffer<RoadBuffer> roadBuffers, ref DynamicBuffer<RoadUvBuffer> uvs, bool hasRoadThroughCellEdge)
        {
            if (hasRoadThroughCellEdge)
            {
                Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
                TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4, ref roadBuffers, ref uvs);
                AddTriangle(center, mL, mC, ref roadBuffers);
                AddTriangle(center, mC, mR, ref roadBuffers);
                AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), ref uvs);
                AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), ref uvs);
            }
            else
            {
                TriangulateRoadEdge(center, mL, mR,ref roadBuffers,ref uvs);
            }
        }

        //三角化路段
        void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6, ref DynamicBuffer<RoadBuffer> roadBuffers, ref DynamicBuffer<RoadUvBuffer> uvs)
        {
            AddQuad(v1, v2, v4, v5,ref roadBuffers);
            AddQuad(v2, v3, v5, v6,ref roadBuffers);
            AddQuadUV(0f, 1f, 0f, 0f,ref uvs);
            AddQuadUV(1f, 0f, 0f, 0f,ref uvs);
        }


        //为道路添加三角
        void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, ref DynamicBuffer<RoadBuffer> roadBuffers)
        {
            roadBuffers.Add((v1));
            roadBuffers.Add((v2));
            roadBuffers.Add((v3));
        }

        void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, ref DynamicBuffer<RoadBuffer> roadBuffers)
        {
            AddTriangle(v1, v3, v2, ref roadBuffers);
            AddTriangle(v2, v3, v4, ref roadBuffers);
        }


        //添加三角区UV
        void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, ref DynamicBuffer<RoadUvBuffer> uvs)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
        }

        //添加矩形UV
        void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, ref DynamicBuffer<RoadUvBuffer> uvs)
        {
            AddTriangleUV(uv1, uv3, uv2, ref uvs);
            AddTriangleUV(uv2, uv3, uv4, ref uvs);
        }

        //添加河流区域的UV
        private void AddQuadUV(float uMin, float uMax, float vMin, float vMax, ref DynamicBuffer<RoadUvBuffer> uvs)
        {
            AddQuadUV(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax), ref uvs);
        }
        #endregion

        #region Commmon

        //添加矩形三角顶点和颜色
        void AddQuad(Vector3 v1, Color c1, Vector3 v2, Color c2, Vector3 v3, Color c3, Vector3 v4, Color c4, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            Color bridgeColor = (c2 + c3) * 0.5f;
            AddTriangle(v1, c1, v3, bridgeColor, v2, bridgeColor, ref colorBuffer, ref vertexBuffer);
            AddTriangle(v2, bridgeColor, v3, bridgeColor, v4, c4, ref colorBuffer, ref vertexBuffer);
        }

        //添加三角顶点与颜色
        void AddTriangle(Vector3 v1, Color bottomColor, Vector3 v2, Color leftColor, Vector3 v3, Color rightColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            Color colorTriangle = (bottomColor + leftColor + rightColor) / 3f;
            vertexBuffer.Add((v1));
            colorBuffer.Add(colorTriangle);
            vertexBuffer.Add((v2));
            colorBuffer.Add(colorTriangle);
            vertexBuffer.Add((v3));
            colorBuffer.Add(colorTriangle);
        }
        //三角化桥面阶梯
        void TriangulateEdgeTerraces(EdgeVertices begin, EdgeVertices end, Color beginColor, Color endColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer,bool hasRoad,ref DynamicBuffer<RoadBuffer> roadBuffers, ref DynamicBuffer<RoadUvBuffer> roadUvs)
        {
            EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
            /////////////(First Step)/////////////////
            Color bridgeColor = HexMetrics.TerraceLerp(beginColor, endColor, 1);
            if (hasRoad)
            {
                TriangulateRoadSegment(begin.v2, begin.v3, begin.v4, e2.v2, e2.v3, e2.v4, ref roadBuffers, ref roadUvs);
            }
            TriangulateEdgeStrip(begin, beginColor, e2, bridgeColor, ref colorBuffer, ref vertexBuffer);
            ///////////////////(Middle Steps)///////////////////
            for (int i = 2; i < HexMetrics.TerraceSteps; i++)
            {
                EdgeVertices e1 = e2;
                Color c1 = bridgeColor;
                e2 = EdgeVertices.TerraceLerp(begin, end, i);
                bridgeColor = HexMetrics.TerraceLerp(beginColor, endColor, i);
                if (hasRoad)
                {
                    TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, ref roadBuffers, ref roadUvs);
                }
                TriangulateEdgeStrip(e1, c1, e2, bridgeColor, ref colorBuffer, ref vertexBuffer);
            }
            ///////////////(Last Step)///////////////////
            if (hasRoad)
            {
                TriangulateRoadSegment(e2.v2, e2.v3, e2.v4, end.v2, end.v3, end.v4, ref roadBuffers, ref roadUvs);
            }
            TriangulateEdgeStrip(e2, bridgeColor, end, endColor, ref colorBuffer, ref vertexBuffer);
        }

        ///桥洞三角化
        void TriangulateCorner(Vector3 bottom, Color bottomColor, Vector3 left, Color leftColor, Vector3 right, Color rightColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer, int bottomElevation, int leftElevation, int rightElevation)
        {
            HexMetrics.HexEdgeType leftEdgeType = HexMetrics.GetEdgeType(bottomElevation, leftElevation);
            HexMetrics.HexEdgeType rightEdgeType = HexMetrics.GetEdgeType(bottomElevation, rightElevation);
            if (leftEdgeType == HexMetrics.HexEdgeType.Slope)
            {
                if (rightEdgeType == HexMetrics.HexEdgeType.Slope)
                {
                    TriangulateCornerTerraces(bottom, bottomColor, left, leftColor, right, rightColor, ref colorBuffer, ref vertexBuffer);
                }
                else if (rightEdgeType == HexMetrics.HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(left, leftColor, right, rightColor, bottom, bottomColor, ref colorBuffer, ref vertexBuffer);
                }
                else
                {
                    TriangulateCornerTerracesCliff(bottom, bottomColor, left, leftColor, right, rightColor, ref colorBuffer, ref vertexBuffer, bottomElevation, leftElevation, rightElevation);
                }
            }
            else if (rightEdgeType == HexMetrics.HexEdgeType.Slope)
            {
                if (leftEdgeType == HexMetrics.HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(right, rightColor, bottom, bottomColor, left, leftColor, ref colorBuffer, ref vertexBuffer);
                }
                else
                {
                    TriangulateCornerCliffTerraces(bottom, bottomColor, left, leftColor, right, rightColor, ref colorBuffer, ref vertexBuffer, bottomElevation, leftElevation, rightElevation);
                }
            }
            else if (HexMetrics.GetEdgeType(leftElevation, rightElevation) == HexMetrics.HexEdgeType.Slope)
            {
                if (leftElevation < rightElevation)
                {
                    TriangulateCornerCliffTerraces(right, rightColor, bottom, bottomColor, left, leftColor, ref colorBuffer, ref vertexBuffer, rightElevation, bottomElevation, leftElevation);
                }
                else
                {
                    TriangulateCornerTerracesCliff(left, leftColor, right, rightColor, bottom, bottomColor, ref colorBuffer, ref vertexBuffer, leftElevation, rightElevation, bottomElevation);
                }
            }
            else
            {
                ////两个单元处于同一平面，填充桥三角补丁，添加桥三角的3个顶点
                AddTriangle(bottom, bottomColor, left, leftColor, right, rightColor, ref colorBuffer, ref vertexBuffer);
            }

        }

        //桥洞阶梯化
        void TriangulateCornerTerraces(Vector3 begin, Color beginColor, Vector3 left, Color leftColor, Vector3 right, Color rightColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            ////////////First Triangle
            Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c3 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);
            Color c4 = HexMetrics.TerraceLerp(beginColor, rightColor, 1);
            AddTriangle(begin, beginColor, v3, c3, v4, c4, ref colorBuffer, ref vertexBuffer);
            ///////////Middle Steps
            for (int i = 2; i < HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v3;
                Vector3 v2 = v4;
                Color c1 = c3;
                Color c2 = c4;
                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceLerp(beginColor, leftColor, i);
                c4 = HexMetrics.TerraceLerp(beginColor, rightColor, i);
                AddQuad(v1, c1, v2, c2, v3, c3, v4, c4, ref colorBuffer, ref vertexBuffer);
            }
            /////////Last Step
            AddQuad(v3, c3, v4, c4, left, leftColor, right, rightColor, ref colorBuffer, ref vertexBuffer);
        }

        //三角化陡峭的阶梯
        void TriangulateCornerTerracesCliff(Vector3 begin, Color beginCellColor, Vector3 left, Color leftCellColor, Vector3 right, Color rightCellColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer, int bottomElevation, int leftElevation, int rightElevation)
        {
            float b = 1f / (rightElevation - bottomElevation);
            if (b < 0)
            {
                b = -b;
            }
            Vector3 boundary = Vector3.Lerp(begin, right, b);
            Color boundaryColor = Color.Lerp(beginCellColor, rightCellColor, b);

            TriangulateBoundaryTriangle(begin, beginCellColor, left, leftCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            if (HexMetrics.GetEdgeType(leftElevation, rightElevation) == HexMetrics.HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCellColor, right, rightCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            }
            else
            {
                AddTriangle(left, leftCellColor, right, rightCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            }
        }

        ///峭壁镜像处理:左右对调
        void TriangulateCornerCliffTerraces(Vector3 begin, Color beginCellColor, Vector3 left, Color leftCellColor, Vector3 right, Color rightCellColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer, int bottomElevation, int leftElevation, int rightElevation)
        {
            float b = 1f / (leftElevation - bottomElevation);
            if (b < 0)
            {
                b = -b;
            }
            Vector3 boundary = Vector3.Lerp(begin, left, b);
            Color boundaryColor = Color.Lerp(beginCellColor, leftCellColor, b);

            TriangulateBoundaryTriangle(right, rightCellColor, begin, beginCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            if (HexMetrics.GetEdgeType(leftElevation, rightElevation) == HexMetrics.HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCellColor, right, rightCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            }
            else
            {
                AddTriangle(left, leftCellColor, right, rightCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            }
        }

        void TriangulateBoundaryTriangle(Vector3 begin, Color beginCellColor, Vector3 left, Color leftCellColor, Vector3 boundary, Color boundaryColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
            Color c2 = HexMetrics.TerraceLerp(beginCellColor, leftCellColor, 1);
            ///////////////////////First Triangle
            AddTriangle(begin, beginCellColor, v2, c2, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            ///////////////////////////Middle Triangles
            for (int i = 2; i < HexMetrics.TerraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.TerraceLerp(begin, left, i);
                c2 = HexMetrics.TerraceLerp(beginCellColor, leftCellColor, i);
                AddTriangle(v1, c1, v2, c2, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            }
            ///////////////////Last Triangle
            AddTriangle(v2, c2, left, leftCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
        }

        ///三角化扇形边缘
        void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            AddTriangle(center, color, edge.v1, color, edge.v2, color, ref colorBuffer, ref vertexBuffer);
            AddTriangle(center, color, edge.v2, color, edge.v3, color, ref colorBuffer, ref vertexBuffer);
            AddTriangle(center, color, edge.v3, color, edge.v4, color, ref colorBuffer, ref vertexBuffer);
            AddTriangle(center, color, edge.v4, color, edge.v5, color, ref colorBuffer, ref vertexBuffer);
        }

        //三角化带状边缘
        void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            AddQuad(e1.v1, c1, e1.v2, c2, e2.v1, c1, e2.v2, c2, ref colorBuffer, ref vertexBuffer);
            AddQuad(e1.v2, c1, e1.v3, c2, e2.v2, c1, e2.v3, c2, ref colorBuffer, ref vertexBuffer);
            AddQuad(e1.v3, c1, e1.v4, c2, e2.v3, c1, e2.v4, c2, ref colorBuffer, ref vertexBuffer);
            AddQuad(e1.v4, c1, e1.v5, c2, e2.v4, c1, e2.v5, c2, ref colorBuffer, ref vertexBuffer);
        }

        /// <summary>
        /// 相反方向
        /// </summary>
        /// <param name="direction">方向</param>
        /// <returns>相反方向</returns>
        int OppositeDirection(int direction)
        {
            return direction < 3 ? (direction + 3) : (direction - 3);
        }
        /// <summary>
        /// 边缘顶点
        /// </summary>
        private struct EdgeVertices {

            public Vector3 v1, v2, v3, v4, v5;

            public EdgeVertices(Vector3 corner1, Vector3 corner2)
            {
                v1 = corner1;
                v2 = Vector3.Lerp(corner1, corner2, 0.25f);
                v3 = Vector3.Lerp(corner1, corner2, 0.5f);
                v4 = Vector3.Lerp(corner1, corner2, 0.75f);
                v5 = corner2;
            }

            public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
            {
                v1 = corner1;
                v2 = Vector3.Lerp(corner1, corner2, outerStep);
                v3 = Vector3.Lerp(corner1, corner2, 0.5f);
                v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
                v5 = corner2;
            }

            public static EdgeVertices TerraceLerp(
                EdgeVertices a, EdgeVertices b, int step)
            {
                EdgeVertices result;
                result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
                result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
                result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
                result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
                result.v5 = HexMetrics.TerraceLerp(a.v5, b.v5, step);
                return result;
            }
        }

        #endregion
    }


    /// <summary>
    /// 如果有新地图,则启动任务
    /// </summary>
    /// <param name="inputDeps">依赖</param>
    /// <returns>任务句柄</returns>
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new CalculateJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),

        }.Schedule(m_CellGroup, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        job.Complete();

        if (job.IsCompleted)
        {
            Debug.Log("CalculateJob IsCompleted:" + job.IsCompleted);
            MainWorld.Instance.RenderMesh();
        }

        return job;

    }
}
