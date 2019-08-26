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
public class CellSpawnSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    private CellSystem cellSystem;
    /// <summary>
    /// 单元系统开关
    /// </summary>
    bool bIfSystemCreated = false;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<Data> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index, [ReadOnly]ref Data createrData)
        {

            if (createrData.BIfNewMap)
            {            
                //0.代码生成预设，这样可以优化性能
                Entity hexCellPrefab = CommandBuffer.CreateEntity(index);
                CommandBuffer.AddComponent<Cell>(index, hexCellPrefab);
                DynamicBuffer<ColorBuff> buff= CommandBuffer.AddBuffer<ColorBuff>(index, entity);
                //buff.Clear();
                //There is no need for Translation for now
                //CommandBuffer.AddComponent<Translation>(index, hexCellPrefab);
                Random random = new Random(1208905299U);

                for (int z = 0; z < createrData.Height; z++)
                {
                    for (int x = 0; x < createrData.Width; x++)
                    {

                        //1.实例化
                        var instance = CommandBuffer.Instantiate(index, hexCellPrefab);

                        //2.计算阵列坐标
                        float _x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
                        float _z = z * (HexMetrics.OuterRadius * 1.5f);

                        //3.设置父组件 
                        //CommandBuffer.SetComponent(index, instance, new Parent
                        //{
                        //    Value = entity
                        //注释：似乎没有必要设置父类
                        //});

                        Color color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
                        buff.Add(color);
                        //4.设置每个单元的数据
                        CommandBuffer.SetComponent(index, instance, new Cell
                        {
                            Color = color,
                            Position= new Vector3(_x, 0F, _z),
                            Switcher=true,
                        });

                        //5.设置位置,目前来看，没有必要使用Translation
                        //CommandBuffer.SetComponent(index, instance, new Translation
                        //{
                        //    Value = new float3(_x, 0F, _z)

                        //});

                    }
                }
                //6.重置数据
                CommandBuffer.SetComponent(index, entity, new Data
                {
                    Width=createrData.Width,
                    Height=createrData.Height,
                    BIfNewMap = false

                });

                //7.摧毁使用完的预设，节约内存资源
                CommandBuffer.DestroyEntity(index, hexCellPrefab);
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
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        job.Complete();
        if (job.IsCompleted)
        {
            if (bIfSystemCreated)
            {
                cellSystem.Update();
            }
            else
            {

                cellSystem = MainWorld.Instance.GetWorld().GetOrCreateSystem<CellSystem>();
                bIfSystemCreated = true;

            }

        }
        return job;

    }
}
