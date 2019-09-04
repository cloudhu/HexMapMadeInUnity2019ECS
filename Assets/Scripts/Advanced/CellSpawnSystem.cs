using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 六边形单元生成系统
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
public class CellSpawnSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    private EntityQuery m_Spawner;
    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        var query = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<Data>(), ComponentType.ReadOnly<NewDataTag>() }
        };
        m_Spawner = GetEntityQuery(query);
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<Data,NewDataTag> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        
        [BurstCompile]
        public void Execute(Entity entity, int index, [ReadOnly]ref Data createrData,[ReadOnly]ref NewDataTag tag)
        {
            //0.代码生成预设，这样可以优化性能
            Entity hexCellPrefab = CommandBuffer.CreateEntity(index);
            CommandBuffer.AddComponent<Cell>(index, hexCellPrefab);
            CommandBuffer.AddComponent<Neighbors>(index, hexCellPrefab);
            CommandBuffer.AddComponent<NeighborsIndex>(index, hexCellPrefab);
            CommandBuffer.AddComponent<ChunkData>(index, hexCellPrefab);
            CommandBuffer.AddComponent<River>(index, hexCellPrefab);
            //1.添加颜色数组，这个数组以后从服务器获取，然后传到这里来处理
            Random random = new Random(1208905299U);
            int cellCountX = createrData.CellCountX;
            int cellCountZ = createrData.CellCountZ;
            int totalCellCount = cellCountZ * cellCountX;
            //保存单元颜色的原生数组
            NativeArray<Color> Colors=new NativeArray<Color>(totalCellCount, Allocator.Temp);
            //保存单元海拔的原生数组
            NativeArray<int> Elevations = new NativeArray<int>(totalCellCount, Allocator.Temp);
            //河流的源头
            NativeList<int> riverSources = new NativeList<int>(totalCellCount/12,Allocator.Temp);
            //流入的河流索引
            NativeArray<int> riverIn = new NativeArray<int>(totalCellCount, Allocator.Temp);
            Colors[0] = Color.green;//使第一个单元成为河流的源头
            Elevations[0] = 5;
            riverSources.Add(0);
            //后面将从服务器获取这些数据，现在暂时随机生成
            for (int i = 1; i < cellCountZ* cellCountX; i++)
            {
                Colors[i]= new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
                int elevtion = random.NextInt(6);
                Elevations[i]= elevtion;
                if (elevtion >= 5) riverSources.Add(i);
            }

            for (int z = 0,i=0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {

                    //2.实例化
                    var instance = CommandBuffer.Instantiate(index, hexCellPrefab);

                    //3.计算阵列对应的六边形单元坐标
                    float _x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
                    float _y = Elevations[i] * HexMetrics.elevationStep;
                    float _z = z * (HexMetrics.OuterRadius * 1.5f);

                    //4.计算当前单元所在六个方向的邻居单元颜色
                    Color[] blendColors = new Color[6];
                    int[] directions = new int[6];
                    //当前单元的颜色
                    Color color = Colors[i];
                    //邻居单元的颜色
                    Color neighbor = color;
                    int direction = int.MinValue;
                    //判断当前单元所在行数是否为偶数
                    bool ifEven = (z & 1) == 0;
                    //当前单元是否处于行尾
                    bool ifEnd = (i + 1) == (z + 1) * cellCountX;
                    //是否处于行首
                    bool ifStart = i == z * cellCountX;
                    //是否最后一行
                    bool isLastRow = (z == (cellCountZ - 1));

                    //0=东北：NE
                    if (!isLastRow)//非最末行
                    {
                        if (ifEven)//偶数行
                        {
                            
                            neighbor = Colors[i + cellCountX];
                            direction = i + cellCountX;
                        }
                        else
                        {
                            if (!ifEnd)//最末尾没有相邻的单元
                            {
                                neighbor = (Colors[i + cellCountX + 1]);
                                direction = i + cellCountX + 1;
                            }
                        }
                    }

                    directions[0] = direction;
                    blendColors[0] = neighbor;
                    direction = int.MinValue;
                    //颜色混合1 东：E
                    if (ifEnd)
                    {
                        //如果在地图行尾，没有东邻居
                        neighbor = color;
                    }
                    else
                    {
                        neighbor = (Colors[i + 1]);
                        direction = i + 1;
                    }

                    directions[1] = direction;
                    blendColors[1] = neighbor;
                    direction = int.MinValue;
                    //东南2：SE
                    neighbor = color;
                    if(i>=cellCountX)
                    {
                        if (ifEven)
                        {
                            neighbor = (Colors[i - cellCountX]);
                            direction = i - cellCountX;
                        }
                        else
                        {
                            if (!ifEnd)
                            {
                                neighbor = (Colors[i - cellCountX + 1]);
                                direction = i - cellCountX + 1;
                            }
                        }
                    }
                    blendColors[2] = neighbor;
                    directions[2] = direction;
                    direction = int.MinValue;
                    //西南3：SW
                    if (i < cellCountX) neighbor = color;
                    else
                    {
                        if (ifEven)
                        {
                            if (ifStart) neighbor = color;
                            else
                            {
                                neighbor = (Colors[i - cellCountX - 1]);
                                direction = i - cellCountX - 1;
                            }
 
                        }
                        else
                        {
                            neighbor = (Colors[i - cellCountX]);
                            direction = i - cellCountX;
                        }
                    }

                    directions[3] = direction;
                    blendColors[3] = neighbor;
                    direction = int.MinValue;
                    //西4：W
                    if (ifStart)
                    {
                        //如果在地图起始位置，没有西邻居
                        neighbor = color;
                    }
                    else
                    {
                        neighbor = (Colors[i - 1]);
                        direction = i - 1;
                    }
                    blendColors[4] = neighbor;
                    directions[4] = direction;
                    direction = int.MinValue;
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
                                neighbor = (Colors[i + cellCountX - 1]);
                                direction = i + cellCountX - 1;
                            }
                        }
                        else
                        {
                            neighbor = (Colors[i + cellCountX]);
                            direction = i + cellCountX;
                        }
                    }

                    directions[5] = direction;
                    blendColors[5] = neighbor;
                    //初始化河流数据
                    bool hasRiver = false;
                    bool hasOutgoingRiver = false;
                    bool hasIncomingRiver = false;
                    int incomingRiver = int.MinValue;
                    int outgoingRiver = int.MinValue;
                    if (riverSources.Contains(i))
                    {
                        hasRiver = true;
                        int lastE = int.MinValue;
                        for (int j = 0; j < 6; j++)
                        {
                            if (directions[j] != int.MinValue)
                            {
                                int elevationR = Elevations[directions[j]];
                                if (i<directions[j] && !riverSources.Contains(directions[j]) && elevationR<= Elevations[i] && elevationR>lastE)
                                {
                                    hasOutgoingRiver = true;
                                    outgoingRiver = directions[j];
                                    lastE = elevationR;
                                }
                                if (riverIn.Contains(directions[j]))
                                {
                                    if (directions[j] == riverIn[i])
                                    {
                                        incomingRiver = riverIn[i];
                                        hasIncomingRiver = true;
                                    }
                                }
                            }
                        }

                        if (hasOutgoingRiver)
                        {
                            riverIn[outgoingRiver]=i;
                            riverSources.Add(outgoingRiver);
                        }
                        else
                        {
                            hasRiver = hasIncomingRiver;
                        }
                    }

                    //5.设置每个六边形单元的数据
                    CommandBuffer.SetComponent(index, instance, new Cell
                    {
                        Index=i,
                        Color = color,
                        Position= new Vector3(_x, _y, _z),
                        Elevation=Elevations[i],
                        HasRiver=hasRiver
                    });
                    CommandBuffer.SetComponent(index, instance, new River
                    {
                        HasIncomingRiver=hasIncomingRiver,
                        HasOutgoingRiver=hasOutgoingRiver,
                        IncomingRiver=incomingRiver,
                        OutgoingRiver=outgoingRiver
                    });
                    CommandBuffer.SetComponent(index, instance, new Neighbors
                    {
                        NE = blendColors[0],
                        E = blendColors[1],
                        SE = blendColors[2],
                        SW = blendColors[3],
                        W = blendColors[4],
                        NW = blendColors[5],
                        NEElevation = Elevations[directions[0] == int.MinValue ? 0 : directions[0]],
                        EElevation = Elevations[directions[1] == int.MinValue ? 0 : directions[1]],
                        SEElevation = Elevations[directions[2] == int.MinValue ? 0 : directions[2]],
                        SWElevation = Elevations[directions[3] == int.MinValue ? 0 : directions[3]],
                        WElevation = Elevations[directions[4] == int.MinValue ? 0 : directions[4]],
                        NWElevation = Elevations[directions[5] == int.MinValue ? 0 : directions[5]]
                    });
                    CommandBuffer.SetComponent(index, instance, new NeighborsIndex
                    {
                        NEIndex = directions[0],
                        EIndex = directions[1],
                        SEIndex = directions[2],
                        SWIndex = directions[3],
                        WIndex = directions[4],
                        NWIndex = directions[5]
                    });
                    int chunkX = x / HexMetrics.chunkSizeX;
                    int chunkZ = z / HexMetrics.chunkSizeZ;
                    int localX = x - chunkX * HexMetrics.chunkSizeX;
                    int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
                    CommandBuffer.SetComponent(index,instance,new ChunkData
                    {
                        ChunkId= chunkX + chunkZ * createrData.ChunkCountX,
                        ChunkIndex= localX + localZ * HexMetrics.chunkSizeX,
                        CellIndex=i
                    });
                    //6.添加新数据标签NewDataTag组件，激活CellSystem来处理新的数据
                    CommandBuffer.AddComponent<NewDataTag>(index,instance);
                    i++;
                }
            }

            //7.摧毁使用完的预设，节约内存资源
            CommandBuffer.DestroyEntity(index, hexCellPrefab);
            CommandBuffer.RemoveComponent<NewDataTag>(index,entity);
            Colors.Dispose();
            Elevations.Dispose();
            riverSources.Dispose();
            riverIn.Dispose();
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
        }.Schedule(m_Spawner, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(spawnJob);

        return spawnJob;

    }
}
