# HexMapMadeInUnity2019ECS
## Versions
 - Unity2019.1.12f1
 - Entities 0.1.1
 - Jobs 0.1.1
 - Burst 1.1.2
 - Hybrid Renderer 0.1.1
 - Mathematics 1.1.0
## What's this system for?

 - I'm tring to develop a auto create map system which can create Infinite map.
 - Wherever players go,there always a perfect new map for them.
 - Like the MagaCity,but much bigger than it,Player is alway in the center of the current little map,long before the player reach the edge of the map new mesh will be automatically created by the map system and send the data to the Server which updates the map database.
 - If the Server map data changed,other players also be noticed.
 - The system also automatically predict where the player may go,then create new map mesh or update from the Server database if there're datas for the direction the player may go.
 - And there is always culling map for less memory usage like the MagaCity did.Alway save resources for other system.
 - So I want to use the ECS for this system,I think it would be awesome if I did this.The lists is for mesh vertices and triangles.

 - If you can help me,please check this out! Star and Fork!

 - I really need some help,thank you very much!
# 自动生成地图系统:这个系统是做什么的?

ECS的世界由许许多多的系统来操控，在进入主世界的时候会创建这些系统!
- 我试图开发一个自动创建地图系统，可以创建无限地图。
- 无论玩家走到哪里，总是有一个完美的新地图等待他们去探索。
- 和MagaCity一样，但比MagaCity大得多的是，玩家始终处于当前小地图的中心，远在玩家到达地图边缘之前，地图系统就会自动创建新的网格，并将数据发送到更新地图数据库的服务器。
- 如果服务器地图数据改变，其他玩家也会被服务器通知到。
- 系统还自动预测玩家可能去的地方，然后创建新的地图网格或从服务器数据库更新，如果服务器上有玩家可能去的方向的地图数据。
- 总是有地图裁剪，以减少内存使用，像MagaCity所做的。总是为其他系统节省资源。
- 所以我想用ECS来做这个系统，我想如果我这样做了会很棒。列表用于网格顶点和三角。
- 如果你能帮我，请一起参与进来，点星和叉!
- 我真的需要一些帮助，非常感谢!

