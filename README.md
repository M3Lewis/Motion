# Motion

> 基于pOd_GH_Animation和Telepathy修改的一个GH Slider动画出图插件



* `ExportSliderAnimation`
  * 支持导出`.png`
  * 支持`Raytraced(光线追踪)`显示模式导出
  * 根据Slider范围自动调整对应导出的帧

* `GraphMapperChanger`
  * 修改`Graph Mapper`的Y坐标，同时将X范围与`pOd_Timeline Slider`同步

* 自动根据`pOd_GH_Animation`插件的`pOd_Timeline Slider`的最大值/最小值范围来修改`Graph Mapper`/`Sender`/`Receiver`的取值范围
* 当`Sender`/`Receiver`连接的`Slider`处于锁定状态时，与`Receiver`在同一个`GH_Group`内的所有`Component`也会保持`锁定`/`隐藏`状态
  * 双击`Receiver`弹出`RemoteData`，将摄像机数据与其连接，并使用`MergeMotionData`(右键菜单选择是Location还是Target)
  * 双击`RemoteData`可以选择连接至Location还是Target
  * `MergeMotionData`和`RemoteData`的`Nickname`与slider区间保持同步

* 工具栏中提供了一个按钮，打开时可以做到按`+`/`-`来切换已有的`Grasshopper Named View`
* `Timeline Range`用于读取当前所有slider的区间，`Range Selector`用于选择区间

