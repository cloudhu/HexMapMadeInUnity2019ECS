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
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct CalculateJob : IJobForEachWithEntity<Cell,NewDataTag> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index,[ReadOnly] ref Cell cellData,[ReadOnly]ref NewDataTag tag)
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
            blendColors[0] = cellData.NE;
            blendColors[1] = cellData.E;
            blendColors[2] = cellData.SE;
            blendColors[3] = cellData.SW;
            blendColors[4] = cellData.W;
            blendColors[5] = cellData.NW;
            //前3个方向相邻单元的索引
            int[] directions = new int[3];
            directions[0] = cellData.NEIndex;
            directions[1] = cellData.EIndex;
            directions[2] = cellData.SEIndex;
            //前三个方向相邻单元的海拔
            int[] elevations = new int[3];
            elevations[0] = cellData.NEElevation;
            elevations[1] = cellData.EElevation;
            elevations[2] = cellData.SEElevation;
            //添加六边形单元六个方向的顶点、三角和颜色
            for (int j = 0; j < 6; j++)
            {
                //1.添加中心区域的3个顶点
                Vector3 vertex1 = (currCellCenter + HexMetrics.SolidCorners[j]);
                Vector3 vertex2 = (currCellCenter + HexMetrics.SolidCorners[j + 1]);
                AddTriangle(currCellCenter, currCellColor, vertex1, currCellColor, vertex2, currCellColor, ref colorBuffer, ref vertexBuffer);

                //Connection Between 2 cells
                #region  Bridge=桥
                //桥只连接前三个方向相邻的单元，从而避免重复连接
                if (j <= 2)
                {
                    if (directions[j] == 0)
                    {//如果没有相邻的单元，则跳过循环
                        continue;
                    }
                    Color neighborColor = blendColors[j];
                    //添加外围桥接区域的顶点
                    Vector3 bridge = (HexMetrics.GetBridge(j));
                    Vector3 vertex3 = (vertex1 + bridge);
                    Vector3 vertex4 = (vertex2 + bridge);

                    //当前单元的海拔
                    int elevation = cellData.Elevation;
                    //邻居单元的海拔
                    int neighborElevation = elevations[j];
                    //顶点3和顶点4在相邻单元上，海拔应与其同高
                    vertex3.y = vertex4.y = neighborElevation * HexMetrics.elevationStep;

                    #region 桥面
                    //判断当前单元与相邻单元的海拔高低差，如果是斜坡，则添加阶梯，平面和峭壁则无需阶梯
                    if (HexMetrics.GetEdgeType(elevation, neighborElevation) == HexMetrics.HexEdgeType.Slope)
                    {
                        TriangulateEdgeTerraces(vertex1,vertex2,currCellColor,vertex3,vertex4,neighborColor,ref colorBuffer, ref vertexBuffer);
                    }
                    else
                    {
                        Color bridgeColor = (currCellColor + neighborColor) * 0.5f;
                        AddQuad(vertex1, currCellColor,vertex2, bridgeColor, vertex3, bridgeColor, vertex4,neighborColor,ref colorBuffer,ref vertexBuffer);
                    }

                    #endregion

                    #region 桥洞

                    //添加外圈区域三向颜色混合
                    int next = (j + 1);
                    if (j <= 1 && directions[next] != 0)
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
                                TriangulateCorner(vertex2, currCellColor, vertex4, neighborColor, vertex5, blendColors[next],ref colorBuffer,ref vertexBuffer,elevation, neighborElevation, nextElevation);
                            }
                            else
                            {
                                TriangulateCorner(vertex5, blendColors[next], vertex2, currCellColor, vertex4, neighborColor, ref colorBuffer, ref vertexBuffer, nextElevation, elevation, neighborElevation);
                            }
                        }
                        else if (neighborElevation <= nextElevation)
                        {
                            TriangulateCorner(vertex4, neighborColor, vertex5, blendColors[next], vertex2, currCellColor, ref colorBuffer, ref vertexBuffer, neighborElevation, nextElevation, elevation);
                        }
                        else
                        {
                            TriangulateCorner(vertex5, blendColors[next], vertex2, currCellColor, vertex4, neighborColor, ref colorBuffer, ref vertexBuffer, nextElevation, elevation, neighborElevation);
                        }

                    }

                    #endregion

                }

                #endregion

            }
            //4.turn off cell system by remove NewDataTag
            CommandBuffer.RemoveComponent<NewDataTag>(index,entity);
        }

        //三角化桥面
        void TriangulateEdgeTerraces(Vector3 beginLeft, Vector3 beginRight, Color beginColor, Vector3 endLeft, Vector3 endRight, Color endColor,ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            Vector3 vertex3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
            Vector3 vertex4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
            /////////////(First Step)/////////////////
            Color bridgeColor = HexMetrics.TerraceLerp(beginColor, endColor, 1);
            AddQuad(beginLeft, beginColor, beginRight, beginColor,vertex3, bridgeColor, vertex4, bridgeColor, ref colorBuffer, ref vertexBuffer);
            ///////////////////(Middle Steps)///////////////////
            for (int i = 2; i < HexMetrics.terraceSteps; i++)
            {
                Vector3 stepVertex1 = vertex3;
                Vector3 stepVertex2 = vertex4;
                Color c1 = bridgeColor;
                vertex3 = HexMetrics.TerraceLerp(beginLeft, vertex3, i);
                vertex4 = HexMetrics.TerraceLerp(beginRight, vertex4, i);
                bridgeColor = HexMetrics.TerraceLerp(beginColor, endColor, i);
                AddQuad(stepVertex1, c1,stepVertex2, c1,vertex3, beginColor, vertex4, bridgeColor, ref colorBuffer, ref vertexBuffer);
            }
            ///////////////(Last Step)///////////////////
            AddQuad(vertex3, beginColor, vertex4, beginColor, endLeft, endColor, endRight, endColor, ref colorBuffer, ref vertexBuffer);
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
            colorBuffer.Add(c1);
            vertexBuffer.Add(v1);

            colorBuffer.Add(c3);
            vertexBuffer.Add(v3);

            colorBuffer.Add(c2);
            vertexBuffer.Add(v2);

            colorBuffer.Add(c2);
            vertexBuffer.Add(v2);

            colorBuffer.Add(c3);
            vertexBuffer.Add(v3);

            colorBuffer.Add(c4);
            vertexBuffer.Add(v4);
        }

        //添加三角顶点与颜色
        void AddTriangle(Vector3 v1, Color bottomColor, Vector3 v2, Color leftColor, Vector3 v3,Color rightColor, ref DynamicBuffer<ColorBuffer> colorBuffer, ref DynamicBuffer<VertexBuffer> vertexBuffer)
        {
            colorBuffer.Add(bottomColor);
            vertexBuffer.Add(v1);

            colorBuffer.Add(leftColor);
            vertexBuffer.Add(v2);

            colorBuffer.Add((bottomColor + leftColor + rightColor) / 3f);
            vertexBuffer.Add(v3);
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
