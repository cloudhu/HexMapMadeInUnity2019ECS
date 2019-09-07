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
    //[BurstCompile]
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
            //用于调试的动态缓存
            //DynamicBuffer<DebugBuffer> debug= CommandBuffer.AddBuffer<DebugBuffer>(index,entity);
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
            //二维数组来保存相邻单元的索引
            int[,] neighborIndexs = new int[totalCellCount,6];

            for (int z = 0, i = 0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {
                    Colors[i] = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());
                    Elevations[i] = random.NextInt(6);
                    if (Elevations[i] >= HexMetrics.RiverSourceElevation) riverSources.Add(i);
                    riverIn[i] = int.MinValue;
                    positions[i] = new Vector3((x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f), Elevations[i] * HexMetrics.ElevationStep, z * (HexMetrics.OuterRadius * 1.5f));
                    //判断当前单元所在行数是否为偶数
                    bool ifEven = (z & 1) == 0;
                    //当前单元是否处于行尾
                    bool notEnd = (i + 1) != (z + 1) * cellCountX;
                    //是否处于行首
                    bool notStart = i != z * cellCountX;
                    //是否最后一行
                    bool notLastRow = (z != (cellCountZ - 1));
                    //默认没有相邻的单元
                    int direction = int.MinValue;
                    //0=东北：NEPosition
                    if (notLastRow)//非最末行
                    {
                        if (ifEven)//偶数行
                        {
                            direction = i + cellCountX;
                        }
                        else
                        {
                            if (notEnd)//最末尾没有相邻的单元
                            {
                                direction = i + cellCountX + 1;
                            }
                        }
                    }

                    neighborIndexs[i,0] = direction;
                    direction = int.MinValue;
                    //颜色混合1 东：EPosition
                    if (notEnd)
                    {
                        direction = i + 1;
                    }

                    neighborIndexs[i,1] = direction;
                    direction = int.MinValue;
                    //东南2：SEPosition
                    if (i >= cellCountX)
                    {
                        if (ifEven)
                        {
                            direction = i - cellCountX;
                        }
                        else
                        {
                            if (notEnd)
                            {
                                direction = i - cellCountX + 1;
                            }
                        }
                    }
                    neighborIndexs[i,2] = direction;
                    direction = int.MinValue;
                    //西南3：SWPosition
                    if (i >= cellCountX)
                    {
                        if (ifEven)
                        {
                            if (notStart)
                            {
                                direction = i - cellCountX - 1;
                            }

                        }
                        else
                        {
                            direction = i - cellCountX;
                        }
                    }

                    neighborIndexs[i,3] = direction;
                    direction = int.MinValue;
                    //西4：WPosition
                    if (notStart)
                    {
                        direction = i - 1;
                    }
                    neighborIndexs[i,4] = direction;
                    direction = int.MinValue;
                    //5西北：NWPosition
                    if (notLastRow)
                    {
                        if (ifEven)
                        {
                            if (notStart)
                            {
                                direction = i + cellCountX - 1;
                            }
                        }
                        else
                        {
                            direction = i + cellCountX;
                        }
                    }

                    neighborIndexs[i,5] = direction;

                    i++;
                }
            }

            for (int z = 0,i=0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {

                    //2.实例化
                    var instance = CommandBuffer.Instantiate(index, hexCellPrefab);

                    //当前单元的颜色
                    Color color = Colors[i];

                    #region River

                    //初始化河流数据
                    bool hasRiver = true;
                    bool hasOutgoingRiver = false;
                    bool hasIncomingRiver = false;
                    int incomingRiver = int.MinValue;
                    int outgoingRiver = int.MinValue;

                    //上一个单元海拔,用来做比较
                    int lastElevation = int.MinValue;

                    //从六个方向寻找河床，无法使用递归函数优化，报错在Job下面的递归函数中
                    for (int j = 0; j < 6; j++)
                    {
                        bool nextHasOutgoingRiver = false;
                        //下一个出口
                        int nextOutgoingRiver = int.MinValue;
                        //低于源头的最高海拔
                        int maxElevation = int.MinValue;
                        int neighborIndex = neighborIndexs[i, j];
                        if (neighborIndex != int.MinValue)//如果是最小值，说明没有相邻单元
                        {
                            //获取相邻单元的海拔
                            int neighborElevation = Elevations[neighborIndex];

                            for (int k = 0; k < 6; k++)
                            {
                                int nextNeighborIndex = neighborIndexs[neighborIndex, k];

                                if (nextNeighborIndex != int.MinValue)
                                {
                                    int nextElevation = Elevations[nextNeighborIndex];
                                    int next2OutgoingRiver = int.MinValue;

                                    int maxE = int.MinValue;
                                    bool next2HasOutgoing = false;

                                    for (int l = 0; l < 6; l++)
                                    {
                                        int next2Index = neighborIndexs[nextNeighborIndex, l];
                                        if (next2Index != int.MinValue)
                                        {
                                            if (riverSources.Contains(nextNeighborIndex))
                                            {
                                                int next2E = Elevations[next2Index];
                                                if (next2E < nextElevation && next2E > maxE)
                                                {
                                                    next2HasOutgoing = true;
                                                    next2OutgoingRiver = next2Index;
                                                    maxE = next2E;
                                                }
                                            }
                                        }
                                    }
                                    //有出水口则保存起来
                                    if (next2HasOutgoing)
                                    {
                                        //当前单元的出水口，正是相邻单元的入水口
                                        if (!riverIn.Contains(nextNeighborIndex) && riverIn[next2OutgoingRiver] == int.MinValue)
                                        {
                                            riverIn[next2OutgoingRiver] = nextNeighborIndex;
                                            if (!riverSources.Contains(next2OutgoingRiver))
                                            {
                                                riverSources.Add(next2OutgoingRiver);
                                            }
                                        }
                                    }

                                    if (riverSources.Contains(neighborIndex))
                                    {
                                        //出口海拔必然低于河源
                                        if (nextElevation < neighborElevation)
                                        {
                                            //为了源远流长，选择与自身海拔相近的单元
                                            if (nextElevation > maxElevation)
                                            {
                                                nextHasOutgoingRiver = true;
                                                maxElevation = nextElevation;
                                                nextOutgoingRiver = nextNeighborIndex;
                                            }
                                        }
                                    }
                                    if (next2OutgoingRiver == neighborIndex && !nextHasOutgoingRiver)
                                    {
                                        for (int l = k - 1; l >= 0; l--)
                                        {
                                            int prevIndex = neighborIndexs[neighborIndex, l];
                                            if (prevIndex != int.MinValue && riverIn[prevIndex] == int.MinValue)
                                            {
                                                int prevE = Elevations[prevIndex];
                                                if (prevE > maxElevation && prevE < Elevations[neighborIndex])
                                                {
                                                    nextHasOutgoingRiver = true;
                                                    nextOutgoingRiver = prevIndex;
                                                    maxElevation = prevE;
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                            //有出水口则保存起来
                            if (nextHasOutgoingRiver)
                            {
                                //当前单元的出水口，正是相邻单元的入水口
                                if (!riverIn.Contains(neighborIndex) && riverIn[nextOutgoingRiver] == int.MinValue)
                                {
                                    riverIn[nextOutgoingRiver] = neighborIndex;
                                    if (!riverSources.Contains(nextOutgoingRiver))
                                    {
                                        riverSources.Add(nextOutgoingRiver);
                                    }
                                }
                            }
                            if (riverSources.Contains(i))
                            {
                                //下游的海拔必然低于河源
                                if (neighborElevation < Elevations[i] && riverIn[neighborIndex] == i)
                                {
                                    //为了源远流长，选择与自身海拔相近的单元
                                    if (neighborElevation > lastElevation)
                                    {
                                        hasOutgoingRiver = true;
                                        outgoingRiver = neighborIndex;
                                        lastElevation = neighborElevation;
                                    }
                                }

                                if (nextOutgoingRiver == i && !hasOutgoingRiver)
                                {
                                    for (int k = j - 1; k >= 0; k--)
                                    {
                                        int prevIndex = neighborIndexs[i, k];
                                        if (prevIndex != int.MinValue && riverIn[prevIndex] == int.MinValue)
                                        {
                                            int prevE = Elevations[prevIndex];
                                            if (prevE > lastElevation && prevE < Elevations[i])
                                            {
                                                hasOutgoingRiver = true;
                                                outgoingRiver = prevIndex;
                                                lastElevation = prevE;
                                            }
                                        }
                                    }
                                }

                                if (!hasOutgoingRiver && j == 5)
                                {
                                    for (int k = 4; k >= 0; k--)
                                    {
                                        int prevIndex = neighborIndexs[i, k];
                                        if (prevIndex != int.MinValue && riverIn[prevIndex] == int.MinValue)
                                        {
                                            int prevE = Elevations[prevIndex];
                                            if (prevE < Elevations[i])
                                            {
                                                hasOutgoingRiver = true;
                                                outgoingRiver = prevIndex;
                                            }
                                        }
                                    }
                                }
                            }

                            //判断入水口，但凡入水口都保存在数组中了
                            if (riverIn.Contains(neighborIndex))
                            {
                                //方向校正，当前单元的入水方向即是相邻单元的出水方向
                                if (neighborIndex == riverIn[i])
                                {
                                    incomingRiver = neighborIndex;
                                    hasIncomingRiver = true;
                                }
                            }
                        }
                    }
                    //有出水口则保存起来
                    if (hasOutgoingRiver)
                    {
                        //当前单元的出水口，正是相邻单元的入水口
                        if (!riverIn.Contains(i) && riverIn[outgoingRiver] == int.MinValue)
                        {
                            riverIn[outgoingRiver] = i;
                            if (!riverSources.Contains(outgoingRiver))
                            {
                                riverSources.Add(outgoingRiver);
                            }
                        }
                    }
                    else
                    {//出水口没有，进水口也没有，则闭源
                        hasRiver = hasIncomingRiver;
                    }
                    
                    //if (hasRiver)//Todo:优化河流系统
                    //{
                    //    CommandBuffer.AddComponent<RiverRenderTag>(index, instance);
                    //}

                    #endregion

                    #region Road

                    //生成道路数据，用一个临时数组来暂存
                    bool[] roads = new bool[6];
                    bool hasRoad = false;
                    //遍历6个方向，判断是否有道路通过
                    for (int j = 0; j < 6; j++)
                    {
                        roads[j] = false;
                        //首先确认该方向有相邻的单元
                        if (neighborIndexs[i, j] != int.MinValue)
                        {
                            //计算海拔差值，海拔相差过大的悬崖峭壁就不修路了
                            int tmpE = Elevations[i] - Elevations[neighborIndexs[i, j]];
                            tmpE = tmpE > 0 ? tmpE : -tmpE;
                            //河流通过的地方不修路，后面会造桥
                            if (tmpE <= 1 && neighborIndexs[i, j] != incomingRiver && neighborIndexs[i, j] != outgoingRiver)
                            {
                                roads[j] = true;
                                hasRoad = true;
                            }
                        }
                    }

                    #endregion

                    #region Water
                    
                    //当前单元是否被水淹没？
                    bool isUnderWater = true;
                    //当前单元的水位
                    float waterLevel = Elevations[i] + HexMetrics.WaterLevelOffset;
                    //相邻单元是否处于水下
                    bool[] neighborIsUnderWater = new bool[6];
                    //单元6边都被高地环绕的地带积水
                    for (int j = 0; j < 6; j++)
                    {
                        neighborIsUnderWater[j] = true;
                        int neighborIndex = neighborIndexs[i, j];
                        if (neighborIndex != int.MinValue)
                        {
                            if (Elevations[neighborIndex] < Elevations[i])
                            {
                                isUnderWater = false;
                                waterLevel = int.MinValue;
                            }

                            for (int k = 0; k < 6; k++)
                            {
                                int nextNeighborIndex = neighborIndexs[neighborIndex, k];
                                if (nextNeighborIndex!=int.MinValue)
                                {
                                    if (Elevations[nextNeighborIndex] < Elevations[neighborIndex])
                                    {
                                        neighborIsUnderWater[j] = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    neighborIsUnderWater[j] = false;
                                    break;
                                }
                            }

                        }
                        else
                        {
                            isUnderWater = false;
                            waterLevel = int.MinValue;
                            neighborIsUnderWater[j] = false;
                        }
                    }
                    //河流尽头积水
                    //if (!isUnderWater)
                    //{
                    //    //有进无出，则积水
                    //    if (hasIncomingRiver && !hasOutgoingRiver)
                    //    {
                    //        isUnderWater = true;
                    //        waterLevel = Elevations[i] + HexMetrics.WaterLevelOffset;
                    //    }
                    //}

                    #endregion

                    #region SetComponent设置每个六边形单元的数据

                    //5.设置每个六边形单元的数据
                    CommandBuffer.SetComponent(index, instance, new Cell
                    {
                        Index = i,
                        Color = color,
                        Position = positions[i],
                        Elevation = Elevations[i],
                        HasRiver = hasRiver,
                        HasRoad = hasRoad,
                        WaterLevel = waterLevel,
                        IsUnderWater = isUnderWater
                    });

                    CommandBuffer.SetComponent(index, instance, new River
                    {
                        HasIncomingRiver = hasIncomingRiver,
                        HasOutgoingRiver = hasOutgoingRiver,
                        IncomingRiver = incomingRiver,
                        OutgoingRiver = outgoingRiver
                    });

                    CommandBuffer.SetComponent(index, instance, new Neighbors
                    {
                        NEColor = neighborIndexs[i, 0] == int.MinValue ? Color.clear : Colors[neighborIndexs[i, 0]],
                        EColor = neighborIndexs[i, 1] == int.MinValue ? Color.clear : Colors[neighborIndexs[i, 1]],
                        SEColor = neighborIndexs[i, 2] == int.MinValue ? Color.clear : Colors[neighborIndexs[i, 2]],
                        SWColor = neighborIndexs[i, 3] == int.MinValue ? Color.clear : Colors[neighborIndexs[i, 3]],
                        WColor = neighborIndexs[i, 4] == int.MinValue ? Color.clear : Colors[neighborIndexs[i, 4]],
                        NWColor = neighborIndexs[i, 5] == int.MinValue ? Color.clear : Colors[neighborIndexs[i, 5]],
                        NEElevation = neighborIndexs[i, 0] == int.MinValue ? int.MinValue : Elevations[neighborIndexs[i, 0]],
                        EElevation = neighborIndexs[i, 1] == int.MinValue ? int.MinValue : Elevations[neighborIndexs[i, 1]],
                        SEElevation = neighborIndexs[i, 2] == int.MinValue ? int.MinValue : Elevations[neighborIndexs[i, 2]],
                        SWElevation = neighborIndexs[i, 3] == int.MinValue ? int.MinValue : Elevations[neighborIndexs[i, 3]],
                        WElevation = neighborIndexs[i, 4] == int.MinValue ? int.MinValue : Elevations[neighborIndexs[i, 4]],
                        NWElevation = neighborIndexs[i, 5] == int.MinValue ? int.MinValue : Elevations[neighborIndexs[i, 5]],
                        NEIndex = neighborIndexs[i, 0],
                        EIndex = neighborIndexs[i, 1],
                        SEIndex = neighborIndexs[i, 2],
                        SWIndex = neighborIndexs[i, 3],
                        WIndex = neighborIndexs[i, 4],
                        NWIndex = neighborIndexs[i, 5],
                        NEPosition = neighborIndexs[i, 0] == int.MinValue ? Vector3.left : positions[neighborIndexs[i, 0]],
                        EPosition = neighborIndexs[i, 1] == int.MinValue ? Vector3.left : positions[neighborIndexs[i, 1]],
                        SEPosition = neighborIndexs[i, 2] == int.MinValue ? Vector3.left : positions[neighborIndexs[i, 2]],
                        SWPosition = neighborIndexs[i, 3] == int.MinValue ? Vector3.left : positions[neighborIndexs[i, 3]],
                        WPosition = neighborIndexs[i, 4] == int.MinValue ? Vector3.left : positions[neighborIndexs[i, 4]],
                        NWPosition = neighborIndexs[i, 5] == int.MinValue ? Vector3.left : positions[neighborIndexs[i, 5]],
                        NEIsUnderWater = neighborIsUnderWater[0],
                        EIsUnderWater = neighborIsUnderWater[1],
                        SEIsUnderWater = neighborIsUnderWater[2],
                        SWIsUnderWater = neighborIsUnderWater[3],
                        WIsUnderWater = neighborIsUnderWater[4],
                        NWIsUnderWater = neighborIsUnderWater[5]
                    });

                    CommandBuffer.SetComponent(index, instance, new RoadBools
                    {
                        NEHasRoad = roads[0],
                        EHasRoad = roads[1],
                        SEHasRoad = roads[2],
                        SWHasRoad = roads[3],
                        WHasRoad = roads[4],
                        NWHasRoad = roads[5]
                    });
                    int chunkX = x / HexMetrics.ChunkSizeX;
                    int chunkZ = z / HexMetrics.ChunkSizeZ;
                    int localX = x - chunkX * HexMetrics.ChunkSizeX;
                    int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
                    CommandBuffer.SetComponent(index, instance, new ChunkData
                    {
                        ChunkId = chunkX + chunkZ * createrData.ChunkCountX,
                        ChunkIndex = localX + localZ * HexMetrics.ChunkSizeX,
                        CellIndex = i
                    });
                    //6.添加新数据标签NewDataTag组件，激活CellSystem来处理新的数据
                    CommandBuffer.AddComponent<NewDataTag>(index, instance);
                    i++;

                    #endregion

                }
            }

            #region Dispose回收内存

            //7.摧毁使用完的预设，节约内存资源
            CommandBuffer.DestroyEntity(index, hexCellPrefab);
            CommandBuffer.RemoveComponent<NewDataTag>(index, entity);
            Colors.Dispose();
            Elevations.Dispose();
            riverSources.Dispose();
            riverIn.Dispose();
            positions.Dispose();

            #endregion

        }


        #region 在Job中调用递归报错StackOverflowException: The requested operation caused a stack overflow.

        //FindRiverSoureRecursion(i,2, neighborIndexs, Elevations, ref riverSources, ref riverIn);
        //for (int j = 0; j < 6; j++)
        //{
        //    //相邻单元的索引
        //    int neighborIndex = neighborIndexs[i, j];
        //    //如果是最小值，说明没有相邻单元
        //    if (neighborIndex != int.MinValue)
        //    {
        //        //获取相邻单元的海拔
        //        int neighborElevation = Elevations[neighborIndex];
        //        //出水口的海拔必须低于源头的海拔，水往低处流
        //        if (neighborElevation < Elevations[i])
        //        {
        //            //寻找接近源头的出水口，尽量使水流平缓
        //            if (neighborElevation > lastElevation)
        //            {
        //                hasOutgoingRiver = true;
        //                outgoingRiver = neighborIndex;
        //                lastElevation = neighborElevation;
        //            }
        //        }

        //        //判断入水口，但凡入水口都保存在数组中了
        //        if (riverIn.Contains(neighborIndex))
        //        {
        //            //方向校正，当前单元的入水方向即是相邻单元的出水方向
        //            if (neighborIndex == riverIn[i])
        //            {
        //                incomingRiver = neighborIndex;
        //                hasIncomingRiver = true;
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// 递归寻找河源StackOverflowException: The requested operation caused a stack overflow.
        /// </summary>
        //void FindRiverSoureRecursion(int currIndex,int deep,int[,] neighborIndexs, NativeArray<int> Elevations,ref NativeList<int> riverSources,ref NativeArray<int> riverIn)
        //{
        //    //如果当前单元就是源头
        //    if (riverSources.Contains(currIndex))
        //    {
        //        //与源头最接近的最高海拔
        //        int maxElevation = int.MinValue;
        //        //当前单元是否有出水口
        //        bool hasOutgoingRiver = false;
        //        //出水口索引
        //        int outgoingRiver = int.MinValue;
        //        //从六个方向寻找河床
        //        for (int j = 0; j < 6; j++)
        //        {
        //            //相邻单元的索引
        //            int neighborIndex = neighborIndexs[currIndex, j];
        //            //如果是最小值，说明没有相邻单元 && 如果已经有入水口，说明出水口已经被占用
        //            if (neighborIndex != int.MinValue && riverIn[neighborIndex]==int.MinValue)
        //            {
        //                //获取相邻单元的海拔
        //                int neighborElevation = Elevations[neighborIndex];
        //                //出水口的海拔必须低于源头的海拔，水往低处流
        //                if (neighborElevation< Elevations[currIndex])
        //                {
        //                    //寻找接近源头的出水口，尽量使水流平缓
        //                    if (neighborElevation>maxElevation)
        //                    {
        //                        hasOutgoingRiver = true;
        //                        outgoingRiver = neighborIndex;
        //                        maxElevation = neighborElevation;
        //                    }
        //                }

        //            }
        //        }

        //        //有出水口则保存起来
        //        if (hasOutgoingRiver)
        //        {
        //            //当前单元的出水口，正是相邻单元的入水口
        //            if (!riverIn.Contains(currIndex))
        //            {
        //                riverIn[outgoingRiver] = currIndex;
        //                //有水流入，则说明自身也成为水源了
        //                if (!riverSources.Contains(outgoingRiver))
        //                {
        //                    riverSources.Add(outgoingRiver);
        //                }
        //            }
        //        }
        //    }
        //    else//当前单元不是水源，则递归寻找水源
        //    {
        //        //从六个方向寻找水源
        //        for (int j = 0; j < 6; j++)
        //        {
        //            //相邻单元的索引
        //            int nextIndex = neighborIndexs[currIndex, j];
        //            //如果是最小值，说明没有相邻单元
        //            if (nextIndex != int.MinValue && deep>0)
        //            {
        //                //递归寻找源头
        //                FindRiverSoureRecursion(nextIndex,deep--, neighborIndexs, Elevations, ref riverSources, ref riverIn);
        //            }
        //        }
        //    }

        //}
        #endregion
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
