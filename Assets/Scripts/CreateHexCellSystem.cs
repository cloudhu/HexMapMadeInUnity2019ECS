using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 创建六边形单元系统
/// </summary>
//[DisableAutoCreation]
public class CreateHexCellSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    /// <summary>
    /// 新地图开关
    /// </summary>
    bool bIfNewMap = true;
    private CreateHexMapSystem createHexMapSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<CreaterData,SwitchCreateCellData> {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        [BurstCompile]
        public void Execute(Entity entity, int index, [ReadOnly]ref CreaterData  createrData,ref SwitchCreateCellData switchCreateCell)
        {
            //代码生成预设，这样可以优化性能
            Entity hexCellPrefab = CommandBuffer.CreateEntity(index);
            CommandBuffer.AddComponent<HexCellData>(index, hexCellPrefab);
            CommandBuffer.AddComponent< Translation >(index, hexCellPrefab);

            if (switchCreateCell.bIfNewMap)
            {
                Random random= new Random(1208905299U);

                for (int z = 0; z < createrData.Height; z++)
                {
                    for (int x = 0; x < createrData.Width; x++)
                    {
                        
                        //1.实例化
                        var instance = CommandBuffer.Instantiate(index, hexCellPrefab);
                        //entities[i] = instance;
                        
                        //2.计算阵列坐标
                        float _x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
                        float _z = z * (HexMetrics.OuterRadius * 1.5f);

                        //3.设置父组件 
                        //CommandBuffer.SetComponent(index, instance, new Parent
                        //{
                        //    Value = entity
                        //注释：似乎没有必要设置父类
                        //});

                        //4.设置每个单元的数据
                        CommandBuffer.SetComponent(index, instance, new HexCellData
                        {
                            X = x - z / 2,
                            Y = 0,
                            Z = z,
                            color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat()),

                        });

                        //5.设置位置
                        CommandBuffer.SetComponent(index, instance, new Translation
                        {
                            Value = new float3(_x, 0F, _z)

                        });
                        
                    }
                }
                CommandBuffer.SetComponent(index, entity, new SwitchCreateCellData
                {
                    bIfNewMap=false

                });

                //摧毁使用完的预设，节约内存资源
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

        if (bIfNewMap)
        {
            var job = new SpawnJob
            {
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()

            }.Schedule(this, inputDeps);

            m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
            job.Complete();
            createHexMapSystem = World.GetOrCreateSystem<CreateHexMapSystem>();
            createHexMapSystem.bIfNewMap = true;

            bIfNewMap = false;

            return job;
        }
        else
        {
            if (createHexMapSystem.bIfNewMap)
            {
                createHexMapSystem.Update();
            }
        }

        return inputDeps;

    }
}
