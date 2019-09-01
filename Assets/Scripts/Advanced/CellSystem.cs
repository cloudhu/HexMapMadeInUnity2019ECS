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

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    /// <summary>
    /// 计算六边形单元的顶点和颜色
    /// </summary>
    //[BurstCompile]//Unity2019.1.14f1会报错，Unity2019.1.12f1则不会
    struct CalculateJob : IJobForEachWithEntity<Cell,NewDataTag,Neighbors,NeighborsIndex> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index,[ReadOnly] ref Cell cellData,[ReadOnly]ref NewDataTag tag, [ReadOnly]ref Neighbors neighbors, [ReadOnly]ref NeighborsIndex neighborsIndex)
        {
            //0.获取单元索引，Execute的index不可靠，添加动态缓存
            int cellIndex = cellData.Index;
            DynamicBuffer<ColorBuffer> colorBuffer = CommandBuffer.AddBuffer<ColorBuffer>(index, entity);
            DynamicBuffer<VertexBuffer> vertexBuffer = CommandBuffer.AddBuffer<VertexBuffer>(index, entity);
            //1.获取当前单元的中心位置
            Vector3 currCellCenter = cellData.Position;
            //缓存当前单元的颜色
            Color currCellColor = cellData.Color;
            ////保存需要混合的颜色，使用数组[]是为了方便循环
            Color[] blendColors = new Color[6];
            blendColors[0] = neighbors.NE;
            blendColors[1] = neighbors.E;
            blendColors[2] = neighbors.SE;
            blendColors[3] = neighbors.SW;
            blendColors[4] = neighbors.W;
            blendColors[5] = neighbors.NW;
            //前3个方向相邻单元的索引
            int[] directions = new int[3];
            directions[0] = neighborsIndex.NEIndex;
            directions[1] = neighborsIndex.EIndex;
            directions[2] = neighborsIndex.SEIndex;
            //前三个方向相邻单元的海拔
            int[] elevations = new int[3];
            elevations[0] = neighbors.NEElevation;
            elevations[1] = neighbors.EElevation;
            elevations[2] = neighbors.SEElevation;
            //添加六边形单元六个方向的顶点、三角和颜色
            for (int j = 0; j < 6; j++)
            {
                //1.添加中心区域的3个顶点
                Vector3 vertex1 = (currCellCenter + HexMetrics.SolidCorners[j]);
                Vector3 vertex2 = (currCellCenter + HexMetrics.SolidCorners[j + 1]);
                //vertex1.y = vertex2.y = cellData.Elevation * HexMetrics.elevationStep;
                EdgeVertices e = new EdgeVertices(vertex1, vertex2);
                TriangulateEdgeFan(currCellCenter, e, currCellColor, ref colorBuffer, ref vertexBuffer);

                //Connection Between 2 cells
                #region  Bridge=桥
                //桥只连接前三个方向相邻的单元，从而避免重复连接
                if (j <= 2)
                {
                    if (directions[j] == int.MinValue)
                    {//如果没有相邻的单元，则跳过循环
                        continue;
                    }
                    //相邻单元的颜色
                    Color neighborColor = blendColors[j];
                    //当前单元的海拔
                    int elevation = cellData.Elevation;
                    //邻居单元的海拔
                    int neighborElevation = elevations[j];
                    //添加外围桥接区域的顶点
                    Vector3 bridge = (HexMetrics.GetBridge(j));

                    bridge.y=(neighborElevation-elevation) * HexMetrics.elevationStep;
                    EdgeVertices e2 = new EdgeVertices(vertex1 + bridge, vertex2+ bridge);

                    #region 桥面
                    //判断当前单元与相邻单元的海拔高低差，如果是斜坡，则添加阶梯，平面和峭壁则无需阶梯
                    if (HexMetrics.GetEdgeType(elevation, neighborElevation) == HexMetrics.HexEdgeType.Slope)
                    {
                        TriangulateEdgeTerraces(e,e2,currCellColor,neighborColor,ref colorBuffer, ref vertexBuffer);
                    }
                    else
                    {
                        Color bridgeColor = (currCellColor + neighborColor) * 0.5f;
                        TriangulateEdgeStrip(e, currCellColor, e2, neighborColor, ref colorBuffer, ref vertexBuffer);
                    }

                    #endregion

                    #region 桥洞

                    //添加外圈区域三向颜色混合
                    int next = (j + 1);
                    if (j <= 1 && directions[next] != int.MinValue)
                    {
                        //下一个相邻单元的海拔
                        int nextElevation = elevations[next];
                        Vector3 vertex5 = vertex2 + HexMetrics.GetBridge(next);
                        vertex5.y = nextElevation * HexMetrics.elevationStep;
                        //判断相邻的三个六边形单元的高低关系，按照最低（Bottom），左（Left），右（Right）的顺序进行三角化处理
                        if (elevation <= neighborElevation)
                        {
                            if (elevation <= nextElevation)
                            {
                                //当前单元海拔最低
                                TriangulateCorner(vertex2, currCellColor, e2.v4, neighborColor, vertex5, blendColors[next],ref colorBuffer,ref vertexBuffer,elevation, neighborElevation, nextElevation);
                            }
                            else
                            {
                                TriangulateCorner(vertex5, blendColors[next], vertex2, currCellColor, e2.v4, neighborColor, ref colorBuffer, ref vertexBuffer, nextElevation, elevation, neighborElevation);
                            }
                        }
                        else if (neighborElevation <= nextElevation)
                        {
                            TriangulateCorner(e2.v4, neighborColor, vertex5, blendColors[next], vertex2, currCellColor, ref colorBuffer, ref vertexBuffer, neighborElevation, nextElevation, elevation);
                        }
                        else
                        {
                            TriangulateCorner(vertex5, blendColors[next], vertex2, currCellColor, e2.v4, neighborColor, ref colorBuffer, ref vertexBuffer, nextElevation, elevation, neighborElevation);
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


        //添加矩形三角顶点和颜色
        void AddQuad(Vector3 v1,Color c1, Vector3 v2,Color c2, Vector3 v3, Color c3, Vector3 v4, Color c4, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            Color bridgeColor = (c2 + c3) * 0.5f;
            AddTriangle(v1, c1, v3, bridgeColor, v2, bridgeColor, ref colorBuffer, ref vertexBuffer);
            AddTriangle(v2, bridgeColor, v3, bridgeColor, v4, c4, ref colorBuffer, ref vertexBuffer);
        }

        //添加三角顶点与颜色
        void AddTriangle(Vector3 v1, Color bottomColor, Vector3 v2, Color leftColor, Vector3 v3,Color rightColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            Color colorTriangle = (bottomColor + leftColor + rightColor) / 3f;
            colorBuffer.Add(colorTriangle);
            vertexBuffer.Add((v1));

            colorBuffer.Add(colorTriangle);
            vertexBuffer.Add((v2));

            colorBuffer.Add(colorTriangle);
            vertexBuffer.Add((v3));
        }

        ///三角化扇形边缘
        void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            AddTriangle(center,color, edge.v1,color, edge.v2,color,ref colorBuffer,ref vertexBuffer);
            AddTriangle(center,color, edge.v2,color, edge.v3, color,ref colorBuffer, ref vertexBuffer);

            AddTriangle(center,color, edge.v3, color,edge.v4,color, ref colorBuffer, ref vertexBuffer);
        }

        //三角化带状边缘
        void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2,ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            AddQuad(e1.v1,c1, e1.v2,c2, e2.v1,c1, e2.v2,c2, ref colorBuffer, ref vertexBuffer);

            AddQuad(e1.v2,c1 ,e1.v3,c2, e2.v2,c1, e2.v3,c2, ref colorBuffer, ref vertexBuffer);

            AddQuad(e1.v3,c1, e1.v4,c2, e2.v3,c1, e2.v4,c2, ref colorBuffer, ref vertexBuffer);

        }

        /// <summary>
        /// 边缘顶点
        /// </summary>
        public struct EdgeVertices {

            public Vector3 v1, v2, v3, v4;

            public EdgeVertices(Vector3 corner1, Vector3 corner2)
            {
                v1 = corner1;
                v2 = Vector3.Lerp(corner1, corner2, 1f / 3f);
                v3 = Vector3.Lerp(corner1, corner2, 2f / 3f);
                v4 = corner2;
            }

            public static EdgeVertices TerraceLerp(
                EdgeVertices a, EdgeVertices b, int step)
            {
                EdgeVertices result;
                result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
                result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
                result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
                result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
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

        }.Schedule(this, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        job.Complete();

        if (job.IsCompleted)
        {
            MainWorld.Instance.RenderMesh();
        }

        return job;

    }
}
