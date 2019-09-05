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
            CommandBuffer.AddComponent<ChunkData>(index, hexCellPrefab);
            CommandBuffer.AddComponent<River>(index, hexCellPrefab);
            CommandBuffer.AddComponent<RoadBools>(index, hexCellPrefab);
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
            NativeList<int> riverSources = new NativeList<int>(totalCellCount/15,Allocator.Temp);
            //流入的河流索引
            NativeArray<int> riverIn = new NativeArray<int>(totalCellCount, Allocator.Temp);
            //所有单元的坐标
            NativeArray<Vector3> positions = new NativeArray<Vector3>(totalCellCount, Allocator.Temp);
            for (int z = 0, i = 0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {
                    Colors[i] = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
                    Elevations[i] = random.NextInt(6);
                    if (Elevations[i] >= HexMetrics.RiverSourceElevation) riverSources.Add(i);
                    riverIn[i] = -1;
                    positions[i] = new Vector3((x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f), Elevations[i] * HexMetrics.ElevationStep, z * (HexMetrics.OuterRadius * 1.5f));
                    i++;
                }
            }

            for (int z = 0,i=0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {

                    //2.实例化
                    var instance = CommandBuffer.Instantiate(index, hexCellPrefab);

                    //3.计算阵列对应的六边形单元坐标


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

                    //0=东北：NEPosition
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
                    //颜色混合1 东：EPosition
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
                    //东南2：SEPosition
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
                    //西南3：SWPosition
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
                    //西4：WPosition
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
                    //5西北：NWPosition
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
                    //如果当前单元是河源
                    if (riverSources.Contains(i))
                    {
                        hasRiver = true;//河源单元必然有河流
                        //上一个单元海拔,用来做比较
                        int lastElevation = int.MinValue;
                        //从六个方向寻找河床
                        for (int j = 0; j < 6; j++)
                        {
                            if (directions[j] != int.MinValue)//如果是最小值，说明没有相邻单元
                            {
                                int elevationR = Elevations[directions[j]];//获取相邻单元的海拔
                                //先判断出水口：水向东流，则当前所以必然小于相邻索引，否则就在西面
                                if (i<directions[j])
                                {
                                    //如果已经是源头了，则无法在流入了，一个单元最多有一条河流经，且海拔必然低于河源
                                    if (!riverSources.Contains(directions[j]) && elevationR <= Elevations[i])
                                    {
                                        //为了源远流长，选择与自身海拔相近的单元
                                        if (elevationR > lastElevation)
                                        {
                                            hasOutgoingRiver = true;
                                            outgoingRiver = directions[j];
                                            lastElevation = elevationR;
                                        }
                                    }
                                }
                                //判断入水口，但凡入水口都保存在数组中了
                                if (riverIn.Contains(directions[j]))
                                {
                                    //方向校正，当前单元的入水方向即是相邻单元的出水方向
                                    if (directions[j] == riverIn[i])
                                    {
                                        incomingRiver = riverIn[i];
                                        hasIncomingRiver = true;
                                    }
                                }
                            }
                        }
                        //有出水口则保存起来
                        if (hasOutgoingRiver)
                        {
                            //当前单元的出水口，正是相邻单元的入水口
                            riverIn[outgoingRiver]=i;
                            riverSources.Add(outgoingRiver);
                        }
                        else
                        {//出水口没有，进水口也没有，则闭源
                            hasRiver = hasIncomingRiver;
                        }
                    }
                    //if (hasRiver)//Todo:优化河流系统
                    //{
                    //    CommandBuffer.AddComponent<RiverRenderTag>(index, instance);
                    //}

                    //生成道路数据，用一个临时数组来暂存
                    bool[] roads = new bool[6];
                    bool hasRoad = false;
                    //遍历6个方向，判断是否有道路通过
                    for (int j = 0; j < 6; j++)
                    {
                        roads[j] = false;
                        //首先确认该方向有相邻的单元
                        if (directions[j]!=int.MinValue)
                        {
                            //计算海拔差值，海拔相差过大的悬崖峭壁就不修路了
                            int tmpE = Elevations[i] -Elevations[directions[j]];
                            tmpE = tmpE > 0 ? tmpE : -tmpE;
                            //河流通过的地方不修路，后面会造桥
                            if (tmpE<=1 && directions[j]!=incomingRiver && directions[j] !=outgoingRiver)
                            {
                                roads[j] = true;
                                hasRoad = true;
                            }
                        }
                    }
                    //是否被水淹没？
                    bool isUnderWater = false;
                    int waterLevel = int.MinValue;
                    //海拔低于1的低洼地带被水淹没
                    if (Elevations[i]<=1)
                    {
                        isUnderWater = true;
                        waterLevel = Elevations[i] + 3;
                    }
                    //单元6边都被高地环绕的地带积水
                    if (!isUnderWater)
                    {
                        isUnderWater = true;
                        waterLevel = Elevations[i] + 3;
                        for (int j = 0; j < 6; j++)
                        {
                            if (directions[j] != int.MinValue)
                            {
                                if (Elevations[directions[j]]<Elevations[i])
                                {
                                    isUnderWater = false;
                                    waterLevel = int.MinValue;
                                    break;
                                }
                            }
                            else
                            {
                                isUnderWater = false;
                                waterLevel = int.MinValue;
                                break;
                            }
                        }
                    }
                    //河流尽头积水
                    if (!isUnderWater)
                    {
                        //有进无出，则积水
                        if (hasIncomingRiver && !hasOutgoingRiver)
                        {
                            isUnderWater = true;
                            waterLevel = Elevations[i] + 3;
                        }
                    }
                    //5.设置每个六边形单元的数据
                    CommandBuffer.SetComponent(index, instance, new Cell
                    {
                        Index=i,
                        Color = color,
                        Position= positions[i],
                        Elevation=Elevations[i],
                        HasRiver=hasRiver,
                        HasRoad=hasRoad,
                        WaterLevel=waterLevel,
                        IsUnderWater=isUnderWater
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
                        NEElevation = directions[0] == int.MinValue ? int.MinValue : Elevations[directions[0]],
                        EElevation = directions[1] == int.MinValue ? int.MinValue : Elevations[directions[1]],
                        SEElevation = directions[2] == int.MinValue ? int.MinValue : Elevations[directions[2]],
                        SWElevation = directions[3] == int.MinValue ? int.MinValue : Elevations[directions[3]],
                        WElevation = directions[4] == int.MinValue ? int.MinValue : Elevations[directions[4]],
                        NWElevation = directions[5] == int.MinValue ? int.MinValue : Elevations[directions[5]],
                        NEIndex = directions[0],
                        EIndex = directions[1],
                        SEIndex = directions[2],
                        SWIndex = directions[3],
                        WIndex = directions[4],
                        NWIndex = directions[5],
                        NEPosition = directions[0] == int.MinValue ? Vector3.left : positions[directions[0]],
                        EPosition = directions[1] == int.MinValue ? Vector3.left : positions[directions[1]],
                        SEPosition = directions[2] == int.MinValue ? Vector3.left : positions[directions[2]],
                        SWPosition = directions[3] == int.MinValue ? Vector3.left : positions[directions[3]],
                        WPosition = directions[4] == int.MinValue ? Vector3.left : positions[directions[4]],
                        NWPosition = directions[5] == int.MinValue ? Vector3.left : positions[directions[5]]
                    });

                    CommandBuffer.SetComponent(index, instance, new RoadBools
                    {
                        NEBool = roads[0],
                        EBool = roads[1],
                        SEBool = roads[2],
                        SWBool = roads[3],
                        WBool = roads[4],
                        NWBool = roads[5]
                    });
                    int chunkX = x / HexMetrics.ChunkSizeX;
                    int chunkZ = z / HexMetrics.ChunkSizeZ;
                    int localX = x - chunkX * HexMetrics.ChunkSizeX;
                    int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
                    CommandBuffer.SetComponent(index,instance,new ChunkData
                    {
                        ChunkId= chunkX + chunkZ * createrData.ChunkCountX,
                        ChunkIndex= localX + localZ * HexMetrics.ChunkSizeX,
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
            positions.Dispose();
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
