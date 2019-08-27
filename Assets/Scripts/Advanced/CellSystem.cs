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
            colorBuffer.Clear();
            vertexBuffer.Clear();
            //1.获取当前单元的位置和颜色数据
            Vector3 center = cellData.Position;
            Color color = cellData.Color;
            ////保存需要混合的颜色
            Color[] blendColors = new Color[6];
            blendColors[0] = cellData.NE;
            blendColors[1] = cellData.E;
            blendColors[2] = cellData.SE;
            blendColors[3] = cellData.SW;
            blendColors[4] = cellData.W;
            blendColors[5] = cellData.NW;
            int[] directions = new int[3];
            directions[0] = cellData.NEIndex;
            directions[1] = cellData.EIndex;
            directions[2] = cellData.SEIndex;

            //添加六边形单元六个方向的顶点、三角和颜色
            for (int j = 0; j < 6; j++)
            {
                //1.添加中心区域的3个顶点
                Color neighbor = blendColors[j];
                Vector3 V1 = (center + HexMetrics.SolidCorners[j]);
                Vector3 V2 = (center + HexMetrics.SolidCorners[j + 1]);

                colorBuffer.Add(color);
                vertexBuffer.Add(center);

                colorBuffer.Add(color);
                vertexBuffer.Add(V1);

                colorBuffer.Add(color);
                vertexBuffer.Add(V2);
                if (j <= 2)
                {
                    if (directions[j] == 0)
                    {//如果没有相邻的单元，则跳过循环
                        continue;
                    }
                    Color bridgeColor = ((color + neighbor) * 0.5F);
                    //添加外围桥接区域的4个顶点
                    Vector3 bridge = (HexMetrics.GetBridge(j));
                    Vector3 V3 = (V1 + bridge);
                    Vector3 V4 = (V2 + bridge);

                    colorBuffer.Add(bridgeColor);
                    vertexBuffer.Add(V1);

                    colorBuffer.Add(bridgeColor);
                    vertexBuffer.Add(V3);

                    colorBuffer.Add(bridgeColor);
                    vertexBuffer.Add(V2);

                    colorBuffer.Add(bridgeColor);
                    vertexBuffer.Add(V3);

                    colorBuffer.Add(bridgeColor);
                    vertexBuffer.Add(V4);

                    colorBuffer.Add(bridgeColor);
                    vertexBuffer.Add(V2);
                    //添加外圈区域三向颜色混合
                    int next = (j + 1);
                    if (j <= 1 && directions[next] != 0)
                    {
                        //填充桥三角
                        Color triangleColor = (color + blendColors[next] + neighbor) / 3F;
                        colorBuffer.Add(triangleColor);
                        vertexBuffer.Add(V2);
                        //添加桥三角的3个顶点

                        colorBuffer.Add(triangleColor);
                        vertexBuffer.Add(V4);
                        colorBuffer.Add(triangleColor);
                        vertexBuffer.Add(V2 + HexMetrics.GetBridge(next));
                    }
                }

            }
            //4.turn off cell system or just destory the cell,which is better I do not know for now
            //CommandBuffer.DestroyEntity(index, entity);
            CommandBuffer.RemoveComponent<NewDataTag>(index,entity);
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
