using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 六边形单元系统
/// </summary>
[DisableAutoCreation]
public class CellSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    /// <summary>
    /// 开关
    /// </summary>
    bool bSwitcher = true;

    private HexMapSystem hexMapSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<Cell> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        //public DynamicBuffer<ColorBuffer> Colors;
        //public DynamicBuffer<VertexBuffer> Vertices;
        //public DynamicBuffer<TriangleBuffer> Triangles;
        [ReadOnly] public int CellCount;
        [ReadOnly] public int Width;
        [ReadOnly] public DynamicBuffer<ColorBuff> buff;
        [BurstCompile]
        public void Execute(Entity entity, int index, ref Cell cellData)
        {

            if (cellData.Switcher)
            {

                //0.代码生成预设，这样可以优化性能
                Entity vertexPrefab = CommandBuffer.CreateEntity(index);
                CommandBuffer.AddComponent<Vertex>(index, vertexPrefab);
                //暂时不需要Translation
                //CommandBuffer.AddComponent<Translation>(index, hexCellPrefab);
                //1.获取当前单元的位置和颜色数据
                Vector3 center = cellData.Position;
                Color color = cellData.Color;
                //邻居的颜色
                Color neighbor = color;
                //保存需要混合的颜色
                Color[] blendColors = new Color[6];
                //2.计算当前单元所在行数
                int currHeight = index / Width;
                //3.判断当前所在行数是否为偶数
                bool ifEven = (currHeight & 1) == 0;
                //是否处于行尾
                bool ifEnd = (index + 1) == (currHeight + 1) * Width;
                //是否处于行首
                bool ifStart = index == currHeight * Width;
                //高等于总数除以宽
                int height = CellCount / Width;
                //是否最后一行
                bool isLastRow = (currHeight == (height - 1));

                int vertexIndex = index;
                //0=东北：NE
                if (!isLastRow)
                {
                    if (ifEven)//偶数行
                    {
                        neighbor = buff[index + Width].Value;
                        
                    }
                    else
                    {
                        if (ifEnd)//最末尾没有相邻的单元
                        {
                            neighbor = color;
                        }
                        else
                        {
                            neighbor = (buff[index + Width + 1].Value);
                        }
                    }
                }

                blendColors[0] = neighbor;
                //颜色混合1 东：E
                if (ifEnd)
                {
                    //如果在地图行尾，没有东邻居
                    neighbor = color;
                }
                else
                {
                    neighbor = (buff[index + 1].Value);
                }

                blendColors[1] = neighbor;
                //东南2：SE
                if (index < Width)
                {
                    vertexIndex = index * 33;
                    neighbor = color;
                }
                else
                {
                    if (isLastRow)
                    {
                        vertexIndex = index * 42 - ( index- (currHeight - 1)*Width - 1) * 9 - (currHeight / 2) * 32;
                    }
                    else
                    {
                        vertexIndex = index * 42 - (Width - 1) * 9 - (currHeight / 2) * 32;
                    }
                    if (ifEven)
                    {
                        neighbor = (buff[index - Width].Value);
                    }
                    else
                    {
                        if (ifEnd)
                        {
                            neighbor = color;
                        }
                        else
                        {

                            neighbor = (buff[index - Width + 1].Value);
                        }
                    }
                }
                blendColors[2] = neighbor;
                //西南3：SW
                if (index < Width) neighbor = color;
                else
                {
                    if (ifEven)
                    {
                        if (ifStart) neighbor = color;
                        else
                            neighbor = (buff[index - Width - 1].Value);
                    }
                    else
                        neighbor = (buff[index - Width].Value);
                }
                blendColors[3] = neighbor;
                //西4：W
                if (ifStart)
                {
                    //如果在地图起始位置，没有西邻居
                    neighbor = color;
                }
                else
                {
                    neighbor = (buff[index - 1].Value);
                }
                blendColors[4] = neighbor;
                //5西北：NW
                if (isLastRow)
                {
                    neighbor = color;
                }
                else
                {
                    if (ifEven)
                    {
                        if (ifStart)
                        {
                            neighbor = color;
                        }
                        else
                        {
                            neighbor = (buff[index + Width - 1].Value);
                        }
                    }
                    else
                    {
                        neighbor = (buff[index + Width].Value);
                    }
                }
                blendColors[5] = neighbor;

                //添加顶点、三角和颜色
                for (int j = 0; j < 6; j++)
                {
                    //1.添加中心区域的3个顶点
                    var vertex = CommandBuffer.Instantiate(index, vertexPrefab);
                    Vector3 V1 = (center + HexMetrics.SolidCorners[j]);
                    Vector3 V2 = (center + HexMetrics.SolidCorners[j + 1]);
                    CommandBuffer.SetComponent<Vertex>(index, vertex, new Vertex
                    {
                        Vector=center,
                        Color=color,
                        Triangle=vertexIndex,
                        Switcher=true
                    });
                    vertexIndex++;
                    var vertex1 = CommandBuffer.Instantiate(index, vertexPrefab);
                    CommandBuffer.SetComponent<Vertex>(index, vertex1, new Vertex
                    {
                        Vector = V1,
                        Color = color,
                        Triangle = vertexIndex,
                        Switcher = true
                    });
                    vertexIndex++;
                    var vertex2 = CommandBuffer.Instantiate(index, vertexPrefab);
                    CommandBuffer.SetComponent<Vertex>(index, vertex2, new Vertex
                    {
                        Vector = V2,
                        Color = color,
                        Triangle = vertexIndex,
                        Switcher = true
                    });
                    vertexIndex++;
                    if (j <= 2)
                    {
                        if (blendColors[j] == color)
                        {//如果没有相邻的单元，则跳过循环
                            continue;
                        }
                        Color bridgeColor = ((color + blendColors[j]) * 0.5F);
                        //添加外围桥接区域的4个顶点
                        Vector3 bridge = (HexMetrics.GetBridge(j));
                        Vector3 V3 = (V1 + bridge);
                        Vector3 V4 = (V2 + bridge);
                        var vertex3 = CommandBuffer.Instantiate(index, vertexPrefab);
                        CommandBuffer.SetComponent<Vertex>(index, vertex3, new Vertex
                        {
                            Vector = V1,
                            Color = color,
                            Triangle = vertexIndex,
                            Switcher = true
                        });
                        vertexIndex++;
                        var vertex4 = CommandBuffer.Instantiate(index, vertexPrefab);
                        CommandBuffer.SetComponent<Vertex>(index, vertex4, new Vertex
                        {
                            Vector = V3,
                            Color = color,
                            Triangle = vertexIndex,
                            Switcher = true
                        });
                        vertexIndex++;
                        var vertex5 = CommandBuffer.Instantiate(index, vertexPrefab);
                        CommandBuffer.SetComponent<Vertex>(index, vertex5, new Vertex
                        {
                            Vector = V2,
                            Color = bridgeColor,
                            Triangle = vertexIndex,
                            Switcher = true
                        });
                        vertexIndex++;
                        var vertex6 = CommandBuffer.Instantiate(index, vertexPrefab);
                        CommandBuffer.SetComponent<Vertex>(index, vertex6, new Vertex
                        {
                            Vector = V2,
                            Color = neighbor,
                            Triangle = vertexIndex,
                            Switcher = true
                        });
                        vertexIndex++;
                        var vertex7 = CommandBuffer.Instantiate(index, vertexPrefab);
                        CommandBuffer.SetComponent<Vertex>(index, vertex7, new Vertex
                        {
                            Vector = V3,
                            Color = neighbor,
                            Triangle = vertexIndex,
                            Switcher = true
                        });
                        vertexIndex++;
                        var vertex8 = CommandBuffer.Instantiate(index, vertexPrefab);
                        CommandBuffer.SetComponent<Vertex>(index, vertex8, new Vertex
                        {
                            Vector = V4,
                            Color = bridgeColor,
                            Triangle = vertexIndex,
                            Switcher = true
                        });
                        vertexIndex++;
                        //添加外圈区域三向颜色混合
                        int next = (j + 1) > 5 ? 0 : (j + 1);
                        if (j <= 1 && blendColors[next] != color)
                        {
                            //填充桥三角
                            var vertex9 = CommandBuffer.Instantiate(index, vertexPrefab);
                            CommandBuffer.SetComponent<Vertex>(index, vertex9, new Vertex
                            {
                                Vector = V2,
                                Color = color,
                                Triangle = vertexIndex,
                                Switcher = true
                            });
                            vertexIndex++;
                            //添加桥三角的3个顶点
                            var vertex10 = CommandBuffer.Instantiate(index, vertexPrefab);
                            CommandBuffer.SetComponent<Vertex>(index, vertex10, new Vertex
                            {
                                Vector = V4,
                                Color = (color + blendColors[next] + blendColors[j]) / 3F,
                                Triangle = vertexIndex,
                                Switcher = true
                            });
                            vertexIndex++;
                            var vertex11 = CommandBuffer.Instantiate(index, vertexPrefab);
                            CommandBuffer.SetComponent<Vertex>(index, vertex11, new Vertex
                            {
                                Vector = (V2 + HexMetrics.GetBridge(next)),
                                Color = bridgeColor,
                                Triangle = vertexIndex,
                                Switcher = true
                            });
                            vertexIndex++;
                        }
                    }

                }
                //4.turn off cell system or just destory the cell,which is better I do not know for now
                CommandBuffer.SetComponent(index, entity, new Cell
                {
                    Switcher = false
                });
                //CommandBuffer.DestroyEntity(index, entity);
                //摧毁使用完的预设，节约内存资源
                //CommandBuffer.DestroyEntity(index, vertexPrefab);
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
        var job = new SpawnJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            CellCount= HexMetrics.HexCelllCount,
            Width = HexMetrics.MapWidth,
            buff =MainWorld.Instance.GetColorBuff()
        }.Schedule(this, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        job.Complete();
        if (job.IsCompleted)
        {

            if (bSwitcher)
            {
                hexMapSystem = MainWorld.Instance.GetWorld().GetOrCreateSystem<HexMapSystem>();
                bSwitcher = false;
            }
            else
            {
                hexMapSystem.Update();
            }
        }
        return job;

    }


}
