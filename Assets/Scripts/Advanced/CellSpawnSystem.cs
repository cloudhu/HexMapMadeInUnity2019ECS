using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 六边形单元系统
/// </summary>
[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class CellSpawnSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<Data,OnCreateTag> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        
        [BurstCompile]
        public void Execute(Entity entity, int index, [ReadOnly]ref Data createrData,[ReadOnly]ref OnCreateTag tag)
        {
            //0.代码生成预设，这样可以优化性能
            Entity hexCellPrefab = CommandBuffer.CreateEntity(index);
            CommandBuffer.AddComponent<Cell>(index, hexCellPrefab);
            //DynamicBuffer is kind of sucks here,add it is good,after finished this job get it otherplace is always report error:not added it,indeed I added it,what's wrong?
            //DynamicBuffer<ColorBuff> buff= CommandBuffer.AddBuffer<ColorBuff>(index, entity);
            //There is no need for Translation for now
            //CommandBuffer.AddComponent<Translation>(Index, hexCellPrefab);
            //1.添加颜色数组，这个数组以后从服务器获取，然后传到这里来处理
            Random random = new Random(1208905299U);
            int Width = createrData.Width;
            int Height = createrData.Height;
            NativeArray<Color> Colors=new NativeArray<Color>(Height * Width, Allocator.Temp);
            for (int i = 0; i < Height* Width; i++)
            {
                Colors[i]= new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
            }

            for (int z = 0,i=0; z < Height; z++)
            {
                for (int x = 0; x < Width; x++)
                {

                    //2.实例化
                    var instance = CommandBuffer.Instantiate(index, hexCellPrefab);

                    //3.计算阵列坐标
                    float _x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
                    float _z = z * (HexMetrics.OuterRadius * 1.5f);

                    //3.设置父组件 
                    //CommandBuffer.SetComponent(Index, instance, new Parent
                    //{
                    //    Value = entity
                    //注释：似乎没有必要设置父类
                    //});
                    //add the color to DynamicBuffer
                    //Color color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
                    //buff.Add(color);

                    //4.计算当前单元所在六个方向的邻居单元颜色
                    Color[] blendColors = new Color[6];
                    //当前单元的颜色
                    Color color = Colors[i];
                    //邻居单元的颜色
                    Color neighbor = color;
                    //判断当前单元所在行数是否为偶数
                    bool ifEven = (z & 1) == 0;
                    //当前单元是否处于行尾
                    bool ifEnd = (i + 1) == (z + 1) * Width;
                    //是否处于行首
                    bool ifStart = i == z * Width;
                    //是否最后一行
                    bool isLastRow = (z == (Height - 1));

                    //0=东北：NE
                    if (!isLastRow)//非最末行
                    {
                        if (ifEven)//偶数行
                        {
                            neighbor = Colors[i + Width];

                        }
                        else
                        {
                            if (ifEnd)//最末尾没有相邻的单元
                            {
                                neighbor = color;
                            }
                            else
                            {
                                neighbor = (Colors[i + Width + 1]);
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
                        neighbor = (Colors[i + 1]);
                    }

                    blendColors[1] = neighbor;
                    //东南2：SE
                    if (i < Width)
                    {
                        neighbor = color;
                    }
                    else
                    {
                        if (ifEven)
                        {
                            neighbor = (Colors[i - Width]);
                        }
                        else
                        {
                            if (ifEnd)
                            {
                                neighbor = color;
                            }
                            else
                            {

                                neighbor = (Colors[i - Width + 1]);
                            }
                        }
                    }
                    blendColors[2] = neighbor;
                    //西南3：SW
                    if (i < Width) neighbor = color;
                    else
                    {
                        if (ifEven)
                        {
                            if (ifStart) neighbor = color;
                            else
                                neighbor = (Colors[i - Width - 1]);
                        }
                        else
                            neighbor = (Colors[i - Width]);
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
                        neighbor = (Colors[i - 1]);
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
                                neighbor = (Colors[i + Width - 1]);
                            }
                        }
                        else
                        {
                            neighbor = (Colors[i + Width]);
                        }
                    }
                    blendColors[5] = neighbor;
                    //5.设置每个六边形单元的数据
                    CommandBuffer.SetComponent(index, instance, new Cell
                    {
                        Index=i,
                        Color = color,
                        Position= new Vector3(_x, 0F, _z),
                        NE=blendColors[0],
                        E=blendColors[1],
                        SE=blendColors[2],
                        SW=blendColors[3],
                        W=blendColors[4],
                        NW= blendColors[5]
                    });

                    //6.设置位置,目前来看，没有必要使用Translation
                    //CommandBuffer.SetComponent(Index, instance, new Translation
                    //{
                    //    Value = new float3(_x, 0F, _z)

                    //});
                    CommandBuffer.AddComponent<OnCreateTag>(index,instance);
                    //CommandBuffer.AddComponent<SwitcherTag>(index, instance);
                    i++;
                }
            }

            //7.摧毁使用完的预设，节约内存资源
            CommandBuffer.DestroyEntity(index, hexCellPrefab);
            CommandBuffer.RemoveComponent<OnCreateTag>(index,entity);
            Colors.Dispose();
        }
    }

    /// <summary>
    /// 如果有新地图,则启动任务
    /// </summary>
    /// <param name="inputDeps">依赖</param>
    /// <returns>任务句柄</returns>
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        var spawnJob = new SpawnJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(spawnJob);

        return spawnJob;

    }
}
