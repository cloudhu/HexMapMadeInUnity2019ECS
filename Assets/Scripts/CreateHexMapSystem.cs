using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
public class CreateHexMapSystem : JobComponentSystem
{
    private EntityQuery hexCells;
    private EntityQuery hexMesh;

    private Entity meshEntity;
    private HexMeshTag hexMeshTag;
    public bool bIfNewMap=false;

    /// <summary>
    /// 构造函数,被World.CreateSystem调用
    /// </summary>
    public CreateHexMapSystem()
    {

    }

    protected override void OnCreate()
    {
        hexCells = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<HexCellData>(), ComponentType.ReadOnly<Translation>()

            },
        });

        hexMesh = GetEntityQuery(typeof(HexMeshTag), typeof(RenderMesh));
        
        meshEntity = hexMesh.GetSingletonEntity();

        hexMeshTag = EntityManager.GetComponentData<HexMeshTag>(meshEntity);

    }

    /// <summary>
    /// 把所有六边形单元实体的数据传递出去
    /// </summary>
    [BurstCompile]
    private struct GetHexCellDataForRenderMeshJob : IJobForEachWithEntity<Translation, HexCellData> {
        public NativeArray<Vector3> Vertices;
        public NativeArray<Color> Colors;
        public void Execute(Entity entity, int index, [ReadOnly]ref Translation position,[ReadOnly]ref HexCellData hexCellData)
        {
            var center = position.Value;
            Colors[index] = hexCellData.color;
            Vertices[index] = new Vector3
            {
                x = center.x,
                y = center.y,
                z = center.z
            };
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        ///六边形单元顶点集合
        var vertices = new NativeArray<Vector3>(HexMetrics.HexCelllCount, Allocator.TempJob);
        //六边形单元的颜色集合
        var colors = new NativeArray<Color>(HexMetrics.HexCelllCount, Allocator.TempJob);
        var getDataJob = new GetHexCellDataForRenderMeshJob
        {
            Vertices = vertices,
            Colors=colors

        }.Schedule(hexCells, inputDeps);
        getDataJob.Complete();
        //所有六边形单元的总顶点数
        int totalCount = HexMetrics.HexCelllCount * HexMetrics.CellVerticesCount;
        var Vertices = new NativeList<Vector3>(totalCount, Allocator.TempJob);
        var Triangles = new NativeList<int>(totalCount, Allocator.TempJob);
        var Colors= new NativeList<Color>(totalCount, Allocator.TempJob);
        int width = HexMetrics.MapWidth;
        int height= HexMetrics.HexCelllCount/HexMetrics.MapWidth;

        for (int i = 0; i < vertices.Length; i++)
        {//Todo:this is too slow,should do it in a Job with Burst
            ////把最后一个放到第0顺位，后面的依此类推
            Vector3 center;
            if (i+1== vertices.Length)
            {
                center = vertices[0];
            }
            else
            {
                center = vertices[i+1];
            }
            //添加颜色：自身中心区域颜色
            Color color = colors[i];

            //The codes below is redundancy,there must be a better way to do this
            //Todo:代码冗余，需要改进
            //得到当前单元周边六个单元的颜色，并添加到列表中

            //当前单元所在行数
            int currHeight = i==0?0:(i / width);
            
            //判断当前所在行是否为偶数
            bool ifEven = (currHeight & 1) == 0;
            //是否处于行尾
            bool ifEnd = (i + 1) == (currHeight + 1) * width;
            //是否处于行首
            bool ifStart = i == currHeight * width;
            //Debug.Log("当前单元："+i+"处于行首："+ifStart+" |处于行尾："+ifEnd+" |当前行数："+currHeight+"是偶数行:"+ifEven);

            //邻居的颜色
            Color neighbor=color;
            //保存需要混合的颜色
            Color[] blendColors = new Color[6];
            //0=东北：NE
            if (currHeight != (height - 1))
            {
                if (ifEven)//偶数行
                {
                    neighbor = colors[i + width];
                }
                else
                {
                    if (ifEnd)//最末尾没有相邻的单元
                    {
                        neighbor = color;
                    }
                    else
                    {
                        neighbor = (colors[i + width + 1]);
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
                neighbor = (colors[i+1]);
            }

            blendColors[1] = neighbor;
            //东南2：SE
            if (i<width)
            {
                neighbor = color;
            }
            else
            {
                if (ifEven)
                {
                    neighbor = (colors[i - width ]);
                }
                else
                {
                    if (ifEnd)
                    {
                        neighbor = color;
                    }
                    else
                    {
                        neighbor = (colors[i - width+1]);
                    }
                }
            }
            blendColors[2] = neighbor;
            //西南3：SW
            if (i < width) neighbor = color;
            else
            {
                if (ifEven)
                {
                    if (ifStart) neighbor = color;
                    else
                        neighbor = (colors[i - width - 1]);
                }
                else
                    neighbor = (colors[i - width]);
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
                neighbor = (colors[i - 1]);
            }
            blendColors[4] = neighbor;
            //5西北：NW
            if (currHeight == (height - 1))
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
                        neighbor = (colors[i + width - 1]);
                    }
                }
                else
                {
                    neighbor = (colors[i + width]);
                }
            }
            blendColors[5] = neighbor;

            //添加顶点、三角和颜色
            for (int j = 0; j < 6; j++)
            {
                Vector3 V1 = (center + HexMetrics.SolidCorners[j]);
                Vector3 V2 = (center + HexMetrics.SolidCorners[j + 1]);
                int vertexIndex = Vertices.Length;
                //添加中心区域的3个顶点
                Vertices.Add(center);
                Vertices.Add(V1);
                Vertices.Add(V2);
                Colors.Add(color);
                Colors.Add(color);
                Colors.Add(color);
                //添加中心区域的三角
                Triangles.Add(vertexIndex);
                Triangles.Add(vertexIndex + 1);
                Triangles.Add(vertexIndex + 2);

                if (j<=2)
                {
                    if (blendColors[j]==color)
                    {//如果没有相邻的单元，则跳过循环
                        continue;
                    }
                    Color bridgeColor = ((color + blendColors[j]) * 0.5F);
                    vertexIndex += 3;
                    //添加外围桥接区域的4个顶点
                    Vector3 bridge = (HexMetrics.GetBridge(j));
                    Vector3 V3 = (V1 + bridge);
                    Vector3 V4 = (V2 + bridge);
                    Vertices.Add(V1);
                    Vertices.Add(V2);
                    Vertices.Add(V3);
                    Vertices.Add(V4);
                    //添加外围区域的三角
                    Triangles.Add(vertexIndex);
                    Triangles.Add(vertexIndex + 2);
                    Triangles.Add(vertexIndex + 1);
                    Triangles.Add(vertexIndex + 1);
                    Triangles.Add(vertexIndex + 2);
                    Triangles.Add(vertexIndex + 3);
                    //添加桥的颜色
                    Colors.Add(bridgeColor);
                    Colors.Add(bridgeColor);
                    Colors.Add(bridgeColor);
                    Colors.Add(bridgeColor);
                    //添加外圈区域三向颜色混合
                    //int prev = (j - 1) < 0 ? 5 :(j - 1);
                    int next = (j + 1) > 5 ? 0 : (j + 1);
                    if (j<=1 && blendColors[next] != color)
                    {
                        //填充桥三角
                        vertexIndex += 4;
                        //添加桥三角的3个顶点
                        Vertices.Add(V2);
                        Vertices.Add(V4);
                        Vector3 V5 = (V2 + HexMetrics.GetBridge(next));
                        Vertices.Add(V5);
                        //添加桥洞区域的三角
                        Triangles.Add(vertexIndex);
                        Triangles.Add(vertexIndex + 1);
                        Triangles.Add(vertexIndex + 2);
                        Colors.Add((color + blendColors[next] + blendColors[j]) / 3F);
                        Colors.Add((color + blendColors[next] + blendColors[j]) / 3F);
                        Colors.Add((color + blendColors[next] + blendColors[j]) / 3F);
                    }
                }

            }
        }

        Debug.Log(Vertices.Length/36);
        var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(meshEntity);

        renderMesh.mesh.vertices = Vertices.ToArray();
        renderMesh.mesh.triangles = Triangles.ToArray();
        renderMesh.mesh.colors = Colors.ToArray();
        renderMesh.mesh.RecalculateNormals();
        //目前ECS还没有物理引擎支持，所以MeshCollider无效！Todo：添加物理特性
        //var meshColider = EntityManager.GetSharedComponentData<MeshColliderData>(meshEntity);
        //meshColider.HexMeshCollider.sharedMesh = renderMesh.mesh;
        vertices.Dispose();
        colors.Dispose();
        Vertices.Dispose();
        Triangles.Dispose();
        Colors.Dispose();
        hexMeshTag.bIfNewMap = false;
        bIfNewMap = false;

        return getDataJob;

    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        //verticesAsNativeArray.Dispose();
        //trianglesAsNativeArray.Dispose();
    }
}
