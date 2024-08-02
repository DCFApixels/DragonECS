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
        <img src="https://github.com/user-attachments/assets/30528cb5-f38e-49f0-b23e-d001844ae930"></br>
        <span>English(WIP)</span>
      </a>  
    </td>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS/blob/main/README-ZN.md">
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
    // Add System1 to the systems queue.
    .Add(new System1())
    // Add System2 to the queue after System1.
    .Add(new System2())
    // Add System3 to the queue after System2, as a unique instance.
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
> For simultaneous building and initialization, there is the method  `Builder.BuildAndInit();`

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

## Процессы
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

## Пул
Является хранилищем для компонентов, предоставляет методы для добавления/чтения/редактирования/удаления компонентов на сущности. Есть несколько видов пулов, для разных целей:
* `EcsPool` - универсальный пул, хранит struct-компоненты реализующие интерфейс `IEcsComponent`;
* `EcsTagPool` - специальный пул для пустых компонентов-тегов, хранит struct-компоненты с `IEcsTagComponent` как bool значения, что в сравнении с реализацией `EcsPool` имеет лучше оптимизацию памяти и скорости;

Пулы имеют 5 основных метода и их разновидности:
``` c#
// Один из способов получить пул из мира.
EcsPool<Pose> poses = _world.GetPool<Pose>();
 
// Добавит компонент на сущность, бросит исключение если компонент уже есть у сущности.
ref var addedPose = ref poses.Add(entityID);
 
// Вернет компонент, бросит исключение если у сущности нет этого компонента. 
ref var gettedPose = ref poses.Get(entityID);
 
// Вернет компонент доступный только для чтения, бросит исключение если у сущности нет этого компонента. 
ref readonly var readonlyPose = ref poses.Read(entityID);
 
// Вернет true если у сущности есть компонент, в противном случае false.
if (poses.Has(entityID)) { /* ... */ }
 
// Удалит компонент у сущности, бросит исключение если у сущности нет этого компонента.
poses.Del(entityID);
```
> Есть "безопасные" методы, которые сначала выполнят проверку наличия/отсутствия компонента, названия таких методов начинаются с `Try`.
    
> Имеется возможность реализации пользовательского пула. Эта функция будет описана в ближайшее время.
 
## Аспект
Это пользовательские классы наследуемые от `EcsAspect` и используемые для взаимодействия с сущностями. Аспекты одновременно являются кешем пулов и маской компонентов для фильтрации сущностей. Можно рассматривать аспекты как описание того с какими сущностями работает система.

Упрощенный синтаксис:
``` c#
using DCFApixels.DragonECS;
// ...
class Aspect : EcsAspect
{
    // Кешируется пул и Pose добавляется во включающее ограничение.
    public EcsPool<Pose> poses = Inc;
    // Кешируется пул и Velocity добавляется во включающее ограничение.
    public EcsPool<Velocity> velocities = Inc;
    // Кешируется пул и FreezedTag добавляется в исключающее ограничение.
    public EcsTagPool<FreezedTag> freezedTags = Exc;

    // При запросах будет проверяться наличие компонентов
    // из включающего ограничения маски и отсутствие из исключающего.
    // Так же есть Opt - только кеширует пул, не влияя на маску. 
}
```

Явный синтаксис (результат идентичен примеру выше):
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
<summary>Комбинирование аспектов</summary>

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

## Запросы
Что бы получить необходимый набор сущностей используется метод-запрос `EcsWorld.Where<TAspect>(out TAspect aspect)`. В качестве `TAspect` указывается аспект, сущности будут отфильтрованны по маске указанного аспекта. Запрос `Where` применим как к `EcsWorld` так и коллекциям фреймворка (в этом плане Where чем-то похож на аналогичный из Linq).
Пример:
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
 
## Коллекции