上一篇中PlayerInputSystem负责处理玩家的操作，与之对应的组件有UserCommand（用户命令），TargetPosition（目标位置）和MoveSpeed（移动速度）。原本想一起看看源码，加一点注释进去，算是走马观花，画蛇添足。不过，这样做实在没有太多营养价值，如果大家有兴趣，自行看下源码吧。这一篇想写一点创造性的东西，例如生动生成地图系统。
### AutoCreateMapSystem
灵感来源于[Unity Hex Map Tutorial](https://catlikecoding.com/unity/tutorials/hex-map/)，我觉得自动生成地图这件事情太适合ECS了，为什么？

 - 自动生成的地图涉及到大量的实体；
 - ECS的性能是为大世界而生，在其性能加持下，我们可以生成无限世界；
 - 逻辑解耦，分工明确。

不管怎样，都值得尝试一下。
说下我的大概需求：

 1. 自动生成地图，利用各种System来制定地图的规则，使其尽量贴近自然；
 2. 无限地图，玩家离地图边缘一定距离后，预判玩家行走线路并在其方向上动态扩展；
 3. 将地图数据保存到服务器，与其他玩家进行同步；
 4. 动态加载和动态裁剪，以最小的资源做出最大的地图；
 5. 地图与玩家互动，可破坏，可创建，所有操作进行网络同步。

### 神奇的六边形
我觉得像MegaCity那样的大地图，太吃资源，如果把地图的所有一切都转换成数据。然后再通过数据来驱动无限地图，这样也许很有意思，但是也不是随机生成所有一切，要利用算法来尽量还原大自然的规则。
大概就是这样，我们先从最简单的开始，一步一步实现我们的需求。就先从六边形开始吧！
 国外的大佬解释了六边形有多么神奇和好用，蜜蜂选择六边形来筑巢，足以说明这个东西道法自然，详情点上面的链接了解。
```javascript
using UnityEngine;
/// <summary>
/// 六边形常量
/// </summary>
public static class HexMetrics {

    /// <summary>
    /// 总的顶点数，一个六边形有18个顶点
    /// </summary>
    public static int totalVertices = 18;
    
    /// <summary>
    /// 六边形外半径=六边形边长
    /// </summary>
    public const float outerRadius = 10f;

    /// <summary>
    /// 六边形内半径=0.8*外半径
    /// </summary>
    public const float innerRadius = outerRadius * 0.866025404f;

    /// <summary>
    /// 六边形的六个角组成的数组
    /// </summary>
	public readonly static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),//最顶上那个角作为起点，顺时针画线
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),//顺数第二个
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),//顺数第三个
		new Vector3(0f, 0f, -outerRadius),//依次类推，坐标如下图所示
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};
}
```
![在这里插入图片描述](https://img-blog.csdnimg.cn/20190817164718204.png?x-oss-process=image/watermark,type_ZmFuZ3poZW5naGVpdGk,shadow_10,text_aHR0cHM6Ly9ibG9nLmNzZG4ubmV0L3FxXzMwMTM3MjQ1,size_16,color_FFFFFF,t_70)
如图，红色虚线代表内半径，蓝色实线代表外半径，而其数值都是相对固定的常量，因此这里直接定义出来。
根据这些常量，设定圆心坐标为（0，0，0），我们以最上角最为起点，就可以得出六个角的顶点坐标了。

### 六边形实体
接下来创建六边形实体，如下图所示：
![在这里插入图片描述](https://img-blog.csdnimg.cn/20190817165544962.png)
实际上就是个空对象，我本来要通过ConvertToEntity将其转化成实体的，但是出了一个红色警报，只好移除，保留E脚本：
```javascript
/// <summary>
/// E:六边形单元
/// </summary>
[RequiresEntityConversion]
public class HexCellEntity : MonoBehaviour,IConvertGameObjectToEntity {

    /// <summary>
    /// 三维坐标
    /// </summary>
    public int X;
    public int Y;
    public int Z;

    /// <summary>
    /// 颜色
    /// </summary>
    public Color Color;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        //数据交给C保存
        dstManager.AddComponentData(entity, new HexCellData
        {
            X=this.X,
            Y=this.Y,
            Z=this.Z,
            color=Color,
            RadiansPerSecond= math.radians(DegreesPerSecond)
        });
        //添加父组件
        dstManager.AddComponent(entity, typeof(Parent));
        //添加相对父类的本地位置组件
        dstManager.AddComponent(entity, typeof(LocalToParent));
    }

}
```
对应的C组件：
```javascript
/// <summary>
/// C:保存六边形的坐标和颜色数据
/// </summary>
[Serializable]
public struct HexCellData : IComponentData
{
    public int X;
    public int Y;
    public int Z;
    public Color color;
    public float RadiansPerSecond;
}
```
暂时设定六边形的功能是旋转，后面再更改成变色：
```javascript

/// <summary>
/// S:这里暂时只做旋转，后面会变色等
/// </summary>
public class HexCellSystem : JobComponentSystem {
    EntityQuery m_Group;//查询到特定组件的实体，将其放入这个组中

    /// <summary>
    /// 这里根据类型来查询到特定的实体
    /// </summary>
    protected override void OnCreate()
    {
        ///typeof(Rotation)=带有Rotation组件的；ComponentType=对应HexCellData组件类型的
        /// ReadOnly=只读会加快获取实体的速度，ReadWrite=读写 则相对较慢
        m_Group = GetEntityQuery(typeof(Rotation), ComponentType.ReadOnly<HexCellData>());
    }

    [BurstCompile]//同样使用Burst编译器来加速，区别是使用了块接口：IJobChunk
    struct RotationSpeedJob : IJobChunk {
        /// <summary>
        /// 时间
        /// </summary>
        public float DeltaTime;

        /// <summary>
        /// 原型块组件类型=Rotation
        /// </summary>
        public ArchetypeChunkComponentType<Rotation> RotationType;

        /// <summary>
        /// 只读 原型块组件类型=HexCellData
        /// </summary>
        [ReadOnly]
        public ArchetypeChunkComponentType<HexCellData> RotationSpeedType;

        /// <summary>
        /// 找出满足条件的实体来执行
        /// </summary>
        /// <param name="chunk"><原型块/param>
        /// <param name="chunkIndex">块索引</param>
        /// <param name="firstEntityIndex">第一个实体索引</param>
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkRotations = chunk.GetNativeArray(RotationType);
            var chunkRotationSpeeds = chunk.GetNativeArray(RotationSpeedType);
            for (var i = 0; i < chunk.Count; i++)
            {
                var rotation = chunkRotations[i];
                var rotationSpeed = chunkRotationSpeeds[i];

                chunkRotations[i] = new Rotation
                {
                    Value = math.mul(math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime))
                };
            }
        }
    }

    /// <summary>
    /// 这个方法在主线程上每帧运行
    /// </summary>
    /// <param name="inputDependencies">输入依赖</param>
    /// <returns></returns>
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        // Explicitly declare: 声明
        // - Read-Write access to Rotation 读写的方式访问旋转
        // - Read-Only access to HexCellData 只读的方式访问旋转速度
        var rotationType = GetArchetypeChunkComponentType<Rotation>();
        var rotationSpeedType = GetArchetypeChunkComponentType<HexCellData>(true);

        var job = new RotationSpeedJob()
        {
            RotationType = rotationType,
            RotationSpeedType = rotationSpeedType,
            DeltaTime = Time.deltaTime
        };

        return job.Schedule(m_Group, inputDependencies);
    }
}

```
如上代码是六边形单元的基本ECS写法，都是最基础的：

在游戏对象上添加上一个Mesh显示相应的组件就可以让其旋转起来了，其实很简单。
![在这里插入图片描述](https://img-blog.csdnimg.cn/2019081920490989.png)
接下来我们把它做成一个预设，然后再大量生成，以后的大地图就建立在这个六边形单元的基础上。

### 创建者和创建六边形单元系统
接下来我们新建一个空游戏对象，命名为：MapCreater。为其添加ConvertToEntity脚本组件，使其转化为实体，新建一个C#脚本来描述这个实体，命名为CreaterEntity：
```javascript
/// <summary>
/// E:创建者实体
/// </summary>
[RequiresEntityConversion]
public class CreaterEntity : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    /// <summary>
    /// 六边形单元预设
    /// </summary>
    public GameObject HexCellPrefab;

    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    public int MapWidth=6;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    public int MapHeight=6;

    /// <summary>
    /// 地图颜色
    /// </summary>
    public Color defaultColor = Color.white;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        HexMetrics.totalVertices = MapWidth * MapHeight * 18;
        dstManager.AddComponentData(entity, new MapData
        {
            Width=MapWidth,
            Height=MapHeight,
            Prefab = conversionSystem.GetPrimaryEntity(HexCellPrefab),
            Color=defaultColor,
            bIsNewMap=bCreatNewMap
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(HexCellPrefab);
    }
}

```
数据交给C保存起来：
```javascript
/// <summary>
/// C:保存创建者数据
/// </summary>
[Serializable]
public struct CreaterData : IComponentData {
    public int Width;
    public int Height;
    public Entity Prefab;
    public Color Color;
}
```
S:创建六边形单元系统
```javascript
/// <summary>
/// 创建六边形单元系统
/// </summary>
public class CreateHexCellSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    /// <summary>
    /// 是否是新地图
    /// </summary>
    public bool bIfNewMap = true;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<CreaterData> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index, [ReadOnly]ref CreaterData  createrData)
        {

            for (int z = 0; z < createrData.Height; z++)
            {
                for (int x = 0; x < createrData.Width; x++)
                {
                    //1.实例化
                    var instance = CommandBuffer.Instantiate(index, createrData.Prefab);
                    //2.计算阵列坐标
                    float _x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
                    float _z = z * (HexMetrics.outerRadius * 1.5f);
                    //3.设置父组件
                    CommandBuffer.SetComponent(index, instance, new Parent
                    {
                        Value = entity

                    });
                    //4.设置每个单元的数据
                    CommandBuffer.SetComponent(index, instance, new HexCellData
                    {
                        X = x - z / 2,
                        Y = 0,
                        Z = z,
                        color = createrData.Color,

                    });
                    //5.设置位置
                    CommandBuffer.SetComponent(index, instance, new Translation
                    {
                        Value = new float3(_x, 0F, _z)

                    });
                }
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
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),

            }.Schedule(this, inputDeps);

            m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
            job.Complete();
            var mapSystem = World.GetOrCreateSystem<CreateHexMapSystem>();
            mapSystem.bIfNewMap = true;
            //新地图创建完成，关闭创建
            bIfNewMap = false;
            return job;
        }

        return inputDeps;
    }
}

```
![在这里插入图片描述](https://img-blog.csdnimg.cn/20190819223350428.png?x-oss-process=image/watermark,type_ZmFuZ3poZW5naGVpdGk,shadow_10,text_aHR0cHM6Ly9ibG9nLmNzZG4ubmV0L3FxXzMwMTM3MjQ1,size_16,color_FFFFFF,t_70)
如上图所示，我们创建6*6的单元矩阵，但是它们并没有旋转。我们通过Entity Debugger窗口可以看到对应的实体。
我发现Rotation的数据一直都是0，并没有发生旋转，但是代码并没有问题。到官方论坛反馈时，发现是Rotation的API变了！
ECS还处于过渡时期，所以API会经常变动，开发起来非常尴尬。
我发现以前的写法，在做升级之后，就不起作用了。不仅如此，很多物理组件无法使用。
因此这一篇到这里搁浅了，后面找到正确的API继续写。
已经把项目上传到Github，有兴趣的朋友可以看看：[HexMapMadeInUnity2019ECS](https://github.com/cloudhu/HexMapMadeInUnity2019ECS)




## 作者的话
![Alt](https://imgconvert.csdnimg.cn/aHR0cHM6Ly9hdmF0YXIuY3Nkbi5uZXQvNy83L0IvMV9yYWxmX2h4MTYzY29tLmpwZw)
>  <font color=#FF0000 size=3 face="微软雅黑" >**如果喜欢我的文章可以点赞支持一下，谢谢鼓励！如果有什么疑问可以给我留言，有错漏的地方请批评指证！**</font>
> <font color=#008000 size=3 face="微软雅黑"> **如果有技术难题需要讨论，可以加入开发者联盟：566189328（付费群）为您提供有限的技术支持，以及，心灵鸡汤！**</font>
>  <font color=#0000FF size=3 face="微软雅黑">**当然，不需要技术支持也欢迎加入进来，随时可以请我喝咖啡、茶和果汁！**(￣┰￣*)</font>
# ECS系列目录
## [ECS官方示例1：ForEach](https://blog.csdn.net/qq_30137245/article/details/98959135)
## [ECS官方案例2：IJobForEach](https://blog.csdn.net/qq_30137245/article/details/99049676)
## [ECS官方案例3：IJobChunk](https://blog.csdn.net/qq_30137245/article/details/99068336)
## [ECS官方案例4：SubScene](https://blog.csdn.net/qq_30137245/article/details/99071697)
## [ECS官方案例5：SpawnFromMonoBehaviour](https://blog.csdn.net/qq_30137245/article/details/99078586)
## [ECS官方案例6：SpawnFromEntity](https://blog.csdn.net/qq_30137245/article/details/99083411)
## [ECS官方案例7：SpawnAndRemove](https://blog.csdn.net/qq_30137245/article/details/99101996)
## [ECS进阶：FixedTimestepWorkaround](https://blog.csdn.net/qq_30137245/article/details/99166229)
## [ECS进阶：Boids](https://blog.csdn.net/qq_30137245/article/details/99281187)
## [ECS进阶：场景切换器](https://blog.csdn.net/qq_30137245/article/details/99299167)
## [ECS进阶：MegaCity0](https://blog.csdn.net/qq_30137245/article/details/99399378)
## [ECS进阶：MegaCity1](https://blog.csdn.net/qq_30137245/article/details/99542443)
## [UnityMMO资源整合&服务器部署](https://blog.csdn.net/qq_30137245/article/details/99305502)
## [UnityMMO选人流程](https://blog.csdn.net/qq_30137245/article/details/99578650)
## [UnityMMO主世界](https://blog.csdn.net/qq_30137245/article/details/99619769)
## [UnityMMO网络同步](https://blog.csdn.net/qq_30137245/article/details/99674348)
