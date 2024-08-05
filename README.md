<p align="center">
<img width="660" src="https://github.com/DCFApixels/DragonECS/assets/99481254/c09e385e-08c1-4c04-904a-36ad7e25e45b">
</p>

<!--<a href="https://discord.gg/kqmJjExuCf"><img alt="Discord" src="https://img.shields.io/discord/1111696966208999525?color=%2300b269&label=Discord&logo=Discord&logoColor=%23ffffff&style=for-the-badge"></a>-->

<p align="center">
<img alt="Version" src="https://img.shields.io/github/package-json/v/DCFApixels/DragonECS?color=%23ff4e85&style=for-the-badge">
<img alt="License" src="https://img.shields.io/github/license/DCFApixels/DragonECS?color=ff4e85&style=for-the-badge">
<a href="https://discord.gg/kqmJjExuCf"><img alt="Discord" src="https://img.shields.io/badge/Discord-JOIN-00b269?logo=discord&logoColor=%23ffffff&style=for-the-badge"></a>
<a href="http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781"><img alt="QQ" src="https://img.shields.io/badge/QQ-JOIN-00b269?logo=tencentqq&logoColor=%23ffffff&style=for-the-badge"></a>
</p>

# DragonECS - C# Entity Component System Framework

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
        <img src="https://github.com/user-attachments/assets/3c699094-f8e6-471d-a7c1-6d2e9530e721"></br>
        <span>English(WIP)</span>
      </a>  
    </td>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS/blob/main/README-ZH.md">
        <img src="https://github.com/user-attachments/assets/8e598a9a-826c-4a1f-b842-0c56301d2927"></br>
        <span>中文</span>
      </a>  
    </td>
  </tr>
</table>

</br>