### EcsSpan
Коллекция сущностей, доступная только для чтения и выделяемая только в стеке. Состоит из ссылки на массив, длинны и идентификатора мира. Аналог `ReadOnlySpan<int>`.
``` c#
// Запрос Where возвращает сущности в виде EcsSpan.
EcsSpan es = _world.Where(out Aspect a);
// Итерироваться можно по foreach и for.
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
> Хотя `EcsSpan` является просто массивом, в нем не допускается дублирование сущностей. 

### EcsGroup
Вспомогательная коллекция основанная на Sparse Set для хранения множества сущностей с O(1) операциями добавления/удаления/проверки и т.д.
``` c#
// Получаем новую группу. EcsWorld содержит в себе пул групп,
// поэтому будет создана новая или переиспользована свободная.
EcsGroup group = EcsGroup.New(_world);
// Освобождаем группу.
group.Dispose();
```
``` c#
// Добавляем сущность entityID.
group.Add(entityID);
// Проверяем наличие сущности entityID.
group.Has(entityID);
// Удаляем сущность entityID.
group.Remove(entityID);
```
``` c#
// Запрос WhereToGroup возвращает сущности в виде группы только для чтения EcsReadonlyGroup.
EcsReadonlyGroup group = _world.WhereToGroup(out Aspect a);
// Итерироваться можно по foreach и for.
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
Так как группы это множества, они содержат методы аналогичные `ISet<T>`. Редактирующие методы имеет 2 варианта, с записью результата в groupA, либо с возвращением новой группы:            
                                
``` c#
// Объединение groupA и groupB.
groupA.UnionWith(groupB);
EcsGroup newGroup = EcsGroup.Union(groupA, groupB);

// Пересечение groupA и groupB.
groupA.IntersectWith(groupB);
EcsGroup newGroup = EcsGroup.Intersect(groupA, groupB);

// Разность groupA и groupB.
groupA.ExceptWith(groupB);
EcsGroup newGroup = EcsGroup.Except(groupA, groupB);

// Симметрическая разность groupA и groupB.
groupA.SymmetricExceptWith(groupB);
EcsGroup newGroup = EcsGroup.SymmetricExcept(groupA, groupB);

// Разница всех сущностей в мире и groupA.
groupA.Inverse();
EcsGroup newGroup = EcsGroup.Inverse(groupA);
```

## Корень ECS
Это пользовательский класс который является точкой входа для ECS. Основное назначение инициализация, запуск систем на каждый Update движка и освобождение ресурсов по окончанию использования.
### Пример для Unity
``` c#
using DCFApixels.DragonECS;
using UnityEngine;
public class EcsRoot : MonoBehaviour
{
    private EcsPipeline _pipeline;
    private EcsDefaultWorld _world;
    private void Start()
    {
        // Создание мира для сущностей и компонентов.
        _world = new EcsDefaultWorld();
        // Создание пайплайна для систем.
        _pipeline = EcsPipeline.New()
            // Добавление систем.
            // .Add(new SomeSystem1())
            // .Add(new SomeSystem2())
            // .Add(new SomeSystem3())

            // Внедрение мира в системы.
            .Inject(_world)
            // Прочие внедрения.
            // .Inject(SomeData)

            // Завершение построения пайплайна.
            .Build();
        // Инициализация пайплайна и запуск IEcsPreInit.PreInit()
        // и IEcsInit.Init() у всех добавленных систем.
        _pipeline.Init();
    }
    private void Update()
    {
        // Запуск IEcsRun.Run() у всех добавленных систем.
        _pipeline.Run();
    }
    private void OnDestroy()
    {
        // Запускает IEcsDestroy.Destroy() у всех добавленных систем.
        _pipeline.Destroy();
        _pipeline = null;
        // Обязательно удалять миры которые больше не будут использованы.
        _world.Destroy();
        _world = null;
    }
}
```
### Общий пример
``` c#
using DCFApixels.DragonECS;
public class EcsRoot
{
    private EcsPipeline _pipeline;
    private EcsDefaultWorld _world;
    // Инициализация окружения.
    public void Init()
    {
        //Создание мира для сущностей и компонентов.
        _world = new EcsDefaultWorld();
        //Создание пайплайна для систем.
        _pipeline = EcsPipeline.New()
            // Добавление систем.
            // .Add(new SomeSystem1())
            // .Add(new SomeSystem2())
            // .Add(new SomeSystem3())

            // Внедрение мира в системы.
            .Inject(_world)
            // Прочие внедрения.
            // .Inject(SomeData)

            // Завершение построения пайплайна.
            .Build();
        // Инициализация пайплайна и запуск IEcsPreInit.PreInit()
        // и IEcsInit.Init() у всех добавленных систем.
        _pipeline.Init();
    }

    // Update-цикл движка.
    public void Update()
    {
        // Запуск IEcsRun.Run() у всех добавленных систем.
        _pipeline.Run();
    }

    // Очистка окружения.
    public void Destroy()
    {
        // Запускает IEcsDestroy.Destroy() у всех добавленных систем.
        _pipeline.Destroy();
        _pipeline = null;
        // Обязательно удалять миры которые больше не будут использованы.
        _world.Destroy();
        _world = null;
    }
}
```

