using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
            All = new ComponentType[] { ComponentType.ReadOnly<Cell>(), ComponentType.ReadOnly<NewDataTag>(),ComponentType.ReadOnly<Neighbors>(),ComponentType.ReadOnly<NeighborsIndex>(), ComponentType.ReadOnly<River>(), ComponentType.ReadOnly<RoadBools>() },
            None= new ComponentType[] { ComponentType.ReadOnly<UpdateData>() }
        };
        m_CellGroup = GetEntityQuery(query);

    }

    /// <summary>
    /// 计算六边形单元的顶点和颜色
    /// </summary>
    //[BurstCompile]//Unity2019.1.14f1会报错，Unity2019.1.12f1则不会
    struct CalculateJob : IJobForEachWithEntity<Cell,NewDataTag,Neighbors,NeighborsIndex,River,RoadBools> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index,[ReadOnly] ref Cell cellData,[ReadOnly]ref NewDataTag tag, [ReadOnly]ref Neighbors neighbors, [ReadOnly]ref NeighborsIndex neighborsIndex,[ReadOnly] ref River river,[ReadOnly] ref RoadBools roadBools)
        {
            #region InitData初始化数据

            //0.获取单元索引，Execute的index不可靠，添加动态缓存
            int cellIndex = cellData.Index;
            DynamicBuffer<ColorBuffer> colorBuffer = CommandBuffer.AddBuffer<ColorBuffer>(index, entity);
            DynamicBuffer<VertexBuffer> vertexBuffer = CommandBuffer.AddBuffer<VertexBuffer>(index, entity);
            //用于河流的动态缓存
            DynamicBuffer<UvBuffer> uvBuffer = CommandBuffer.AddBuffer<UvBuffer>(index, entity);
            DynamicBuffer<RiverBuffer> riverBuffers = CommandBuffer.AddBuffer<RiverBuffer>(index, entity);

            //用于道路的动态缓存
            DynamicBuffer<RoadBuffer> roadBuffers = CommandBuffer.AddBuffer<RoadBuffer>(index, entity);
            DynamicBuffer<RoadUvBuffer> roadUvs= CommandBuffer.AddBuffer<RoadUvBuffer>(index, entity);
            //1.获取当前单元的中心位置
            Vector3 currCellCenter = cellData.Position;
            //缓存当前单元的颜色
            Color currCellColor = cellData.Color;
            //当前单元的海拔
            int elevation = cellData.Elevation;
            ////保存需要混合的颜色，使用数组[]是为了方便循环
            Color[] blendColors = new Color[6];
            blendColors[0] = neighbors.NE;
            blendColors[1] = neighbors.E;
            blendColors[2] = neighbors.SE;
            blendColors[3] = neighbors.SW;
            blendColors[4] = neighbors.W;
            blendColors[5] = neighbors.NW;
            //6个方向相邻单元的索引
            int[] directionIndex = new int[6];
            directionIndex[0] = neighborsIndex.NEIndex;
            directionIndex[1] = neighborsIndex.EIndex;
            directionIndex[2] = neighborsIndex.SEIndex;
            directionIndex[3] = neighborsIndex.SWIndex;
            directionIndex[4] = neighborsIndex.WIndex;
            directionIndex[5] = neighborsIndex.NWIndex;
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
            roads[0] = roadBools.NEBool;
            roads[1] = roadBools.EBool;
            roads[2] = roadBools.SEBool;
            roads[3] = roadBools.SWBool;
            roads[4] = roadBools.WBool;
            roads[5] = roadBools.NWBool;
            
            #endregion

            //添加六边形单元六个方向的顶点、三角和颜色
            for (int j = 0; j < 6; j++)
            {
                //1.添加中心区域的3个顶点
                int next = (j + 1) > 5 ? 0 : (j + 1);
                EdgeVertices e = new EdgeVertices((currCellCenter + HexMetrics.SolidCorners[j]), (currCellCenter + HexMetrics.SolidCorners[next]));
                
                int prev= (j - 1) < 0 ? 5 : (j - 1);
                int next2 = (j + 2) <= 5 ? (j + 2) : (j - 4);
                int prev2 = (j - 2) >= 0 ? (j - 2) : (j + 4);
                //是否有河流通过
                bool hasRiverThroughEdge = HasRiverThroughEdge(river,directionIndex[j]);
                float RiverSurfaceY= (elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;
                bool hasRoad = roads[j];
                #region River 河流

                //如果有河流通过，则降低海拔来创造河道
                if (cellData.HasRiver)
                {
                    if (hasRiverThroughEdge)
                    {
                        e.v3.y = (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
                        if (river.HasOutgoingRiver != river.HasIncomingRiver)
                        {
                            EdgeVertices m = new EdgeVertices(Vector3.Lerp(currCellCenter, e.v1, 0.5f), Vector3.Lerp(currCellCenter, e.v5, 0.5f));
                            m.v3.y = e.v3.y;
                            if (hasRoad)
                            {
                                TriangulateRoadSegment(m.v2, m.v3, m.v4, e.v2, e.v3, e.v4,ref roadBuffers,ref roadUvs);
                            }
                            TriangulateEdgeStrip(m, currCellColor, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                            TriangulateEdgeFan(currCellCenter, m, currCellColor, ref colorBuffer, ref vertexBuffer);
                            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, RiverSurfaceY, RiverSurfaceY, 0.6f, ref uvBuffer, ref riverBuffers, river.HasIncomingRiver);
                            Vector3 center = currCellCenter;
                            center.y = m.v2.y = m.v4.y = RiverSurfaceY;
                            AddTriangle(center, m.v2, m.v4, ref riverBuffers);
                            if (river.HasIncomingRiver)
                            {
                                AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f), ref uvBuffer);
                            }
                            else
                            {
                                AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f), ref uvBuffer);
                            }
                        }
                        else
                        {
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
                            bool reversed = river.IncomingRiver == directionIndex[j];
                            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, RiverSurfaceY, RiverSurfaceY, 0.4f, ref uvBuffer, ref riverBuffers, reversed);
                            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, RiverSurfaceY, RiverSurfaceY, 0.6f, ref uvBuffer, ref riverBuffers, reversed);
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
                                else if (incomingDirection == (outgoingDirection - 1 < 0?5: outgoingDirection - 1))
                                {
                                    roadCenter -= HexMetrics.GetSecondCorner(incomingDirection) * 0.2f;
                                }
                                else if (incomingDirection == (outgoingDirection + 1 > 5 ?0: outgoingDirection + 1 ))
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
                                    if (!roads[middle] && !roads[(middle-1<0?5:middle-1)] && !roads[(middle+1>5?0:middle+1)])
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
                        Vector2 interpolators = GetRoadInterpolators(roads[j], roads[prev],roads[next]);

                        TriangulateRoad(currCellCenter, Vector3.Lerp(currCellCenter, e.v1, interpolators.x), Vector3.Lerp(currCellCenter, e.v5, interpolators.y), e,ref roadBuffers,ref roadUvs, roads[j]);
                    }
                }

                #endregion

                //Connection Between 2 cells
                #region  Bridge=桥
                //桥只连接前三个方向相邻的单元，从而避免重复连接
                if (j <= 2)
                {
                    if (directionIndex[j] == int.MinValue)
                    {//如果没有相邻的单元，则跳过循环
                        continue;
                    }

                    //添加外围桥接区域的顶点
                    Vector3 bridge = (HexMetrics.GetBridge(j));

                    bridge.y=(elevations[j]-elevation) * HexMetrics.ElevationStep;
                    EdgeVertices e2 = new EdgeVertices(e.v1 + bridge, e.v5+ bridge);
                    if (hasRiverThroughEdge)
                    {
                        float neighborRiverSurfaceY = (elevations[j] + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;
                        e2.v3.y= (elevations[j] + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
                        TriangulateRiverQuad(e.v2, e.v4, e2.v2, e2.v4, RiverSurfaceY, neighborRiverSurfaceY,0.8f,ref uvBuffer,ref riverBuffers, river.HasIncomingRiver && river.IncomingRiver == directionIndex[j]);
                    }
                    #region 桥面
                    //判断当前单元与相邻单元的海拔高低差，如果是斜坡，则添加阶梯，平面和峭壁则无需阶梯
                    if (HexMetrics.GetEdgeType(elevation, elevations[j]) == HexMetrics.HexEdgeType.Slope)
                    {
                        TriangulateEdgeTerraces(e,e2,currCellColor,blendColors[j],ref colorBuffer, ref vertexBuffer,roads[j],ref roadBuffers,ref roadUvs);
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
                                TriangulateCorner(e.v5, currCellColor, e2.v5, blendColors[j], vertex5, blendColors[next],ref colorBuffer,ref vertexBuffer,elevation, elevations[j], nextElevation);
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

            }
            //4.turn off cell system by remove NewDataTag
            CommandBuffer.RemoveComponent<NewDataTag>(index,entity);
        }


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