The [ECS](https://en.wikipedia.org/wiki/Entity_component_system) Framework aims to maximize usability, modularity, extensibility and performance of dynamic entity changes. Without code generation and dependencies. Inspired by [LeoEcs](https://github.com/Leopotam/ecslite). 

> [!WARNING]
> The project is a work in progress, API may change.
> 
> While the English version of the README is incomplete, you can view the [Russian version](https://github.com/DCFApixels/DragonECS/blob/main/README-RU.md).

## Оглавление
- [Installation](#Installation)
- [Basic Concepts](#Basic-Concepts)
  - [Entity](#entity)
  - [Component](#component)
  - [System](#system)
- [Framework Concepts](#Framework-Concepts)
  - [Pipeline](#Pipeline)
    - [Building](#Building)
    - [Dependency Injection](#Dependency-Injection)
    - [Modules](#Modules)
    - [Layers](#Layers)
  - [Processes](#Processes)
  - [World](#World)
  - [Pool](#Pool)
  - [Aspect](#Aspect)
  - [Queries](#Queries)
  - [Collections](#Collections)
  - [ECS Root](#ecs-root)
- [Debug](#debug)
  - [Meta Attributes](#Meta-Attributes)
  - [EcsDebug](#ecsdebug)
  - [Profiling](#Profiling)
- [Define Symbols](#define-symbols)
- [Framework Extension Tools](#Framework-Extension-tools)
  - [World Components](#World-Components)
  - [Configs](#Configs)
- [Projects powered by DragonECS](#Projects-powered-by-DragonECS)
- [Extensions](#Extensions)
- [FAQ](#faq)
- [Feedback](#Feedback)

</br>

# Installation
Versioning semantics - [Open](https://gist.github.com/DCFApixels/e53281d4628b19fe5278f3e77a7da9e8#file-dcfapixels_versioning_ru-md)
## Environment
Requirements:
+ Minimum version of C# 7.3;
  
Optional:
+ Support for NativeAOT
+ Game engines with C#: Unity, Godot, MonoGame, etc.
  
Tested with:
+ **Unity:** Minimum version 2020.1.0;

## Unity Installation
* ### Unity Package
The framework can be installed as a Unity package by adding the Git URL [in the PackageManager](https://docs.unity3d.com/2023.2/Documentation/Manual/upm-ui-giturl.html) or manually adding it to `Packages/manifest.json`: 
```
https://github.com/DCFApixels/DragonECS.git
```
* ### Source Code
The framework can also be added to the project as source code.

</br>

# Basic Concepts
## Entity
Сontainer for components. They are implemented as identifiers, of which there are two types:
* `int` - a short-term identifier used within a single tick. Storing `int` identifiers is not recommended, use `entlong` instead;
* `entlong` - long-term identifier, contains a full set of information for unique identification;
``` c#
// Creating a new entity in the world.
int entityID = _world.NewEntity();

// Deleting an entity.
_world.DelEntity(entityID);

// Copying components from one entity to another.
_world.CopyEntity(entityID, otherEntityID);

// Cloning an entity.
int newEntityID = _world.CloneEntity(entityID);
```

<details>
<summary>Working with entlong</summary>
 
``` c#
// Convert int to entlong.
entlong entity = _world.GetEntityLong(entityID);
// or
entlong entity = (_world, entityID);

// Check that the entity is still alive.
if (entity.IsAlive) { }

// Converting entlong to int. Throws an exception if the entity no longer exists.
int entityID = entity.ID;
// or
var (entityID, world) = entity;
 
// Converting entlong to int. Returns true and the int identifier if the entity is still alive.
if (entity.TryGetID(out int entityID)) { }
```
 
 </details>
 
> **NOTICE:** Entities cannot exist without components, empty entities will be automatically deleted immediately after the last component is deleted.

## Component
Data for entities. Must implement the ``IEcsComponent`` interface or other specifying type of component. 
```c#
struct Health : IEcsComponent
{
    public float health;
    public int armor;
}
struct PlayerTag : IEcsTagComponent {}
```
Built-in component types:
* `IEcsComponent` - Components with data. Universal component type.
* `IEcsTagComponent` - Tag components. Components without data.

## System
Represent the core logic defining entity behaviors. They are implemented as user-defined classes that implement at least one of the process interfaces. Key processes include:
```c#
class SomeSystem : IEcsPreInit, IEcsInit, IEcsRun, IEcsDestroy
{
    // Called once during EcsPipeline.Init() and before IEcsInit.Init().
    public void PreInit () { }
    
    // Called once during EcsPipeline.Init() and after IEcsPreInit.PreInit().
    public void Init ()  { }
    
    // Called each time during EcsPipeline.Run().
    public void Run () { }
    
    // Called once during EcsPipeline.Destroy().
    public void Destroy () { }
}
```
> For implementing additional processes, refer to the [Processes](#Processes) section.

</br>

# Framework Concepts
## Pipeline
Container and engine of systems. Responsible for setting up the system call queue, provides mechanisms for communication between systems, and dependency injection. Implemented as the `EcsPipeline` class.
### Building
Builder is responsible for building the pipeline. Systems are added to the Builder and at the end, the pipeline is built. Example:
```c#
EcsPipeline pipeline = EcsPipeline.New() // Создает Builder пайплайна.
    // Adds System1 to the systems queue.
    .Add(new System1())
    // Adds System2 to the queue after System1.
    .Add(new System2())
    // Adds System3 to the queue after System2, as a unique instance.
    .AddUnique(new System3())
    // Completes the pipeline building and returns its instance.
    .Build(); 
pipeline.Init(); // Initializes the pipeline.
```

```c#
class SomeSystem : IEcsRun, IEcsPipelineMember
{
    // Gets the pipeline instance to which the system belongs.
    public EcsPipeline Pipeline { get ; set; }

    public void Run () { }
}
```
> For simultaneous building and initialization, there is the method `Builder.BuildAndInit();`

### Dependency Injection
The framework implements dependency injection for systems. This process begins during pipeline initialization  and injects data passed to the Builder.
> Using built-in dependency injection is optional. 
``` c#
class SomeDataA { /* ... */ }
class SomeDataB : SomeDataA { /* ... */ }

// ...
SomeDataB _someDataB = new SomeDataB();
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    // Injects _someDataB into systems implementing IEcsInject<SomeDataB>.
    .Inject(_someDataB) 
    // Adds systems implementing IEcsInject<SomeDataA> to the injection tree,
    // now these systems will also receive _someDataB.
    .Injector.AddNode<SomeDataA>()
    // ...
    .Add(new SomeSystem())
    // ...
    .BuildAndInit();

// ...
// Injection uses the interface IEcsInject<T> and its method Inject(T obj).
class SomeSystem : IEcsInject<SomeDataA>, IEcsRun
{
    SomeDataA _someDataA
    // obj will be an instance of type SomeDataB.
    public void Inject(SomeDataA obj) => _someDataA = obj;

    public void Run () 
    {
        _someDataA.DoSomething();
    }
}
```

### Modules
Groups of systems that implement a common feature can be grouped into modules and easily added to the Pipeline.
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

### Layers
Queues in the system can be segmented into layers. A layer defines a position in the queue for inserting systems.  For example, if a system needs to be inserted at the end of the queue regardless of where it is added, you can add this system to the `EcsConsts.END_LAYER` layer.
``` c#
const string SOME_LAYER = nameof(SOME_LAYER);
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    // Inserts a new layer before the end layer EcsConsts.END_LAYER
    .Layers.Insert(EcsConsts.END_LAYER, SOME_LAYER)
    // System SomeSystem will be added to the SOME_LAYER layer
    .Add(New SomeSystem(), SOME_LAYER) 
    // ...
    .BuildAndInit();
```
The built-in layers are arranged in the following order:
* `EcsConst.PRE_BEGIN_LAYER`
* `EcsConst.BEGIN_LAYER`
* `EcsConst.BASIC_LAYER` (Systems are added here if no layer is specified during addition)
* `EcsConst.END_LAYER`
* `EcsConst.POST_END_LAYER`

## Processes
Processes are queues of systems that implement a common interface, such as `IEcsRun`. Runners are used to start processes. Built-in processes are started automatically. It is possible to implement custom processes.

<details>
<summary>Built-in processes</summary>
 
* `IEcsPreInit`, `IEcsInit`, `IEcsRun`, `IEcsDestroy` - lifecycle processes of `EcsPipeline`.
* `IEcsInject<T>` - [Dependency Injection](#Dependency-Injection) processes.
* `IOnInitInjectionComplete` - Similar to the [Dependency Injection](#Dependency-Injection) process, but signals the completion of initialization injection.

</details>
 
<details>
<summary>Custom Processes</summary>
 
Для добавления нового процесса создайте интерфейс наследованный от `IEcsProcess` и создайте раннер для него. Раннер это класс реализующий интерфейс запускаемого процесса и наследуемый от `EcsRunner<TInterface>`. Пример:
``` c#
// Интерфейс.
interface IDoSomethingProcess : IEcsProcess
{
    void Do();
}
// Реализация раннера. Пример реализации можно так же посмотреть в встроенных процессах 
sealed class DoSomethingProcessRunner : EcsRunner<IDoSomethingProcess>, IDoSomethingProcess
{
    public void Do() 
    {
        foreach (var item in Process) item.Do();
    }
}
// ...

// Добавление раннера при создании пайплайна.
_pipeline = EcsPipeline.New()
    //...
    .AddRunner<DoSomethingProcessRunner>()
    //...
    .BuildAndInit();

// Запуск раннера если раннер был добавлен.
_pipeline.GetRunner<IDoSomethingProcess>.Do()

// or если раннер не был добавлен(Вызов GetRunnerInstance так же добавит раннер в пайплайн).
_pipeline.GetRunnerInstance<DoSomethingProcessRunner>.Do()
```
> Раннеры имеют ряд требований к реализации: 
> * Наследоваться от `EcsRunner<T>` можно только напрямую;
> * Раннер может содержать только один интерфейс(за исключением `IEcsProcess`);
> * Наследуемый класс `EcsRunner<T>,` должен так же реализовать интерфейс `T`;
    
> Не рекомендуется в цикле вызывать `GetRunner`, иначе кешируйте полученный раннер.
</details>

## World
Is a container for entities and components.
``` c#
// Creating an instance of the world.
_world = new EcsDefaultWorld();
// Creating and deleting an entity as shown in the Entities section.
var e = _world.NewEntity();
_world.DelEntity(e);
```
> **NOTICE:** It's necessary to call EcsWorld.Destroy() on the world instance when it's no longer needed, otherwise it will remain in memory.

### World Configuration
To initialize the world with a required size upfront and reduce warm-up time, you can pass an `EcsWorldConfig` instance to the constructor.

``` c#
EcsWorldConfig config = new EcsWorldConfig(
    // Pre-initializes the world capacity for 2000 entities.
    entitiesCapacity: 2000, 
    // Pre-initializes the pools capacity for 2000 components.
    poolComponentsCapacity: 2000);  
_world = new EcsDefaultWorld(config);
```

## Pool
Stash of components, providing methods for adding, reading, editing, and removing components on entities. There are several types of pools designed for different purposes:
* `EcsPool` - universal pool, stores struct components implementing the `IEcsComponent` interface;
* `EcsTagPool` - special pool for empty tag components, stores struct-components with `IEcsTagComponent` as bool values, which in comparison with `EcsPool` implementation has better memory and speed optimization;

Pools have 5 main methods and their variations:
``` c#
// One way to get a pool from the world.
EcsPool<Pose> poses = _world.GetPool<Pose>();
 
// Adds component to entity, throws an exception if the entity already has the component.
ref var addedPose = ref poses.Add(entityID);
 
// Returns exist component, throws an exception if the entity does not have this component.
ref var gettedPose = ref poses.Get(entityID);
 
// Returns a read-only component, throwing an exception if the entity does not have this component. 
ref readonly var readonlyPose = ref poses.Read(entityID);
 
// Returns true if the entity has the component, otherwise false.
if (poses.Has(entityID)) { /* ... */ }
 
// Removes component from entity, throws an exception if the entity does not have this component.
poses.Del(entityID);
```
> There are "safe" methods that first perform a check for the presence or absence of a component. Such methods are prefixed with `Try`.
    
> It is possible to implement a user pool. This feature will be described shortly.
 
## Аспект
These are custom classes inherited from `EcsAspect` and used to interact with entities. Aspects are both a pool cache and a component mask for filtering entities. You can think of aspects as a description of what entities the system is working with.

Simplified syntax:
``` c#
using DCFApixels.DragonECS;
// ...
class Aspect : EcsAspect
{
    // Caches the Pose pool and adds it to the inclusive constraint.
    public EcsPool<Pose> poses = Inc;
    // Caches the Velocity pool and adds it to the inclusive constraint.
    public EcsPool<Velocity> velocities = Inc;
    // Caches the FreezedTag pool and adds it to the exclusive constraint.
    public EcsTagPool<FreezedTag> freezedTags = Exc;

    // During queries, it checks for the presence of components
    // in the inclusive constraint and absence in the exclusive constraint.
    // There is also Opt - it only caches the pool without affecting the mask.
}
```

Explicit syntax (the result is identical to the example above):
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
<summary>Combining aspects</summary>

В аспекты можно добавлять другие аспекты, тем самым комбинируя их. Ограничения так же будут скомбинированы.
``` c#
using DCFApixels.DragonECS;
// ...
class Aspect : EcsAspect
{
    public OtherAspect1 otherAspect1;
    public OtherAspect2 otherAspect2;
    public EcsPool<Pose> poses;
 
    // Функция Init аналогична конструктору Aspect(Builder b).
    protected override void Init(Builder b)
    {
        // Комбинирует с SomeAspect1.
        otherAspect1 = b.Combine<OtherAspect1>(1);
        // Хотя для OtherAspect1 метод Combine был вызван раньше, сначала будет скомбинирован с OtherAspect2, так как по умолчанию order = 0.
        otherAspect2 = b.Combine<OtherAspect2>();
        // Если в OtherAspect1 или в OtherAspect2 было ограничение b.Exclude<Pose>() тут оно будет заменено на b.Include<Pose>().
        poses = b.Include<Pose>();
    }
}
```
Если будут конфликтующие ограничения у комбинируемых аспектов, то новые ограничения будут заменять добавленные ранее. Ограничения корневого аспекта всегда заменяют ограничения из добавленных аспектов. Визуальный пример комбинации ограничений:
| | cmp1 | cmp2 | cmp3 | cmp4 | cmp5 | разрешение конфликтных ограничений|
| :--- | :--- | :--- | :--- | :--- | :--- |:--- |
| OtherAspect2 | :heavy_check_mark: | :x: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | |
| OtherAspect1 | :heavy_minus_sign: | :heavy_check_mark: | :heavy_minus_sign: | :x: | :heavy_minus_sign: | Для `cmp2` будет выбрано :heavy_check_mark: |
| Aspect | :x: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | Для `cmp1` будет выбрано :x: |
| Итоговые ограничения | :x: | :heavy_check_mark: | :heavy_minus_sign: | :x: | :heavy_check_mark: | |

</details>

## Queries
To get the set of required entities, there is a query method `EcsWorld.Where<TAspect>(out TAspect aspect)`. Aspect is specified as `TAspect`, the entities will be filtered by the mask of the specified aspect. The `Where` query is applicable to both `EcsWorld` and framework collections (in this respect, Where is somewhat similar to a similar one from Linq).
Example:
``` c#
public class SomeDamageSystem : IEcsRun, IEcsInject<EcsDefaultWorld>
{
    class Aspect : EcsAspect
    {
        public EcsPool<Health> healths = Inc; 
        public EcsPool<DamageSignal> damageSignals = Inc; 
        public EcsTagPool<IsInvulnerable> isInvulnerables = Exc;
    }
    EcsDefaultWorld _world;
    public void Inject(EcsDefaultWorld world) => _world = world;

    public void Run()
    {
        foreach (var e in _world.Where(out Aspect a))
        {
            // Сюда попадают сущности с компонентами Health, DamageSignal и без IsInvulnerable.
            a.healths.Get(e).points -= a.damageSignals.Get(e).points;
        }
    }
}
```
 
## Collections

### EcsSpan
Collection of entities that is read-only and stack-allocated. It consists of a reference to an array, its length, and the world identifier. Similar to `ReadOnlySpan<int>`.
``` c#
// Where query returns entities as EcsSpan.
EcsSpan es = _world.Where(out Aspect a);
// Iteration is possible using foreach and for loops.
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
> Although `EcsSpan` is just an array, it does not allow duplicate entities. 

### EcsGroup
Sparse Set based auxiliary collection for storing a set of entities with O(1) add/delete/check operations, etc.
``` c#
// Getting a new group. EcsWorld contains pool of groups,
// so a new one will be created or a free one will be reused.
EcsGroup group = EcsGroup.New(_world);
// Release the group.
group.Dispose();
```
``` c#
// Add entityID to the group.
group.Add(entityID);
// Check if entityID exists in the group.
group.Has(entityID);
// Remove entityID from the group.
group.Remove(entityID);
```
``` c#
// WhereToGroup query returns entities as a read-only group EcsReadonlyGroup.
EcsReadonlyGroup group = _world.WhereToGroup(out Aspect a);
// Iteration is possible using foreach and for loops.
foreach (var e in group) 
{ 
    // ...
}
for (int i = 0; i < group.Count; i++)
{
    int e = group[i];
    // ...
}
```
Since groups are sets, they have methods similar to `ISet<T>`. Editing methods have 2 variants: either they modify `groupA` directly or return a new group:         
                                
``` c#
// Union of groupA and groupB.
groupA.UnionWith(groupB);
EcsGroup newGroup = EcsGroup.Union(groupA, groupB);

// Intersection of groupA and groupB.
groupA.IntersectWith(groupB);
EcsGroup newGroup = EcsGroup.Intersect(groupA, groupB);

// Difference of groupA and groupB.
groupA.ExceptWith(groupB);
EcsGroup newGroup = EcsGroup.Except(groupA, groupB);

// Symmetric difference of groupA and groupB.
groupA.SymmetricExceptWith(groupB);
EcsGroup newGroup = EcsGroup.SymmetricExcept(groupA, groupB);

// Difference of all entities in world and groupA.
groupA.Inverse();
EcsGroup newGroup = EcsGroup.Inverse(groupA);
```

## ECS Root
This is a custom class that is the entry point for ECS. Its main purpose is to initialize, start systems on each engine Update and release resources when no longer needed.
### Example for Unity
``` c#
using DCFApixels.DragonECS;
using UnityEngine;
public class EcsRoot : MonoBehaviour
{
    private EcsPipeline _pipeline;
    private EcsDefaultWorld _world;
    private void Start()
    {
        // Creating world for entities and components.
        _world = new EcsDefaultWorld();
        // Creating pipeline for systems.
        _pipeline = EcsPipeline.New()
            // Adding systems.
            // .Add(new SomeSystem1())
            // .Add(new SomeSystem2())
            // .Add(new SomeSystem3())

            // Injecting world into systems.
            .Inject(_world)
            // Other injections.
            // .Inject(SomeData)

            // Finalizing the pipeline construction.
            .Build();
        // Initialize the Pipeline and run IEcsPreInit.PreInit()
        // and IEcsInit.Init() on all added systems.
        _pipeline.Init();
    }
    private void Update()
    {
        // Invoking IEcsRun.Run() on all added systems.
        _pipeline.Run();
    }
    private void OnDestroy()
    {
        // Invoking IEcsDestroy.Destroy() on all added systems.
        _pipeline.Destroy();
        _pipeline = null;
        // Requires deleting worlds that will no longer be used.
        _world.Destroy();
        _world = null;
    }
}
```
### Generic example
``` c#
using DCFApixels.DragonECS;
public class EcsRoot
{
    private EcsPipeline _pipeline;
    private EcsDefaultWorld _world;
    // Engine initialization .
    public void Init()
    {
        // Creating world for entities and components.
        _world = new EcsDefaultWorld();
        // Creating pipeline for systems.
        _pipeline = EcsPipeline.New()
            // Adding systems.
            // .Add(new SomeSystem1())
            // .Add(new SomeSystem2())
            // .Add(new SomeSystem3())

            // Внедрение мира в системы.
            .Inject(_world)
            // Other injections.
            // .Inject(SomeData)

            // Finalizing the pipeline construction.
            .Build();
        // Initialize the Pipeline and run IEcsPreInit.PreInit()
        // and IEcsInit.Init() on all added systems.
        _pipeline.Init();
    }

    // Engine update loop.
    public void Update()
    {
        // Invoking IEcsRun.Run() on all added systems.
        _pipeline.Run();
    }

    // Engine cleanup.
    public void Destroy()
    {
        // Invoking IEcsDestroy.Destroy() on all added systems.
        _pipeline.Destroy();
        _pipeline = null;
        // Requires deleting worlds that will no longer be used.
        _world.Destroy();
        _world = null;
    }
}
```

</br>

# Debug
The framework provides additional tools for debugging and logging, independent of the environment. Also many types have their own DebuggerProxy for more informative display in IDE.
## Meta Attributes
By default, meta-attributes have no use, but are used in integrations with engines to specify display in debugging tools and editors. And can also be used to generate automatic documentation.
``` c#
using DCFApixels.DragonECS;

// Specifies custom name for the type, defaults to the type name.
[MetaName("SomeComponent")]

// Used for grouping types.
[MetaGroup("Abilities/Passive/")] // or [MetaGroup("Abilities", "Passive")]

// Sets the type color in RGB format, where each channel ranges from 0 to 255; defaults to white.
[MetaColor(MetaColor.Red)] // or [MetaColor(255, 0, 0)]
 
// Adds description to the type.
[MetaDescription("The quick brown fox jumps over the lazy dog")] 
 
// Adds string tags to the type.
[MetaTags("Tag1", "Tag2", ...)]  // [MetaTags(MetaTags.HIDDEN))]  to hide in the editor 
public struct Component : IEcsComponent { /* ... */ }
```
Getting meta-information:
``` c#
TypeMeta typeMeta = someComponent.GetMeta();
// or
TypeMeta typeMeta = pool.ComponentType.ToMeta();

var name = typeMeta.Name;
var color = typeMeta.Color;
var description = typeMeta.Description;
var group = typeMeta.Group;
var tags = typeMeta.Tags;
```

## EcsDebug
Has a set of methods for debugging and logging. It is implemented as a static class calling methods of Debug services. Debug services are intermediaries between the debugging systems of the environment and EcsDebug. This allows projects to be ported to other engines without modifying the debug code, by implementing the corresponding Debug service.

By default, `DefaultDebugService` is used, which outputs logs to the console. To implement a custom one, create a class inherited from `DebugService` and implement abstract class members.

``` c#
// Output log.
EcsDebug.Print("Message");

// Output log with tag.
EcsDebug.Print("Tag", "Message");

// Break execution.
EcsDebug.Break();

// Set another Debug Service.
EcsDebug.Set<OtherDebugService>();
```

## Profiling
``` c#
// Creating a marker named SomeMarker.
private static readonly EcsProfilerMarker marker = new EcsProfilerMarker("SomeMarker");

// ...

marker.Begin();
// Code whose execution time is being measured.
marker.End();

// or

using (marker.Auto())
{
    // Code whose execution time is being measured.
}
```

</br>

# Define Symbols
+ `DISABLE_POOLS_EVENTS` - disables reactive behavior in pools.
+ `ENABLE_DRAGONECS_DEBUGGER` - enables EcsDebug functionality in release builds.
+ `ENABLE_DRAGONECS_ASSERT_CHECKS` - enables omitted checks in the release build.
+ `REFLECTION_DISABLED` - completely restricts the framework's use of Reflection.
+ `DISABLE_DEBUG` - for environments where manual DEBUG disabling is not supported, e.g., Unity.
+ `ENABLE_DUMMY_SPAN` - For environments where Span types are not supported, enables its replacement.
+ `DISABLE_CATH_EXCEPTIONS` - Turns off the default exception handling behavior. By default, the framework will catch exceptions with the exception information output via EcsDebug and continue working.

</br>

# Framework Extension Tools
There are additional tools for greater extensibility of the framework.

## Configs
Constructors of `EcsWorld` and `EcsPipeline` classes can accept config containers implementing `IConfigContainer` or `IConfigContainerWriter` interface. These containers can be used to pass data and dependencies. The built-in container implementation is `ConfigContainer`, but you can also use your own implementation.</br>
Example of using configs for EcsWorld:
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
Example of using configs for EcsPipeline:
``` c#
_pipeline = EcsPipeline.New()// similarly _pipeline = EcsPipeline.New(new ConfigContainer())
    .Configs.Set(new SomeDataA(/* ... */))
    .Configs.Set(new SomeDataB(/* ... */))
    // ...
    .BuildAndInit();
// ...
var _someDataA = _pipeline.Configs.Get<SomeDataA>();
var _someDataB = _pipeline.Configs.Get<SomeDataB>();
```

## World Components
С помощью компонентов можно прикреплять дополнительные данные к мирам. В качестве компонентов используются `struct` типы. Доступ к компонентам через `Get` оптимизирован, скорость почти такая же как доступ к полям класса.
``` c#
// Получить компонент.
ref WorldComponent component = ref _world.Get<WorldComponent>();
```
Реализация компонента:
``` c#
public struct WorldComponent
{
    // Данные.
}
```
Или:
``` c#
public struct WorldComponent : IEcsWorldComponent<WorldComponent>
{
    // Данные.
    void IEcsWorldComponent<WorldComponent>.Init(ref WorldComponent component, EcsWorld world)
    {
        // Действия при инициализации компонента. Вызывается до первого возвращения из EcsWorld.Get .
    }
    void IEcsWorldComponent<WorldComponent>.OnDestroy(ref WorldComponent component, EcsWorld world)
    {
        // Действия когда вызывается EcsWorld.Destroy.
        // Вызов OnDestroy, обязует пользователя вручную обнулять компонент, если это необходимо. 
        component = default;
    }
}
```

<details>
<summary>Пример использования</summary>

События интерфейса IEcsWorldComponent<T>, могут быть использованы для автоматической инициализации полей компонента, и освобождения ресурсов.
``` c#
public struct WorldComponent : IEcsWorldComponent<WorldComponent>
{
    private SomeClass _object; // Объект который будет утилизироваться.
    private SomeReusedClass _reusedObject; // Объект который будет переиспользоваться.
    public SomeClass Object => _object;
    public SomeReusedClass ReusedObject => _reusedObject;
    void IEcsWorldComponent<WorldComponent>.Init(ref WorldComponent component, EcsWorld world)
    {
        if (component._reusedObject == null)
            component._reusedObject = new SomeReusedClass();
        component._object = new SomeClass();
        // Теперь при получении компонента через EcsWorld.Get, _reusedObject и _object уже будут созданы.
    }
    void IEcsWorldComponent<WorldComponent>.OnDestroy(ref WorldComponent component, EcsWorld world)
    {
        // Утилизируем не нужный объект, и освобождаем ссылку на него, чтобы GC мог его собрать.
        component._object.Dispose();
        component._object = null;
        
        // Как вариант тут можно сделать сброс значений у переиспользуемого объекта.
        //component._reusedObject.Reset();
        
        // Так как в этом примере не нужно полное обнуление компонента, то строчка ниже не нужна.
        // component = default;
    }
}
```
</details>

> Components and configs can be used to create extensions in conjunction with extension methods.

</br>

# Projects powered by DragonECS
* [3D Platformer (Example)](https://github.com/DCFApixels/3D-Platformer-DragonECS)

</br>

# Extensions
* [Unity integration](https://github.com/DCFApixels/DragonECS-Unity)
* [Dependency autoinjections](https://github.com/DCFApixels/DragonECS-AutoInjections)
* [Classic C# multithreading](https://github.com/DCFApixels/DragonECS-ClassicThreads)
* [Hybrid](https://github.com/DCFApixels/DragonECS-Hybrid)
* [One-Frame Components](https://gist.github.com/DCFApixels/46d512dbcf96c115b94c3af502461f60)
* [Code Templates for IDE](https://gist.github.com/ctzcs/0ba948b0e53aa41fe1c87796a401660b) and [for  Unity](https://gist.github.com/ctzcs/d4c7730cf6cd984fe6f9e0e3f108a0f1)
* Graphs (Work in progress)
> > *Your extension? If you are developing an extension for Dragoness, you can share it [here](#feedback).

</br>
 
# FAQ
## 'ReadOnlySpan<>' could not be found
В версии Unity 2020.1.х в консоли может выпадать ошибка:
```
The type or namespace name 'ReadOnlySpan<>' could not be found (are you missing a using directive or an assembly reference?)
``` 
Чтобы починить добавьте директиву `ENABLE_DUMMY_SPAN` в `Project Settings/Player/Other Settings/Scripting Define Symbols`.

</br>

# Feedback
+ Discord (RU-EN) [https://discord.gg/kqmJjExuCf](https://discord.gg/kqmJjExuCf)
+ QQ (中文) [949562781](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781)

</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
