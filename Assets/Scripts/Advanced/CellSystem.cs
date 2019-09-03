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
            All = new ComponentType[] { ComponentType.ReadOnly<Cell>(), ComponentType.ReadOnly<NewDataTag>(),ComponentType.ReadOnly<Neighbors>(),ComponentType.ReadOnly<NeighborsIndex>(), ComponentType.ReadOnly<River>() },
            None= new ComponentType[] { ComponentType.ReadOnly<UpdateData>() }
        };
        m_CellGroup = GetEntityQuery(query);

    }

    /// <summary>
    /// 计算六边形单元的顶点和颜色
    /// </summary>
    //[BurstCompile]//Unity2019.1.14f1会报错，Unity2019.1.12f1则不会
    struct CalculateJob : IJobForEachWithEntity<Cell,NewDataTag,Neighbors,NeighborsIndex,River> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index,[ReadOnly] ref Cell cellData,[ReadOnly]ref NewDataTag tag, [ReadOnly]ref Neighbors neighbors, [ReadOnly]ref NeighborsIndex neighborsIndex,[ReadOnly] ref River river)
        {
            //0.获取单元索引，Execute的index不可靠，添加动态缓存
            int cellIndex = cellData.Index;
            DynamicBuffer<ColorBuffer> colorBuffer = CommandBuffer.AddBuffer<ColorBuffer>(index, entity);
            DynamicBuffer<VertexBuffer> vertexBuffer = CommandBuffer.AddBuffer<VertexBuffer>(index, entity);
            //用于河流的动态缓存
            DynamicBuffer<UvBuffer> uvBuffer = CommandBuffer.AddBuffer<UvBuffer>(index, entity);
            DynamicBuffer<RiverBuffer> riverBuffers = CommandBuffer.AddBuffer<RiverBuffer>(index, entity);
            
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
            //前3个方向相邻单元的索引
            int[] directionIndex = new int[6];
            directionIndex[0] = neighborsIndex.NEIndex;
            directionIndex[1] = neighborsIndex.EIndex;
            directionIndex[2] = neighborsIndex.SEIndex;
            directionIndex[3] = neighborsIndex.SWIndex;
            directionIndex[4] = neighborsIndex.WIndex;
            directionIndex[5] = neighborsIndex.NWIndex;
            //前三个方向相邻单元的海拔
            int[] elevations = new int[6];
            elevations[0] = neighbors.NEElevation;
            elevations[1] = neighbors.EElevation;
            elevations[2] = neighbors.SEElevation;
            elevations[3] = neighbors.SWElevation;
            elevations[4] = neighbors.WElevation;
            elevations[5] = neighbors.NWElevation;
            //添加六边形单元六个方向的顶点、三角和颜色
            for (int j = 0; j < 6; j++)
            {
                //1.添加中心区域的3个顶点
                int next = (j + 1) > 5 ? 0 : (j + 1);
                Vector3 vertex1 = (currCellCenter + HexMetrics.SolidCorners[j]);
                Vector3 vertex2 = (currCellCenter + HexMetrics.SolidCorners[next]);
                EdgeVertices e = new EdgeVertices(vertex1, vertex2);
                
                int prev= (j - 1) < 0 ? 5 : (j - 1);
                int next2 = (j + 2) <= 5 ? (j + 2) : (j - 4);
                int prev2 = (j - 2) >= 0 ? (j - 2) : (j + 4);
                //是否有河流通过
                bool hasRiverThroughEdge = HasRiverThroughEdge(river,directionIndex[j]);
                float RiverSurfaceY= (elevation + HexMetrics.riverSurfaceElevationOffset) * HexMetrics.elevationStep;

                //如果有河流通过，则降低海拔来创造河道
                if (cellData.HasRiver)
                {
                    if (hasRiverThroughEdge)
                    {
                        e.v3.y = (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
                        if (river.HasOutgoingRiver!= river.HasIncomingRiver)
                        {
                            //TriangulateWithRiverBeginOrEnd(directions[j], directions[prev], directions[next], currCellColor, currCellCenter, e, ref colorBuffer, ref vertexBuffer);
                            EdgeVertices m = new EdgeVertices(Vector3.Lerp(currCellCenter, vertex1, 0.5f), Vector3.Lerp(currCellCenter, vertex2, 0.5f));
                            m.v3.y = e.v3.y;
                            TriangulateEdgeStrip(m, currCellColor, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                            TriangulateEdgeFan(currCellCenter, m, currCellColor, ref colorBuffer, ref vertexBuffer);
                            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, RiverSurfaceY, RiverSurfaceY,0.6f, ref uvBuffer, ref riverBuffers, river.HasIncomingRiver);
                            Vector3 center = currCellCenter;
                            center.y = m.v2.y = m.v4.y = RiverSurfaceY;
                            AddTriangle(center, m.v2, m.v4,ref riverBuffers);
                            if (river.HasIncomingRiver)
                            {
                                AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f),ref uvBuffer);
                            }
                            else
                            {
                                AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f),ref uvBuffer);
                            }
                        }
                        else
                        {
                            //获取当前方向的相反方向，并判断是否有河流经过
                            bool oppositeHasRiverThroughEdge = HasRiverThroughEdge(river, directionIndex[OppositeDirection(j)]);
                            //TriangulateWithRiver(direction, directions[prev], directions[next], currCellColor, currCellCenter, e, ref colorBuffer, ref vertexBuffer, oppositeHasRiverThroughEdge);
                            Vector3 centerL, centerR;
                            if (oppositeHasRiverThroughEdge)
                            {
                                centerL = currCellCenter + HexMetrics.GetFirstSolidCorner(prev) * 0.25f;
                                centerR = currCellCenter + HexMetrics.GetSecondSolidCorner(next) * 0.25f;
                            }else if (HasRiverThroughEdge(river, directionIndex[next]))
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
                                centerR = currCellCenter + HexMetrics.GetSolidEdgeMiddle(next) * (0.5f * HexMetrics.innerToOuter);
                            }
                            else
                            {
                                centerL = currCellCenter + HexMetrics.GetSolidEdgeMiddle(prev) * (0.5f * HexMetrics.innerToOuter);
                                centerR = currCellCenter;
                            }

                            Vector3 center = Vector3.Lerp(centerL, centerR, 0.5f);
                            EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f), Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);
                            m.v3.y = center.y = e.v3.y;
                            TriangulateEdgeStrip(m, currCellColor, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddTriangle(centerL, currCellColor, m.v1, currCellColor, m.v2, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddQuad(centerL, currCellColor, center, currCellColor, m.v2, currCellColor, m.v3, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddQuad(center, currCellColor, centerR, currCellColor, m.v3, currCellColor, m.v4, currCellColor, ref colorBuffer, ref vertexBuffer);
                            AddTriangle(centerR, currCellColor, m.v4, currCellColor, m.v5, currCellColor, ref colorBuffer, ref vertexBuffer);
                            bool reversed = river.IncomingRiver == directionIndex[j];
                            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, RiverSurfaceY, RiverSurfaceY, 0.4f, ref uvBuffer, ref riverBuffers, reversed);
                            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, RiverSurfaceY, RiverSurfaceY, 0.6f,ref uvBuffer, ref riverBuffers, reversed);
                        }
                    }
                    else
                    {
                        if (HasRiverThroughEdge(river,directionIndex[next]))
                        {
                            if (HasRiverThroughEdge(river, directionIndex[prev]))
                            {
                                currCellCenter += HexMetrics.GetSolidEdgeMiddle(j)*(HexMetrics.innerToOuter * 0.5f);
                            }
                            else if (HasRiverThroughEdge(river,directionIndex[prev2]))
                            {
                                currCellCenter += HexMetrics.GetFirstSolidCorner(j) * 0.25f;
                            }
                        }
                        else if (HasRiverThroughEdge(river,directionIndex[prev]) && HasRiverThroughEdge(river, directionIndex[prev2]))
                        {
                            currCellCenter += HexMetrics.GetSecondSolidCorner(j) * 0.25f;
                        }
                        EdgeVertices m = new EdgeVertices(
                            Vector3.Lerp(currCellCenter, e.v1, 0.5f),
                            Vector3.Lerp(currCellCenter, e.v5, 0.5f)
                        );

                        TriangulateEdgeStrip(m, currCellColor, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                        TriangulateEdgeFan(currCellCenter, m, currCellColor, ref colorBuffer, ref vertexBuffer);
                    }
                }
                else
                {
                    TriangulateEdgeFan(currCellCenter, e, currCellColor, ref colorBuffer, ref vertexBuffer);
                }
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

                    bridge.y=(elevations[j]-elevation) * HexMetrics.elevationStep;
                    EdgeVertices e2 = new EdgeVertices(vertex1 + bridge, vertex2+ bridge);
                    if (hasRiverThroughEdge)
                    {
                        float neighborRiverSurfaceY = (elevations[j] + HexMetrics.riverSurfaceElevationOffset) * HexMetrics.elevationStep;
                        e2.v3.y= (elevations[j] + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
                        TriangulateRiverQuad(e.v2, e.v4, e2.v2, e2.v4, RiverSurfaceY, neighborRiverSurfaceY,0.8f,ref uvBuffer,ref riverBuffers, river.HasIncomingRiver && river.IncomingRiver == directionIndex[j]);
                    }
                    #region 桥面
                    //判断当前单元与相邻单元的海拔高低差，如果是斜坡，则添加阶梯，平面和峭壁则无需阶梯
                    if (HexMetrics.GetEdgeType(elevation, elevations[j]) == HexMetrics.HexEdgeType.Slope)
                    {
                        TriangulateEdgeTerraces(e,e2,currCellColor,blendColors[j],ref colorBuffer, ref vertexBuffer);
                    }
                    else
                    {
                        Color bridgeColor = (currCellColor + blendColors[j]) * 0.5f;
                        TriangulateEdgeStrip(e, currCellColor, e2, blendColors[j], ref colorBuffer, ref vertexBuffer);
                    }

                    #endregion

                    #region 桥洞

                    //添加外圈区域三向颜色混合

                    if (j <= 1 && directionIndex[next] != int.MinValue)
                    {
                        //下一个相邻单元的海拔
                        int nextElevation = elevations[next];
                        Vector3 vertex5 = vertex2 + HexMetrics.GetBridge(next);
                        vertex5.y = nextElevation * HexMetrics.elevationStep;
                        //判断相邻的三个六边形单元的高低关系，按照最低（Bottom），左（Left），右（Right）的顺序进行三角化处理
                        if (elevation <= elevations[j])
                        {
                            if (elevation <= nextElevation)
                            {
                                //当前单元海拔最低
                                TriangulateCorner(vertex2, currCellColor, e2.v5, blendColors[j], vertex5, blendColors[next],ref colorBuffer,ref vertexBuffer,elevation, elevations[j], nextElevation);
                            }
                            else
                            {
                                TriangulateCorner(vertex5, blendColors[next], vertex2, currCellColor, e2.v5, blendColors[j], ref colorBuffer, ref vertexBuffer, nextElevation, elevation, elevations[j]);
                            }
                        }
                        else if (elevations[j] <= nextElevation)
                        {
                            TriangulateCorner(e2.v5, blendColors[j], vertex5, blendColors[next], vertex2, currCellColor, ref colorBuffer, ref vertexBuffer, elevations[j], nextElevation, elevation);
                        }
                        else
                        {
                            TriangulateCorner(vertex5, blendColors[next], vertex2, currCellColor, e2.v5, blendColors[j], ref colorBuffer, ref vertexBuffer, nextElevation, elevation, elevations[j]);
                        }

                    }

                    #endregion

                }

                #endregion

            }
            //4.turn off cell system by remove NewDataTag
            CommandBuffer.RemoveComponent<NewDataTag>(index,entity);
        }

        //三角化桥面阶梯
        void TriangulateEdgeTerraces(EdgeVertices begin, EdgeVertices end, Color beginColor, Color endColor,ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
            /////////////(First Step)/////////////////
            Color bridgeColor = HexMetrics.TerraceLerp(beginColor, endColor, 1);
            TriangulateEdgeStrip(begin, beginColor, e2, bridgeColor,ref colorBuffer,ref vertexBuffer);
            ///////////////////(Middle Steps)///////////////////
            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                EdgeVertices e1 = e2;
                Color c1 = bridgeColor;
                e2 = EdgeVertices.TerraceLerp(begin, end, i);
                bridgeColor = HexMetrics.TerraceLerp(beginColor, endColor, i);
                TriangulateEdgeStrip(e1, c1, e2, bridgeColor, ref colorBuffer, ref vertexBuffer);
            }
            ///////////////(Last Step)///////////////////
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
                }else if (rightEdgeType == HexMetrics.HexEdgeType.Flat)
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
                    TriangulateCornerTerracesCliff( left, leftColor, right, rightColor, bottom, bottomColor, ref colorBuffer, ref vertexBuffer,leftElevation, rightElevation, bottomElevation);
                }
            }
            else
            {
                ////两个单元处于同一平面，填充桥三角补丁，添加桥三角的3个顶点
                AddTriangle(bottom,bottomColor,left,leftColor,right,rightColor,ref colorBuffer,ref vertexBuffer);
            }

        }

        //桥洞阶梯化
        void TriangulateCornerTerraces(Vector3 begin, Color beginColor, Vector3 left, Color leftColor, Vector3 right, Color rightColor, ref DynamicBuffer<ColorBuffer> colorBuffer,ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            ////////////First Triangle
            Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
            Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
            Color c3 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);
            Color c4 = HexMetrics.TerraceLerp(beginColor, rightColor, 1);
            AddTriangle(begin,beginColor,v3,c3,v4,c4,ref colorBuffer,ref vertexBuffer);
            ///////////Middle Steps
            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v3;
                Vector3 v2 = v4;
                Color c1 = c3;
                Color c2 = c4;
                v3 = HexMetrics.TerraceLerp(begin, left, i);
                v4 = HexMetrics.TerraceLerp(begin, right, i);
                c3 = HexMetrics.TerraceLerp(beginColor, leftColor, i);
                c4 = HexMetrics.TerraceLerp(beginColor, rightColor, i);
                AddQuad(v1,c1,v2,c2,v3,c3, v4, c4, ref colorBuffer, ref vertexBuffer);
            }
            /////////Last Step
            AddQuad(v3,c3,v4,c4,left,leftColor,right,rightColor, ref colorBuffer, ref vertexBuffer);
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

            TriangulateBoundaryTriangle(begin,beginCellColor,left,leftCellColor,boundary,boundaryColor,ref colorBuffer,ref vertexBuffer);
            if (HexMetrics.GetEdgeType(leftElevation,rightElevation) == HexMetrics.HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(left, leftCellColor, right, rightCellColor, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            }
            else
            {
                AddTriangle(left,leftCellColor,right,rightCellColor,boundary,boundaryColor,ref colorBuffer,ref vertexBuffer);
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
            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 v1 = v2;
                Color c1 = c2;
                v2 = HexMetrics.TerraceLerp(begin, left, i);
                c2 = HexMetrics.TerraceLerp(beginCellColor, leftCellColor, i);
                AddTriangle(v1, c1, v2, c2, boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
            }
            ///////////////////Last Triangle
            AddTriangle( v2, c2,left,leftCellColor ,boundary, boundaryColor, ref colorBuffer, ref vertexBuffer);
        }

        ///三角化扇形边缘
        void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            AddTriangle(center,color, edge.v1,color, edge.v2,color,ref colorBuffer,ref vertexBuffer);
            AddTriangle(center,color, edge.v2,color, edge.v3, color,ref colorBuffer, ref vertexBuffer);
            AddTriangle(center, color, edge.v3, color, edge.v4, color, ref colorBuffer, ref vertexBuffer);
            AddTriangle(center,color, edge.v4, color,edge.v5,color, ref colorBuffer, ref vertexBuffer);
        }

        //三角化带状边缘
        void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2,ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            AddQuad(e1.v1,c1, e1.v2,c2, e2.v1,c1, e2.v2,c2, ref colorBuffer, ref vertexBuffer);

            AddQuad(e1.v2,c1 ,e1.v3,c2, e2.v2,c1, e2.v3,c2, ref colorBuffer, ref vertexBuffer);
            AddQuad(e1.v3, c1, e1.v4, c2, e2.v3, c1, e2.v4, c2, ref colorBuffer, ref vertexBuffer);
            AddQuad(e1.v4,c1, e1.v5,c2, e2.v4,c1, e2.v5,c2, ref colorBuffer, ref vertexBuffer);

        }

        //三角化河流
        void TriangulateWithRiver(int direction,int prevD,int nextD, Color cellColor, Vector3 center, EdgeVertices e, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer,bool oppositeHasRiverThroughEdge)
        {
            Vector3 centerL, centerR;
            if (oppositeHasRiverThroughEdge)
            {
                centerL = center + HexMetrics.GetFirstSolidCorner(prevD) * 0.25f;
                centerR = center + HexMetrics.GetSecondSolidCorner(nextD) * 0.25f;
            }
            else
                centerL = centerR = center;

            
            EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f), Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);
            m.v3.y = center.y = e.v3.y;
            TriangulateEdgeStrip(m, cellColor, e, cellColor, ref colorBuffer, ref vertexBuffer);
            AddTriangle(centerL, cellColor, m.v1, cellColor, m.v2, cellColor, ref colorBuffer, ref vertexBuffer);
            AddQuad(centerL, cellColor, center, cellColor, m.v2, cellColor, m.v3, cellColor, ref colorBuffer, ref vertexBuffer);
            AddQuad(center, cellColor, centerR, cellColor, m.v3, cellColor, m.v4, cellColor, ref colorBuffer, ref vertexBuffer);
            AddTriangle(centerR, cellColor, m.v4, cellColor, m.v5, cellColor, ref colorBuffer, ref vertexBuffer);
        }

        //河流源头或尽头
        void TriangulateWithRiverBeginOrEnd(int direction, int prevD, int nextD, Color cellColor, Vector3 center, EdgeVertices e, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));
            m.v3.y = e.v3.y;
            TriangulateEdgeStrip(m, cellColor, e, cellColor, ref colorBuffer, ref vertexBuffer);
            TriangulateEdgeFan(center, m, cellColor, ref colorBuffer, ref vertexBuffer);
        }

        /// <summary>
        /// 是否有河流通过当前单元的方向
        /// </summary>
        /// <param name="river">河流</param>
        /// <param name="direction">方向</param>
        /// <returns></returns>
        bool HasRiverThroughEdge(River river,int direction )
        {
            return (river.HasIncomingRiver && river.IncomingRiver == direction) ||
                   (river.HasOutgoingRiver && river.OutgoingRiver == direction);
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

        //三角化河流
        void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1,float y2,float v,ref DynamicBuffer<UvBuffer> uvs, ref DynamicBuffer<RiverBuffer> riverBuffers, bool reversed)
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
                AddQuadUV(0f, 1f, v,v+0.2f, ref uvs);
            }
        }

        //为河流添加矩形三角顶点
        void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, ref DynamicBuffer<RiverBuffer> riverBuffers)
        {
            AddTriangle(v1, v3, v2, ref riverBuffers);
            AddTriangle(v2, v3, v4, ref riverBuffers);
        }

        //为河流添加三角
        void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3,ref DynamicBuffer<RiverBuffer> riverBuffers)
        {
            riverBuffers.Add((v1));
            riverBuffers.Add((v2));
            riverBuffers.Add((v3));
        }

        //添加三角区UV
        void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3,ref DynamicBuffer<UvBuffer> uvs)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
        }

        //添加矩形UV
        void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, ref DynamicBuffer<UvBuffer> uvs)
        {
            AddTriangleUV(uv1,uv3,uv2,ref uvs);
            AddTriangleUV(uv2, uv3, uv4, ref uvs);
        }

        //添加河流区域的UV
        private void AddQuadUV(float uMin, float uMax, float vMin, float vMax,ref DynamicBuffer<UvBuffer> uvs)
        {
            AddQuadUV(new Vector2(uMin, vMin), new Vector2(uMax, vMin), new Vector2(uMin, vMax), new Vector2(uMax, vMax),ref uvs);
        }

        //添加矩形三角顶点和颜色
        void AddQuad(Vector3 v1, Color c1, Vector3 v2, Color c2, Vector3 v3, Color c3, Vector3 v4, Color c4, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            //Color bridgeColor = (c2 + c3) * 0.5f;
            AddTriangle(v1, c1, v3, c3, v2, c2, ref colorBuffer, ref vertexBuffer);
            AddTriangle(v2, c2, v3, c3, v4, c4, ref colorBuffer, ref vertexBuffer);
        }

        //添加三角顶点与颜色
        void AddTriangle(Vector3 v1, Color bottomColor, Vector3 v2, Color leftColor, Vector3 v3, Color rightColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            //Color colorTriangle = (bottomColor + leftColor + rightColor) / 3f;
            colorBuffer.Add(bottomColor);
            vertexBuffer.Add((v1));

            colorBuffer.Add(leftColor);
            vertexBuffer.Add((v2));

            colorBuffer.Add(rightColor);
            vertexBuffer.Add((v3));
        }

        /// <summary>
        /// 边缘顶点
        /// </summary>
        private struct EdgeVertices {

            public Vector3 v1, v2, v3,v4, v5;

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