</br>

# Debug
Фреймворк предоставляет дополнительные инструменты для отладки и логирования, не зависящие от среды. Так же многие типы имеют свой DebuggerProxy для более информативного отображения в IDE.
## Мета-Атрибуты
По умолчанию мета-атрибуты не имеют применения, но используются в интеграциях с движками для задания отображения в отладочных инструментах и редакторах. А так же могут быть использованы для генерации автоматической документации.
``` c#
using DCFApixels.DragonECS;

// Задает пользовательское название типа, по умолчанию используется имя типа.
[MetaName("SomeComponent")]

// Используется для группировки типов.
[MetaGroup("Abilities/Passive/")] // or [MetaGroup("Abilities", "Passive")]

// Задает цвет типа в RGB кодировке, где каждый канал принимает значение от 0 до 255, по умолчанию белый. 
[MetaColor(MetaColor.Red)] // or [MetaColor(255, 0, 0)]
 
// Добавляет описание типу.
[MetaDescription("The quick brown fox jumps over the lazy dog")] 
 
// Добавляет строковые теги.
[MetaTags("Tag1", "Tag2", ...)]  // [MetaTags(MetaTags.HIDDEN))] чтобы скрыть в редакторе 
public struct Component : IEcsComponent { /* ... */ }
```
Получение мета-информации:
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
Имеет набор методов для отладки и логирования. Реализован как статический класс вызывающий методы Debug-сервисов. Debug-сервисы - это посредники между системами отладки среды и EcsDebug. Это позволяет не изменяя отладочный код проекта, переносить проект на другие движки, достаточно только реализовать соответствующий Debug-сервис.

По умолчанию используется `DefaultDebugService` который выводит логи в консоль. Для реализации пользовательского создайте класс наследуемый от `DebugService` и реализуйте абстрактные члены класса.

``` c#
// Вывод лога.
EcsDebug.Print("Message");

// Вывод лога с тегом.
EcsDebug.Print("Tag", "Message");

// Прерывание игры.
EcsDebug.Break();

// Установка другого Debug-Сервиса.
EcsDebug.Set<OtherDebugService>();
```

## Профилирование
``` c#
// Создание маркера с именем SomeMarker.
private static readonly EcsProfilerMarker marker = new EcsProfilerMarker("SomeMarker");

...

marker.Begin();
// Код для которого замеряется скорость.
marker.End();

// or

using (marker.Auto())
{
    // Код для которого замеряется скорость.
}
```

</br>

# Define Symbols
+ `DISABLE_POOLS_EVENTS` - выключает реактивное поведение в пулах.
+ `ENABLE_DRAGONECS_DEBUGGER` - включает работу EcsDebug в релизном билде.
+ `ENABLE_DRAGONECS_ASSERT_CHECKS` - включает опускаемые в релизном билде проверки.
+ `REFLECTION_DISABLED` - Полностью ограничивает работу фреймворка с Reflection.
+ `DISABLE_DEBUG` - Для среды где не поддерживается ручное отключение DEBUG, например Unity.
+ `ENABLE_DUMMY_SPAN` - На случай если в среде не поддерживаются Span типы, включает его замену.
+ `DISABLE_CATH_EXCEPTIONS` - Выключает поведение по умолчанию по обработке исключений. По умолчанию фреймворк будет ловить исключения с выводом информации из исключений через EcsDebug и продолжать работу.

</br>

# Расширение фреймворка
Для большей расширяемости фреймворка есть дополнительные инструменты.

## Конфиги
Конструкторы классов `EcsWorld` и `EcsPipeline` могут принимать контейнеры конфигов реализующие интерфейс `IConfigContainer` или `IConfigContainerWriter`. С помощью этих контейнеров можно передавать данные и зависимости. Встроенная реализация контейнера - `ConfigContainer`, но можно так же использовать свою реализацию.</br>
Пример использования конфигов для мира:
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
Пример использования конфигов для пайплайна:
``` c#
_pipeline = EcsPipeline.New()// аналогично _pipeline = EcsPipeline.New(new ConfigContainer())
    .Configs.Set(new SomeDataA(/* ... */))
    .Configs.Set(new SomeDataB(/* ... */))
    // ...
    .BuildAndInit();
// ...
var _someDataA = _pipeline.Configs.Get<SomeDataA>();
var _someDataB = _pipeline.Configs.Get<SomeDataB>();
```

## Компоненты Мира
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

> Компоненты и конфиги можно применять для создания расширений в связке с методами расширений.

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
