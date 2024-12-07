# Motion

`Motion`是一个GH动画插件，参考了多种动画插件以及烟灰动画课程中的核心思路，旨在更方便地进行GH动画的制作。



## 组件

### 01_Animation

#### Motion Slider

* 点击区间文本框直接修改Slider区间
* 配合工具栏上的`CreateUnionSliderButton`，创建`Motion Union Slider`，之后可以复制粘贴`Motion Slider`，`Motion Union Slider`的区间会自动进行更新
* `Motion Union Slider`的值与`Motion Slider`的值是联动的
* `Union Slider`点击右键菜单可以连接到所有`EventOperation`和`Interval Lock`的时间输入端，避免特殊情况下断开连接

#### Motion Sender

* 将`Motion Sender`放在`Motion Slider`的输出端旁边，会自动连接它的输出端，并显示其区间范围
* 双击`Motion Sender`可以快速创建`Event`以及后接的`Graph Mapper`，`Graph Mapper`的默认模式为`Bezier`

#### Event

* 输入端`Time`接收`Motion Sender`传来的值
* 输入端`Domain`确定想改变的数值范围
* 选择某个组件，点击`HIDE`可以在这个Event的时间范围之外隐藏组件显示，点击`LOCK`可以在这个Event的时间范围之外锁定组件
* 鼠标移动到按钮范围内，会自动绘制指示线，来显示哪些组件被这个Event隐藏/锁定
* 右键菜单
  * 使用空值模式时，只有在组件接收不到值时才会进行对指定组件的隐藏/锁定操作
  * 点击`跳转至EventOperation`，可跳转到该`Event`对应的`EventOperation`

#### EventOperation

* 选中多个位于`Event`后面的`Graph Mapper` ，点击工具栏按钮`ConnectToEventOprationButton`，将创建一个`EventOperation`，并自动与选中的所有`Graph Mapper`相连。如果之前已经正确创建了`Union Slider`，那么其第二个输入端`Time`也会自动和`Union Slider`相连。
* 输入端`Event`前会显示当前事件的值【0-1】
* 输出端`Remapped Value`会显示当前事件输出的值，也就是在`Event`的第二个输入端的区间中所对应的值
* 放大组件可以点击`+`按钮，增加两个输出端
  * 输出当前事件在所有事件中的序号
  * 输出当前事件的值域区间
* 双击组件可以弹出菜单，菜单中会显示所有区间的值，点击指定区间会跳转至对应的`Event`

* `Interval Lock`
  * 指定区间，并将这个组件与指定组件放在一个`Group`内，时间在区间外时会对组内的所有组件进行锁定



### 02_Export

#### ExportSliderAnimation

* 导出`.png`格式图片，透明背景
* `Raytraced(光线追踪)`显示模式导出帧，可设置采样数
* 自定义区间导出帧
* 导出完毕后点击`Open`按钮直接跳转至导出文件夹
* 中途可按ESC停止导出



### 03_Utils

#### AdjustSearchCount

* 调整最大组件搜索数量，最大值为30个



#### FilletEdgeIndex

* 根据Brep和输入的点确定边的序号，配合`FilletEdge`进行倒角



#### ZDepth

* 开启深度图显示模式，类似Rhino中的`ShowZBuffer`命令
* 导出深度图
* 自定义导出深度图的比例，基础分辨率基于当前工作视窗。



#### Arrange Tab Components

* 指定插件`Tab`名称，分组列出所有该插件的电池放置在画布上



#### Dynamic Output

* 自动根据输入端的数据生成对应数量的输出端
  * 如果是一个`list`，则每个输出端都会输出list中的1个数据，并显示**Out+序号**
  * 如果是一个`tree`，则每个输出端都会输出一个路径下的所有数据，并显示**路径号**，例如{0;1}



#### Color Alpha

* 修改颜色的`Alpha`值



#### Motion Text

* 设置文字的各类属性
* 可设置文字字符间距
* 可输入多种颜色，颜色会均分到每个字符
* 输出文字Mesh
* 输出文字边缘线



#### Motion Image Preview

* 读取`Motion Material`提供的材质



#### Motion Material

* 支持所有材质相关参数修改

* 支持输入`Diffuse/Transparency/Environment/Bump`贴图
  * 可输入图片的路径
  * 也可直接输入 **Javid** 和  **Bitmap+** 插件输出的`System.Drawing.Bitmap`格式作为贴图



#### Motion Image Selector

* 读取指定文件夹路径，可根据`Index`输出对应序号的图片



#### Image Transform Settings

* 修改`Diffuse` / `Transparency` / `Bump(暂时有问题) `贴图的Transform



#### Point On View

* 配合`Human`插件的`Render xxx to Screen`，可将Mesh等物体渲染到屏幕
* 输出端1用于视窗预览，方便确定位置
* 输出端2用于导出，是物体在导出图片中的实际位置



## 工具栏按钮

#### SliderControlWPFButton

* 控制`UnionSlider`，双向更新
* 支持输入数值，按`Enter`确定
* 点击`+` /`-` 增减值
  * 按住按钮`0.75S`后，可连续增加/减少值
* 点击`MIN` / `MAX` 跳转到最小值



#### UpdateSenderButton

* 为所有`Motion Slider`添加`Motion Sender`



#### CreateUnionSliderButton

* 先选择`MotionSlider`，点击左键，可创建一个`Union Slider`
* 之后只需要将`Motion Slider`和`Motion Sender`一起复制，即可自动联动`Union Slider`与`Motion Slider`的值和区间
* 右键可解除联动关系



#### ConnectToMultipleEventOperationButton

* 选择多个`Graph Mapper`，点击按钮，新建`Event Operation`并自动连好连线
* 新建的`Event Operation`会自动和`Union Slider`连接
* 如果同时选中了已存在的`Event Operation`和`Graph Mapper`，点击左键也会多对一连接



#### ClickFinderButton

* 点击左键，会闪烁显示当前模型空间内所有GH物件的`BoundingBox`
* 点击Rhino视窗内的GH物件，会自动选择该物件并跳转到其在GH画布的位置



#### AddScribbleWPFButton

* 创建`Scribble`
* 突破字体大小限制
* 可设置5种字体样式
* 直接点击下拉列表选择字体
* 可按单行最大字符数来分行，预览显示



#### RangeSelectorWPFButton

* 选择已存在区间的所有值，作为区间的最小值和最大值，并组成区间
* 区间`Param`会自动创建到画布中央



#### NamedViewSwitchButton

* 切换状态，默认开启
* 开启时，如果当前Grasshopper文档已经有Named View，按下CTRL+ `+`/ `-`键，进行`Named View`的循环切换
* 如果没有创建`Named View`，按键后会进行提示





