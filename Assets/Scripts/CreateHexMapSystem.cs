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

        var vertices = new NativeArray<Vector3>(HexMetrics.HexCelllCount, Allocator.TempJob);
        var colors = new NativeArray<Color>(HexMetrics.HexCelllCount, Allocator.TempJob);
        var getDataJob = new GetHexCellDataForRenderMeshJob
        {
            Vertices = vertices,
            Colors=colors

        }.Schedule(hexCells, inputDeps);
        getDataJob.Complete();
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
            //添加顶点和三角
            for (int j = 0; j < 6; j++)
            {
                int verticesIndex = Vertices.Length;
                Vertices.Add(center);
                Vertices.Add(center + HexMetrics.corners[j]);
                Vertices.Add(center + HexMetrics.corners[j + 1]);
                Triangles.Add(verticesIndex);
                Triangles.Add(verticesIndex + 1);
                Triangles.Add(verticesIndex + 2);
            }
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
            //添加颜色：自身
            Color color = colors[i];
            //邻居的颜色
            Color neighbor=color;
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

            //if (i == 0)
            //{
            //    Debug.Log("HexCellColor：" + color + " |  NE Direction Color：" + neighbor);
            //}
            Colors.Add(color);
            Colors.Add(neighbor);
            Colors.Add(neighbor);
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
            //if (i == 0)
            //{
            //    Debug.Log("HexCellColor：" + color + " |  E Direction Color：" + neighbor);
            //}
            Colors.Add(color);
            Colors.Add(neighbor);
            Colors.Add(neighbor);
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
            //if (i == 0)
            //{
            //    Debug.Log("HexCellColor：" + color + " |  SE Direction Color：" + neighbor);
            //}
            Colors.Add(color);
            Colors.Add(neighbor);
            Colors.Add(neighbor);
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
            //if (i == 0)
            //{
            //    Debug.Log("HexCellColor：" + color + " |  SW Direction Color：" + neighbor);
            //}
            Colors.Add(color);
            Colors.Add(neighbor);
            Colors.Add(neighbor);
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
            //if (i == 0)
            //{
            //    Debug.Log("HexCellColor：" + color + " |  W Direction Color：" + neighbor);
            //}
            Colors.Add(color);
            Colors.Add(neighbor);
            Colors.Add(neighbor);
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
            //if (i == 0)
            //{
            //    Debug.Log("HexCellColor：" + color + " |  NW Direction Color：" + neighbor);
            //}
            Colors.Add(color);
            Colors.Add(neighbor);
            Colors.Add(neighbor);

        }

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
