<p align="center">
<img width="660" src="https://github.com/DCFApixels/DragonECS/assets/99481254/c09e385e-08c1-4c04-904a-36ad7e25e45b">
</p>

<p align="center">
<img alt="Version" src="https://img.shields.io/github/package-json/v/DCFApixels/DragonECS?color=%23ff4e85&style=for-the-badge">
<img alt="License" src="https://img.shields.io/github/license/DCFApixels/DragonECS?color=ff4e85&style=for-the-badge">
<a href="https://discord.gg/kqmJjExuCf"><img alt="Discord" src="https://img.shields.io/badge/Discord-JOIN-00b269?logo=discord&logoColor=%23ffffff&style=for-the-badge"></a>
<a href="http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781"><img alt="QQ" src="https://img.shields.io/badge/QQ-JOIN-00b269?logo=tencentqq&logoColor=%23ffffff&style=for-the-badge"></a>
</p>

# DragonECS - C# 实体组件系统框架

<table>
  <tr></tr>
  <tr>
    <td colspan="3">Readme Languages:</td>
  </tr>
  <tr></tr>
  <tr>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS/blob/main/README-RU.md">
        <img src="https://github.com/user-attachments/assets/7bc29394-46d6-44a3-bace-0a3bae65d755"></br>
        <span>Русский</span>
      </a>  
    </td>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS">
        <img src="https://github.com/user-attachments/assets/30528cb5-f38e-49f0-b23e-d001844ae930"></br>
        <span>English</span>
      </a>  
    </td>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS/blob/main/README-ZH.md">
        <img src="https://github.com/user-attachments/assets/3c699094-f8e6-471d-a7c1-6d2e9530e721"></br>
        <span>中文</span>
      </a>  
    </td>
  </tr>
</table>

</br>

