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

##日志Wiki

[Wiki日志](https://github.com/cloudhu/HexMapMadeInUnity2019ECS/wiki)
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
