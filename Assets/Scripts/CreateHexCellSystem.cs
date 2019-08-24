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
            //NativeArray<Entity> entities = new NativeArray<Entity>(createrData.Height* createrData.Width, Allocator.Temp);

            //代码生成预设，这样可以优化性能
            Entity hexCellPrefab = CommandBuffer.CreateEntity(index);
            CommandBuffer.AddComponent<HexCellData>(index, hexCellPrefab);
            CommandBuffer.AddComponent< Translation >(index, hexCellPrefab);
            //Todo：把相对位置关系合成一个不覆盖的组件
            //注解：单元的相对关系可以通过单元索引关系推断
            //CommandBuffer.AddComponent<NeighborNE>(index, hexCellPrefab);
            //CommandBuffer.AddComponent<NeighborE>(index, hexCellPrefab);
            //CommandBuffer.AddComponent<NeighborSE>(index, hexCellPrefab);
            //CommandBuffer.AddComponent<NeighborSW>(index, hexCellPrefab);
            //CommandBuffer.AddComponent<NeighborW>(index, hexCellPrefab);
            //CommandBuffer.AddComponent<NeighborNW>(index, hexCellPrefab);
            //三行代码，我们成功干掉一个预设
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
                        //6.设置单元关联
                        //Reference：https://catlikecoding.com/unity/tutorials/hex-map/part-2/
                        //参考：https://zhuanlan.zhihu.com/p/55068031
                        //单元之间的位置是相对的：W <-0-> E
                        //if (x > 0)
                        //{
                        //    //上一个单元就是本单元的西:W
                        //    Entity W = entities[i - 1];
                        //    CommandBuffer.SetComponent(index, instance, new NeighborW
                        //    {
                        //        W=W
                        //    });
                        //    //本单元就是上一个单元的东:E
                        //    CommandBuffer.SetComponent(index, W, new NeighborE
                        //    {
                        //        E = instance
                        //    });
                        //}

                        //if (z>0)
                        //{
                        //    if ((z & 1) == 0)//按位与运算判断==0偶数
                        //    {
                        //        // SE <-0-> NW 
                        //        Entity SE = entities[i - createrData.Width];
                        //        CommandBuffer.SetComponent(index, instance, new NeighborSE
                        //        {
                        //            SE = SE
                        //        });
                        //        CommandBuffer.SetComponent(index, SE, new NeighborNW
                        //        {
                        //            NW = instance
                        //        });

                        //        if (x>0)
                        //        {
                        //            // SW <-0-> NE 
                        //            Entity SW = entities[i - createrData.Width - 1];
                        //            CommandBuffer.SetComponent(index, instance, new NeighborSW
                        //            {
                        //                SW = SW
                        //            });
                        //            CommandBuffer.SetComponent(index, SW, new NeighborNE
                        //            {
                        //                NE = instance
                        //            });
                        //        }
                        //    }
                        //    else//奇数行
                        //    {
                        //        // SW <-0-> NE 
                        //        Entity SW = entities[i - createrData.Width];
                        //        CommandBuffer.SetComponent(index, instance, new NeighborSW
                        //        {
                        //            SW = SW
                        //        });
                        //        CommandBuffer.SetComponent(index, SW, new NeighborNE
                        //        {
                        //            NE = instance
                        //        });
                        //        if (x < createrData.Width - 1)
                        //        {
                        //            // SE <-0-> NW 
                        //            Entity SE = entities[i - createrData.Width+1];
                        //            CommandBuffer.SetComponent(index, instance, new NeighborSE
                        //            {
                        //                SE = SE
                        //            });
                        //            CommandBuffer.SetComponent(index, SE, new NeighborNW
                        //            {
                        //                NW = instance
                        //            });
                        //        }
                        //    }

                        //}
                        //i++;
                    }
                }
                CommandBuffer.SetComponent(index, entity, new SwitchCreateCellData
                {
                    bIfNewMap=false

                });

                //摧毁使用完的预设，节约内存资源
                CommandBuffer.DestroyEntity(index, hexCellPrefab);
                //entities.Dispose();
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