DragonECS 是一个[实体组件系统](https://www.imooc.com/article/331544)框架。专注于提升便利性、模块性、可扩展性和动态实体修改性能。 用纯C#开发的，没有依赖和代码生成。灵感来自于[LeoEcs Lite](https://github.com/Leopotam/ecslite)。

> [!WARNING]
> 该框架是预发布版本，因此 API 可能会有变化。在 `main` 分支中是当前的工作版本。
>
> 最新版本的 README 是[俄文版](https://github.com/DCFApixels/DragonECS/blob/main/README-RU.md)。
>
> 如果有不清楚的地方，可以在这里提问 [反馈](#反馈)。

## 目录
- [安装](#安装)
- [基础概念](#基础概念)
  - [实体](#实体)
  - [组件](#组件)
  - [系统](#系统)
- [框架概念](#框架概念)
  - [系统管线](#系统管线)
    - [初始化](#初始化)
    - [依赖注入](#依赖注入)
    - [模块](#模块)
    - [排序](#排序)
  - [流程](#流程)
  - [世界](#世界)
  - [池子](#池子)
  - [掩码](#掩码)
  - [方面](#方面)
  - [查询](#查询)
  - [集合](#集合)
  - [ECS入口](#ECS入口)
- [Debug](#debug)
  - [元属性](#元属性)
  - [EcsDebug](#ecsdebug)
  - [性能分析](#性能分析)
- [Define Symbols](#define-symbols)
- [扩展的功能](#扩展的功能)
  - [世界组件](#世界组件)
  - [配置](#配置)
- [使用DragonECS的项目](#使用dragonecs的项目)
- [扩展](#扩展)
- [FAQ](#faq)
- [反馈](#反馈)

</br>

# 安装
版本的语义 [[打开]](https://gist.github.com/DCFApixels/e53281d4628b19fe5278f3e77a7da9e8#file-dcfapixels_versioning_ru-md)
## 环境
必备要求：
+ C# 7.3 的最低版本；

可选要求：
+ 支持NativeAOT；
+ 使用 C# 的游戏引擎：Unity、Godot、MonoGame等。

已测试:
+ **Unity:** 最低版本 2020.1.0；

## 为Unity安装
> 还建议安装[Unity引擎集成](https://github.com/DCFApixels/DragonECS-Unity)扩展。
* ### Unity-软件包
支持以Unity软件包的形式安装。可以通过[git-url添加到PackageManager](https://docs.unity3d.com/cn/2023.2/Manual/upm-ui-giturl.html)或手动添加到`Packages/manifest.json`：
```
https://github.com/DCFApixels/DragonECS.git
```

* ### 作为源代码
框架也可以通过复制源代码添加到项目中。
</br>

# 基础概念
## 实体
**实体**是附加数据的基础。它们以标识符的形式实现，有两种类型：
* `int` - 是在单个更新中使用的一次性标识符。不建议存储`int`标识符，而应使用 `entlong`；
* `entlong` - 是一个长期标识符，包含一整套用于明确识别的信息;
``` c#
// 在世界中创建一个新实体。
int entityID = _world.NewEntity();

// 删除实体。
_world.DelEntity(entityID);

// 一个实体的组件复制到另一个实体。
_world.CopyEntity(entityID, otherEntityID);

// 克隆实体。
int newEntityID = _world.CloneEntity(entityID);
```

<details>
<summary>entlong使用</summary>
 
``` c#
// int 转换为 entlong。
entlong entity = _world.GetEntityLong(entityID);
// 或者
entlong entity = (_world, entityID);

// 检查实体是否还活着。
if (entity.IsAlive) { }

// entlong 转换为 int。如果实体不存在，则会出现异常。
int entityID = entity.ID;
// 或者
var (entityID, world) = entity;
 
// entlong 转换为int。如果实体仍然存在，则返回 true 及其 int 标识符。
if (entity.TryGetID(out int entityID)) { }
```
 
 </details>
 
> **NOTICE:** 没有组件的实体不能存在，空实体会在最后一个组件被删除。

## 组件
**组件**是实体的数据。
```c#
// IEcsComponent 组件存储在普通的存储中。
struct Health : IEcsComponent
{
    public float health;
    public int armor;
}
// IEcsTagComponent 组件存储在为标签优化的存储中。
struct PlayerTag : IEcsTagComponent {}
```

## 系统
**系统**这是基本逻辑，这里定义了实体的行为。系统以用户类的形式实现，用户类至少要实现一个流程接口。基本流程：
```c#
class SomeSystem : IEcsPreInit, IEcsInit, IEcsRun, IEcsDestroy
{
    // 它将在 EcsPipeline.Init() 运行时和 Init 被调用之前被调用一次。
    public void PreInit () { }
    
    // 它将在 EcsPipeline.Init() 运行时和 PreInit 被调用之后被调用一次。
    public void Init ()  { }
    
    // 它将在 EcsPipeline.Run() 运行时调用一次。
    public void Run () { }
    
    // 它将在 EcsPipeline.Destroy() 运行时调用一次。
    public void Destroy () { }
}
```
> 如何实现附加流程在[流程部分](#流程)中描述。

</br>

# 框架概念
## 管线
系统的容器和引擎. 负责设置系统调用队列，提供系统间消息和依赖注入功能。管线以 `EcsPipeline` 类的形式实现。
### 初始化
Builder负责初始化管线。系统被添加到Builder中，然后生成管线。 例子：
```c#
EcsPipeline pipeline = EcsPipeline.New() //创建管线的 Builder。
    // 将 System1 添加到系统队列。
    .Add(new System1())
    // 在 System1 之后将 System2 添加到队列中。
    .Add(new System2())
    // 在 System2 之后将 System3 添加到队列中，但只在一个实例中添加。
    .AddUnique(new System3())
    // 完成管线构造并返回其实例。
    .Build(); 
pipeline.Init(); // 管线初始化。
```

```c#
class SomeSystem : IEcsRun, IEcsPipelineMember
{
    // 获取系统所属管线的实例。
    public EcsPipeline Pipeline { get ; set; }

    public void Run () { }
}
```
> 有一种同时构造和初始化的方法`Builder.BuildAndInit();`
### 依赖注入
框架具有向系统注入依赖的功能。这是一个与管线初始化一起运行的流程，并注入传递给Builder的数据。
> 内置依赖注入的使用是可选的。 
``` c#
class SomeDataA { /* ... */ }
class SomeDataB : SomeDataA { /* ... */ }

// ...
SomeDataB _someDataB = new SomeDataB();
EcsPipeline pipeline = EcsPipeline.New()
    //...
    // 将 _someDataB 的实例注入到实现 IEcsInject<SomeDataB> 的系统中。
    .Inject(_someDataB) 
    // 将实现 IEcsInject<SomeDataA> 的系统添加到注入树中，
    // 这样这些系统也会得到_someDataB。
    .Injector.AddNode<SomeDataA>() // 
    // ...
    .BuildAndInit();

// ...
// IEcsInject<T> 接口及其方法 Inject(T obj) 用于注入。
class SomeSystem : IEcsInject<SomeDataA>, IEcsRun
{
    SomeDataA _someDataA
    // 在本例中，obj 将是 SomeDataB 类型的实例
    public void Inject(SomeDataA obj) => _someDataA = obj;

    public void Run () 
    {
        _someDataA.DoSomething();
    }
}
```

### 模块
实现一个共同特性的系统组可以组合成模块，模块也可以简单地添加到管线中。
``` c#
using DCFApixels.DragonECS;
class Module1 : IEcsModule 
{
    public void Import(EcsPipeline.Builder b) 
    {
        b.Add(new System1());
        b.Add(new System2()); 
        b.AddModule(new Module2());
        // ...
    }
}
```
``` c#
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    .AddModule(new Module1())
    // ...
    .BuildAndInit();
```

### 排序
为了管理系统在管线中的位置，无论添加顺序如何，有两种方式：层级和排序顺序。
#### 层级
系统的队列可以分为层。层定义了队列中插入系统的位置。如果要在队列末尾插入一个系统，无论添加的地方如，可以把这个系统添加到 `EcsConsts.END_LAYER` 层级.
``` c#
const string SOME_LAYER = nameof(SOME_LAYER);
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    // 在最终的 EcsConsts.END_LAYER 层前面插入一个新 SOME_LAYER 层。
    .Layers.Insert(EcsConsts.END_LAYER, SOME_LAYER) 
    // SomeSystem 系统将插入 SAME_LAYER 层。
    .Add(New SomeSystem(), SOME_LAYER) 
    // ...
    .BuildAndInit();
```
嵌入层按以下顺序排列：
* `EcsConst.PRE_BEGIN_LAYER`
* `EcsConst.BEGIN_LAYER`
* `EcsConst.BASIC_LAYER`（默认情况下，系统添加到此层）
* `EcsConst.END_LAYER`
* `EcsConst.POST_END_LAYER`
#### 排序顺序
在同一层内，可以使用 `int` 类型的排序值来排序系统。默认情况下，系统的排序值为 `sortOrder = 0`。
```c#
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    // 将 SomeSystem 系统插入到 EcsConsts.BEGIN_LAYER 层
    // 并且放置在排序值小于 10 的系统之后。
    .Add(New SomeSystem(), EcsConsts.BEGIN_LAYER, 10)
    // ...
    .BuildAndInit();
```

## 流程
流程是实现共同接口的系统队列，例如`IcsRun`接口。用于启动这些流程的是启动器。内置流程会自动启动。还可以实现用户流程。

<details>
<summary>内置流程</summary>
 
* `IEcsPreInit`, `IEcsInit`, `IEcsRun`, `IEcsDestroy` - 生命周期流程`EcsPipeline`.
* `IEcsInject<T>` - [依赖注入](#依赖注入)的流程.
* `IOnInitInjectionComplete` - 也是[依赖注入](#依赖注入)的流程，而是表示初始化注入的完成。

</details>
 
<details>
<summary>用户流程</summary>
 
Для добавления нового процесса создайте интерфейс наследованный от `IEcsProcess` и создайте раннер для него. Раннер это класс реализующий интерфейс запускаемого процесса и наследуемый от `EcsRunner<TInterface>`. Пример:
``` c#
// 流程接口。
interface IDoSomethingProcess : IEcsProcess
{
    void Do();
}
// 启动器实现. 也可以在内置的流程中参考实现示例。
sealed class DoSomethingProcessRunner : EcsRunner<IDoSomethingProcess>, IDoSomethingProcess
{
    public void Do() 
    {
        foreach (var item in Process) item.Do();
    }
}
```
``` c#
// 添加启动器到管线
_pipeline = EcsPipeline.New()
    //...
    .AddRunner<DoSomethingProcessRunner>()
    //...
    .BuildAndInit();

// 如果启动器已经添加，运行它。
_pipeline.GetRunner<IDoSomethingProcess>.Do()

// 如果启动器尚未添加，使用 GetRunnerInstance 将其添加并运行
_pipeline.GetRunnerInstance<DoSomethingProcessRunner>.Do()
```

<details>
<summary>扩展的启动器实现</summary>

``` c#
internal sealed class DoSomethingProcessRunner : EcsRunner<IDoSomethingProcess>, IDoSomethingProcess
{
    // RunHelper 简化了实现，类似于内置流程的实现。
    // 自动调用分析器的标记，同时包含 try-catch 块。
    private RunHelper _helper;
    protected override void OnSetup()
    {
        // 第二个参数是标记的名称，如果不指定，将自动选择名称。
        _helper = new RunHelper(this, nameof(Do));
    }
    public void Do()
    {
        _helper.Run(p => p.Do());
    }
}
```

</details>

> 启动器的实现有一些要求：
> * 必须直接继承自 `EcsRunner<T>`；
> * 启动器只能包含一个接口（除了 `IEcsProcess` 接口）；
> * 继承的 `EcsRunner<T>,` 类必须实现接口 `T`；
    
> 不建议在循环中频繁调用 `GetRunner` 方法，建议缓存获取的启动器实例。
</details>

## 世界
是实体和组件的容器。
``` c#
// 创建世界实例。
_world = new EcsDefaultWorld();
// 创建和删除实体，本例来自实体部分。
var e = _world.NewEntity();
_world.DelEntity(e);
```
> **NOTICE:** 如果实例化的 `EcsWorld` 不再使用，需要调用 `EcsWorld.Destroy()` 来释放它，否则它将继续占用内存。

### 世界配置
为了初始化所需大小的世界并缩短预热时间，可以在构造函数中传递 EcsWorldConfig 的实例。

``` c#
EcsWorldConfig config = new EcsWorldConfig(
    // 预先初始化世界的容量为2000个实体。
    entitiesCapacity: 2000, 
    // 预先初始化池子的容量为2000个组件。
    poolComponentsCapacity: 2000);  
_world = new EcsDefaultWorld(config);
```

## 池子
是组件的存储库，池子有添加/读取/编辑/删除实体上组件的方法。有几种类型的池，用于不同的目的：
* `EcsPool` - 通用池，存储实现 `IEcsComponent` 接口的 struct 组件；
* `EcsTagPool` - 为标签组件优化的特殊池，用于存储带有 `IEcsTagComponent` 的组件;

池有5种主要方法及其品种：
``` c#
// 从世界中获取组件池的一种方法。
EcsPool<Pose> poses = _world.GetPool<Pose>();
 
// 向实体添加一个组件，如果实体已经拥有该组件，则抛出异常。
ref var addedPose = ref poses.Add(entityID);
 
// 返回一个组件，如果实体没有该组件，则抛出异常。
ref var gettedPose = ref poses.Get(entityID);
 
// 返回一个只读组件，如果实体没有该组件，则抛出异常。
ref readonly var readonlyPose = ref poses.Read(entityID);
 
// 如果实体具有组件，则返回true，否则返回false。
if (poses.Has(entityID)) { /* ... */ }
 
// 从实体中删除组件，如果实体没有此组件，则抛发异常。
poses.Del(entityID);
```
> 有一些 “安全 ”方法会首先检查组件是否存在，这些方法的名称以 “Try ”开头。
    
> 可以实现用户池。稍后将介绍这一功能。

## 掩码
用于根据组件的存在与否来过滤实体。
``` c#
// 创建一个掩码，检查实体是否具有组件
// SomeCmp1 和 SomeCmp2，但没有组件 SomeCmp3。
EcsMask mask = EcsMask.New(_world)
    // Inc - 组件存在的条件。
    .Inc<SomeCmp1>()
    .Inc<SomeCmp2>()
    // Exc - 组件不存在的条件。
    .Exc<SomeCmp3>()
    .Build();
```

<details>
<summary>静态掩码</summary>

`EcsMask` 是与特定世界实例绑定的，需要将世界实例传递给 `EcsMask.New(world)`，但是也有 `EcsStaticMask`，它可以在不绑定到世界的情况下创建。

``` c#
class SomeSystem : IEcsRun 
{
    // EcsStaticMask 可以在静态字段中创建。
    static readonly EcsStaticMask _staticMask = EcsStaticMask.Inc<SomeCmp1>().Inc<SomeCmp2>().Exc<SomeCmp3>().Build();

    // ...
}
```
``` c#
// 转换为常规掩码。
EcsMask mask = _staticMask.ToMask(_world);
```

</details>

## 方面
这些是继承自 EcsAspect 的用户类，用于与实体进行交互。方面同时充当池的缓存和实体组件的过滤掩码。可以把方面视为系统处理哪些实体的描述。

简化语法：
``` c#
using DCFApixels.DragonECS;
// ...
class Aspect : EcsAspect
{
    // 缓存池，并将 Pose 添加到包含限制中。
    public EcsPool<Pose> poses = Inc;
    // 缓存池，并将 Velocity 添加到包含限制中。
    public EcsPool<Velocity> velocities = Inc;
    // 缓存池，并将 FreezedTag 添加到排除限制中。
    public EcsTagPool<FreezedTag> freezedTags = Exc;

    // 在查询时将检查包含限制掩码中的组件存在性，
    // 同时确保排除限制中的组件不存在。
    // 还有Opt模式，它只缓存池，不影响掩码。
}
```

显式语法（结果与上面的示例相同）:
``` c#
using DCFApixels.DragonECS;
// ...
class Aspect : EcsAspect
{
    public EcsPool<Pose> poses;
    public EcsPool<Velocity> velocities;
    protected override void Init(Builder b)
    {
        poses = b.Include<Pose>();
        velocities = b.Include<Velocity>();
        b.Exclude<FreezedTag>();
    }
}
```

<details>
<summary>结合方面</summary>

可以把一个方面加入另一个方面，从而组合它们。限制也会被组合
``` c#
using DCFApixels.DragonECS;
...
class Aspect : EcsAspect
{
    public OtherAspect1 otherAspect1;
    public OtherAspect2 otherAspect2;
    public EcsPool<Pose> poses;
 
    protected override void Init(Builder b)
    {
        // 与 SomeAspect1 进行组合。
        otherAspect1 = b.Combine<OtherAspect1>(1);
        // 即使对 OtherAspect1 调用 Combine 方法更早，Aspect 会首先与 OtherAspect2 进行组合，因为默认情况下 order = 0。
        otherAspect2 = b.Combine<OtherAspect2>();
         // 如果 OtherAspect1 或 OtherAspect2 中有 b.Exclude<Pose>() 的限制条件，这里将被替换为 b.Include<Pose>()。
        poses = b.Include<Pose>();
    }
}
```
如果组合的方面存在冲突的限制条件，则新的限制条件将替换先前添加的限制条件。根方面的限制条件始终会替换添加的方面中的限制条件。限制条件组合的视觉示例：
| | cmp1 | cmp2 | cmp3 | cmp4 | cmp5 | разрешение конфликтных ограничений|
| :--- | :--- | :--- | :--- | :--- | :--- |:--- |
| OtherAspect2 | :heavy_check_mark: | :x: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | |
| OtherAspect1 | :heavy_minus_sign: | :heavy_check_mark: | :heavy_minus_sign: | :x: | :heavy_minus_sign: | 对于 `cmp2` 将选择 :heavy_check_mark: |
| Aspect | :x: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | 对于 `cmp1` 将选择 :x: |
| 最终的限制 | :x: | :heavy_check_mark: | :heavy_minus_sign: | :x: | :heavy_check_mark: | |

</details>

## 查询
过滤实体并返回满足特定条件的实体集合。内置查询 `Where` 通过组件掩码匹配条件进行过滤，并有多个重载版本：
+ `EcsWorld.Where(EcsMask mask)` - 基于掩码的普通过滤；
+ `EcsWorld.Where<TAspect>(out TAspect aspect)` - 结合了基于方面掩码的过滤和获取方面；

`Where` 查询既可以应用于 `EcsWorld`，也可以应用于框架的集合（在这方面，`Where` 有点类似于 Linq 中的 `Where`）。此外，还提供了根据 `Comparison<int>` 对实体进行排序的重载。

示例系统：
``` c#
public class SomeDamageSystem : IEcsRun, IEcsInject<EcsDefaultWorld>
{
    class Aspect : EcsAspect
    {
        public EcsPool<Health> healths = Inc;
        public EcsPool<DamageSignal> damageSignals = Inc;
        public EcsTagPool<IsInvulnerable> isInvulnerables = Exc;
        // 不检查此组件的存在与否。
        public EcsTagPool<IsDiedSignal> isDiedSignals = Opt;
    }
    EcsDefaultWorld _world;
    public void Inject(EcsDefaultWorld world) => _world = world;

    public void Run()
    {
        foreach (var e in _world.Where(out Aspect a))
        {
            // 在这里处理具有 Health 和 DamageSignal，但没有 IsInvulnerable 组件的实体。
            ref var health = ref a.healths.Get(e);
            if(health.points > 0)
            {
                health.points -= a.damageSignals.Get(e).points;
                if(health.points <= 0)
                { // 向其他系统发送实体死亡信号。
                    a.isDiedSignals.TryAdd(e);
                }
            }
        }
    }
}
```

> 有一个简化查询语法和组件访问的[扩展](#扩展) - [简单语法](https://gist.github.com/DCFApixels/d7bfbfb8cb70d141deff00be24f28ff0)。
 
## 集合

### EcsSpan
只读且仅在堆栈上分配的实体的集合。包含对数组的引用、长度和世界标识符。类似于 `ReadOnlySpan<int>`.
``` c#
// Where 查询返回 EcsSpan 类型的实体集合。
EcsSpan es = _world.Where(out Aspect a);
// 可以使用 foreach 和 for 进行迭代。
foreach (var e in es)
{
    // ...
}
for (int i = 0; i < es.Count; i++)
{
    int e = es[i];
    // ...
}
```
> 虽然`EcsSpan`只是数组，但它不允许重复实体。

### EcsGroup
基于稀疏集（Sparse Set）的辅助集合，用于存储实体集合，支持 O(1) 的添加、删除、检查等操作。
``` c#
// 获取新的组。EcsWorld 包含组池，
// 因此将创建一个新的组或重新使用空闲的组。
EcsGroup group = EcsGroup.New(_world);
// 将组返回到组池。
group.Dispose();
```
``` c#
// 添加 entityID 实体。
group.Add(entityID);
// 检查 entityID 实体的存在.
group.Has(entityID);
// 删除 entityID 实体。
group.Remove(entityID);
```
``` c#
// WhereToGroup 查询返回 EcsReadonlyGroup 类型的实体集合。
EcsReadonlyGroup es = _world.WhereToGroup(out Aspect a);
// 可以使用 foreach 和 for 进行迭代。
foreach (var e in es) 
{ 
    // ...
}
for (int i = 0; i < es.Count; i++)
{
    int e = es[i];
    // ...
}
```
由于组是没有重复元素的集合，因此组支持集合运算，并实现了接口 `ISet<int>`。编辑方法有两种方式：一种是将结果写入到 groupA 中，另一种是返回一个新的群组：
                                
``` c#
// 合集 groupA 和 groupB。
groupA.UnionWith(groupB);
EcsGroup newGroup = EcsGroup.Union(groupA, groupB);

// 交集 groupA 和 groupB。
groupA.IntersectWith(groupB);
EcsGroup newGroup = EcsGroup.Intersect(groupA, groupB);

// 差集 groupA 和 groupB。
groupA.ExceptWith(groupB);
EcsGroup newGroup = EcsGroup.Except(groupA, groupB);

// 对称差集 groupA 和 groupB。
groupA.SymmetricExceptWith(groupB);
EcsGroup newGroup = EcsGroup.SymmetricExcept(groupA, groupB);

// 全部实体与 groupA 的差集。
groupA.Inverse();
EcsGroup newGroup = EcsGroup.Inverse(groupA);
```

## ECS入口
这是一个用户定义的类，作为 ECS 的入口点。其主要目的是初始化和启动每个 Update 引擎上的系统，并在使用结束后释放资源。
### Unity示例
``` c#
using DCFApixels.DragonECS;
using UnityEngine;
public class EcsRoot : MonoBehaviour
{
    private EcsPipeline _pipeline;
    private EcsDefaultWorld _world;
    private void Start()
    {
        // 创建实体和组件的世界。
        _world = new EcsDefaultWorld();
        // 创建系统的管线。
        _pipeline = EcsPipeline.New()
            // 添加系统。
            // .Add(new SomeSystem1())
            // .Add(new SomeSystem2())
            // .Add(new SomeSystem3())

            // 将世界注入系统。
            .Inject(_world)
            // 其他注入。
            // .Inject(SomeData)

            // 完成管线构造。
            .Build();
        // 初始化管线并运行每个添加系统的 IEcsPreInit.PreInit() 
        // 和 IEcsInit.Init()。
        _pipeline.Init();
    }
    private void Update()
    {
        // 运行每个添加系统的 IEcsRun.Run() 方法。
        _pipeline.Run();
    }
    private void OnDestroy()
    {
        // 运行每个添加系统的 IEcsDestroy.Destroy() 方法。
        _pipeline.Destroy();
        _pipeline = null;
        // 必须销毁不再使用的世界。
        _world.Destroy();
        _world = null;
    }
}
```
### 公用示例
``` c#
using DCFApixels.DragonECS;
public class EcsRoot
{
    private EcsPipeline _pipeline;
    private EcsDefaultWorld _world;
    // 环境的初始化。
    public void Init()
    {
        // 创建实体和组件的世界。
        _world = new EcsDefaultWorld();
        // 创建系统的管线。
        _pipeline = EcsPipeline.New()
            // 添加系统。
            // .Add(new SomeSystem1())
            // .Add(new SomeSystem2())
            // .Add(new SomeSystem3())

            // 将世界注入系统。
            .Inject(_world)
            // 其他注入。
            // .Inject(SomeData)

            // 完成管线构造。
            .Build();
        // 初始化管线并运行每个添加系统的 IEcsPreInit.PreInit() 
        // 和 IEcsInit.Init()。
        _pipeline.Init();
    }

    // 引擎的 Update 循环。
    public void Update()
    {
        // 运行每个添加系统的 IEcsRun.Run() 方法。
        _pipeline.Run();
    }

    // 环境的清理。
    public void Destroy()
    {
        // 运行每个添加系统的 IEcsDestroy.Destroy() 方法。
        _pipeline.Destroy();
        _pipeline = null;
        // 必须销毁不再使用的世界。
        _world.Destroy();
        _world = null;
    }
}
```

</br>

# Debug
该框架提供了额外的调试和日志记录工具，不依赖于环境此外，许多类型都有自己的 DebuggerProxy，以便在 IDE 中更详细地显示信息。
## 元属性
默认情况下，元属性没有用处，在与引擎集成时用于指定在调试工具和编辑器中的显示方式。还可以用于生成自动文档。
``` c#
using DCFApixels.DragonECS;

// 设置用户自定义类型名称，默认情况下使用类型名称。
[MetaName("SomeComponent")]

// 用于对类型进行分组。
[MetaGroup("Abilities", "Passive", ...)] // 或者 [MetaGroup("Abilities/Passive/...")]

// 使用 RGB 编码设置显示颜色，每个通道的值范围从0到255，默认为白色。
[MetaColor(MetaColor.Red)] // 或者 [MetaColor(255, 0, 0)]
 
// 为类型添加描述。
[MetaDescription("The quick brown fox jumps over the lazy dog")] 

// 添加字符串唯一标识符.
[MetaID("8D56F0949201D0C84465B7A6C586DCD6")] // 字符串必须是唯一的，并且不允许包含字符 ,<> 。

// 添加字符串标签。
[MetaTags("Tag1", "Tag2", ...)]  // 使用 [MetaTags(MetaTags.HIDDEN))] 可隐藏在编辑器中。
public struct Component : IEcsComponent { /* ... */ }
```
获取元信息：
``` c#
TypeMeta typeMeta = someComponent.GetMeta();
// 或者
TypeMeta typeMeta = pool.ComponentType.ToMeta();

var name = typeMeta.Name; // [MetaName]
var group = typeMeta.Group; // [MetaGroup]
var color = typeMeta.Color; // [MetaColor]
var description = typeMeta.Description; // [MetaDescription]
var metaID = typeMeta.MetaID; // [MetaID]
var tags = typeMeta.Tags; // [MetaTags]
```
> 为了自动生成唯一的标识符 MetaID，可以使用 `MetaID.GenerateNewUniqueID()` 方法和 [浏览器生成器](https://dcfapixels.github.io/DragonECS-MetaID_Generator_Online/)。

## EcsDebug
具有调试和日志记录方法集. 实现为一个静态类，调用 DebugService 的方法.  DebugService 是环境调试系统与 EcsDebug 之间的中介. 这使得可以将项目移植到其他引擎上，而无需修改项目的调试代码，只需要实现特定的 DebugService 即可。

默认情况下使用 `DefaultDebugService` 会将日志输出到控制台. 要实现自定义的，可以创建继承自`DebugService'的类并实现抽象类成员。

``` c#
// 输出日志。
EcsDebug.Print("Message");

// 输出带标签的日志。
EcsDebug.Print("Tag", "Message");

// 中断游戏。
EcsDebug.Break();

// 设置其他 DebugService。
EcsDebug.Set<OtherDebugService>();
```

## 性能分析
``` c#
// 创建名为 SomeMarker 的标记器。
private static readonly EcsProfilerMarker _marker = new EcsProfilerMarker("SomeMarker");
```
``` c#
_marker.Begin();
// 要测量速度的代码。
_marker.End();

// 或者

using (_marker.Auto())
{
    // 要测量速度的代码。
}
```

</br>

# Define Symbols
+ `DRAGONECS_DISABLE_POOLS_EVENTS` - 禁用池子事件的响应行为。
+ `DRAGONECS_ENABLE_DEBUG_SERVICE` - 在发布版中启用 EcsDebug 的工作。
+ `DRAGONECS_STABILITY_MODE` - 默认情况下，为了优化，框架在发布版本中跳过许多异常检查。此定义不是跳过检查，而是将其替换为能够解决错误的代码。这提高了稳定性，但降低了执行速度。
+ `DRAGONECS_DISABLE_CATH_EXCEPTIONS` - 禁用默认的异常处理行为。默认情况下，框架将捕获异常并通过 EcsDebug 输出异常信息，然后继续执行。
+ `REFLECTION_DISABLED` - 完全限制框架内部代码中的 Reflection 使用。
+ `DISABLE_DEBUG` - 用于不支持手动禁用 DEBUG 的环境，例如 Unity。
+ `ENABLE_DUMMY_SPAN` - 如果环境不支持 Span 类型，则启用它的替代。

</br>

# 扩展的功能
为了增强框架的可扩展性，提供了其他工具。

## 配置
`EcsWorld` 和 `EcsPipeline` 类的构造函数可以接受实现 `IConfigContainer` 或 `IConfigContainerWriter` 接口的配置容器。使用这些容器可以传递数据和依赖关系。内置的容器实现是 `ConfigContainer`，但也可以使用自定义的实现。</br>
为世界使用配置容器的示例：
``` c#
var configs = new ConfigContainer()
    .Set(new EcsWorldConfig(entitiesCapacity: 2000, poolsCapacity: 2000)
    .Set(new SomeDataA(/* ... */))
    .Set(new SomeDataB(/* ... */)));
EcsDefaultWorld _world = new EcsDefaultWorld(configs);
// ...
var _someDataA = _world.Configs.Get<SomeDataA>();
var _someDataB = _world.Configs.Get<SomeDataB>();
```
为管线使用配置容器的示例：
``` c#
_pipeline = EcsPipeline.New()// 相当于 _pipeline = EcsPipeline.New(new ConfigContainer())。
    .Configs.Set(new SomeDataA(/* ... */))
    .Configs.Set(new SomeDataB(/* ... */))
    // ...
    .BuildAndInit();
// ...
var _someDataA = _pipeline.Configs.Get<SomeDataA>();
var _someDataB = _pipeline.Configs.Get<SomeDataB>();
```

## 世界组件
使用世界组件可以将额外的数据附加到世界上. 世界组件使用 `struct` 类型来实现。访问组件的 `Get` 方法经过了速度优化，速度几乎与访问类字段相同。

``` c#
// 获取组件。
ref WorldComponent component = ref _world.Get<WorldComponent>();
```
世界组件实现:
``` c#
public struct WorldComponent
{
    // 数据。
}
```
或者:
``` c#
public struct WorldComponent : IEcsWorldComponent<WorldComponent>
{
    // 数据。

    // 接口的初始化方法。
    void IEcsWorldComponent<WorldComponent>.Init(ref WorldComponent component, EcsWorld world)
    {
        // 初始化组件时执行的操作。在从 EcsWorld.Get 返回之前调用。
    }
    // 接口的销毁方法。
    void IEcsWorldComponent<WorldComponent>.OnDestroy(ref WorldComponent component, EcsWorld world)
    {
        // 在调用 EcsWorld.Destroy 时执行的操作。
        // 调用 OnDestroy 要求用户手动将组件重置为默认状态，如果需要的话。
        component = default;
    }
}
```

<details>
<summary>使用示例</summary>

`IEcsWorldComponent<T>` 接口的事件可用于自动初始化组件字段和释放资源。
``` c#
public struct WorldComponent : IEcsWorldComponent<WorldComponent>
{
    private SomeClass _object; // 被回收的对象。
    private SomeReusedClass _reusedObject; // 被重复使用的对象。
    public SomeClass Object => _object;
    public SomeReusedClass ReusedObject => _reusedObject;
    void IEcsWorldComponent<WorldComponent>.Init(ref WorldComponent component, EcsWorld world)
    {
        if (component._reusedObject == null)
            component._reusedObject = new SomeReusedClass();
        component._object = new SomeClass();
        // 当通过 EcsWorld.Get 获取组件时，_reusedObject 和 _object 已经被创建。
    }
    void IEcsWorldComponent<WorldComponent>.OnDestroy(ref WorldComponent component, EcsWorld world)
    {
        // 处理不再需要的对象，并释放对它的引用，以便让 GC 回收它。
        component._object.Dispose();
        component._object = null;
        
        // 如果需要的话，可以重置可重复使用对象的值。
        // component._reusedObject.Reset();
        
        // 因为在这个示例中不需要完全重置组件，所以下面这行不需要。
        // component = default;
    }
}
```
</details>

> 世界组件和配置容器可以与扩展方法结合使用，用于创建扩展。

</br>

# 使用DragonECS的项目
## With sources:

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/DCFApixels/3D-Platformer-DragonECS-Demo">
        3D Platformer (Example)
        <img src="https://github.com/user-attachments/assets/c593dba7-eeaa-4706-a043-946f132f3f83" alt="screenshot">
      </a> 
    </td>
    <td align="center">
      <a href="https://github.com/DCFApixels/LD_56_Tiny_Aliens">
        Tiny Aliens (Ludum Dare 56)
        <img src="https://github.com/user-attachments/assets/1a8f06ed-c68d-483a-b880-c9faaf7e0b5f" alt="screenshot">
      </a>
    </td>
  </tr>
</table>

## Released games:

<table>
  <tr>
    <td align="center">
      <a href="https://yandex.ru/games/app/206024?utm_source=game_popup_menu">
        Башенки Смерти
        <img src="https://github.com/user-attachments/assets/70fc55a0-c911-49f8-ba75-f503437f087f" alt="screenshot">
      </a> 
    </td>
    <td align="center">
      <span>
        ㅤㅤㅤㅤㅤㅤㅤㅤㅤㅤㅤ
        <img tabindex="-1" src="https://github.com/user-attachments/assets/3fa1ca6d-29f6-43e6-aafe-cc9648d20490" alt="screenshot">
      </span> 
    </td>
  </tr>
</table>

</br>

# 扩展
* Packages:
    * [Unity集成](https://github.com/DCFApixels/DragonECS-Unity)
    * [自动依赖注入](https://github.com/DCFApixels/DragonECS-AutoInjections)
    * [经典C#多线程](https://github.com/DCFApixels/DragonECS-ClassicThreads)
    * [Recursivity](https://github.com/DCFApixels/DragonECS-Recursivity)
    * [Hybrid](https://github.com/DCFApixels/DragonECS-Hybrid)
    * [Graphs](https://github.com/DCFApixels/DragonECS-Graphs)
* Utilities:
    * [简单语法](https://gist.github.com/DCFApixels/d7bfbfb8cb70d141deff00be24f28ff0)
    * [单帧组件](https://gist.github.com/DCFApixels/46d512dbcf96c115b94c3af502461f60)
    * [IDE代码模板](https://gist.github.com/ctzcs/0ba948b0e53aa41fe1c87796a401660b) и [Unity代码模板](https://gist.github.com/ctzcs/d4c7730cf6cd984fe6f9e0e3f108a0f1)

> *你的扩展？如果你正在开发 DragonECS 的扩展，可以[在此处发布](#反馈).

</br>
 
# FAQ
## 'ReadOnlySpan<>' could not be found
在Unity 2020.1.x版本中，控制台可能会出现以下错误：
```
The type or namespace name 'ReadOnlySpan<>' could not be found (are you missing a using directive or an assembly reference?)
``` 
要解决这个问题，需要在`Project Settings/Player/Other Settings/Scripting Define Symbols`中添加`ENABLE_DUMMY_SPAN`指令.
</br>

# 反馈
+ Discord (RU-EN) [https://discord.gg/kqmJjExuCf](https://discord.gg/kqmJjExuCf)
+ QQ (中文) [949562781](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781)

</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
<img width="0" src="https://github.com/user-attachments/assets/8e598a9a-826c-4a1f-b842-0c56301d2927"><!--Чтоб флаг подгружался в любом случае-->
