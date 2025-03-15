<p align="center">
<img width="660" src="https://github.com/DCFApixels/DragonECS/assets/99481254/c09e385e-08c1-4c04-904a-36ad7e25e45b">
</p>

<p align="center">
<img alt="Version" src="https://img.shields.io/github/package-json/v/DCFApixels/DragonECS?color=%23ff4e85&style=for-the-badge">
<img alt="License" src="https://img.shields.io/github/license/DCFApixels/DragonECS?color=ff4e85&style=for-the-badge">
<a href="https://discord.gg/kqmJjExuCf"><img alt="Discord" src="https://img.shields.io/badge/Discord-JOIN-00b269?logo=discord&logoColor=%23ffffff&style=for-the-badge"></a>
<a href="http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781"><img alt="QQ" src="https://img.shields.io/badge/QQ-JOIN-00b269?logo=tencentqq&logoColor=%23ffffff&style=for-the-badge"></a>
</p>

# DragonECS - C# Entity Component System Фреймворк

<table>
  <tr></tr>
  <tr>
    <td colspan="3">Readme Languages:</td>
  </tr>
  <tr></tr>
  <tr>
    <td nowrap width="100">
      <a href="https://github.com/DCFApixels/DragonECS/blob/main/README-RU.md">
        <img src="https://github.com/user-attachments/assets/3c699094-f8e6-471d-a7c1-6d2e9530e721"></br>
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
        <img src="https://github.com/user-attachments/assets/8e598a9a-826c-4a1f-b842-0c56301d2927"></br>
        <span>中文</span>
      </a>  
    </td>
  </tr>
</table>

</br>

DragonECS - это [ECS](https://en.wikipedia.org/wiki/Entity_component_system) фреймворк нацеленный на максимальную удобность, модульность, расширяемость и производительность динамического изменения сущностей. Разработан на чистом C#, без зависимостей и генерации кода. Вдохновлен [LeoEcs Lite](https://github.com/Leopotam/ecslite).

> [!WARNING]
> Проект предрелизной версии, поэтому API может меняться. В ветке main актуальная и рабочая версия.
>
> Если есть неясные моменты, вопросы можно задать тут [Обратная связь](#обратная-связь)

## Оглавление
- [Установка](#установка)
- [Основные концепции](#основные-концепции)
  - [Entity](#entity)
  - [Component](#component)
  - [System](#system)
- [Концепции фреймворка](#концепции-фреймворка)
  - [Пайплайн](#пайплайн)
    - [Построение](#построение)
    - [Внедрение зависимостей](#внедрение-зависимостей)
    - [Модули](#модули)
    - [Сортировка](#сортировка)
  - [Процессы](#процессы)
  - [Мир](#мир)
  - [Пул](#пул)
  - [Маска](#маска)
  - [Аспект](#аспект)
  - [Запросы](#запросы)
  - [Коллекции](#Коллекции)
  - [Корень ECS](#корень-ecs)
- [Debug](#debug)
  - [Мета-Атрибуты](#мета-атрибуты)
  - [EcsDebug](#ecsdebug)
  - [Профилирование](#профилирование)
- [Define Symbols](#define-symbols)
- [Расширение фреймворка](#расширение-фреймворка)
  - [Компоненты мира](#компоненты-мира)
  - [Конфиги](#конфиги)
- [Проекты на DragonECS](#Проекты-на-DragonECS)
- [Расширения](#расширения)
- [FAQ](#faq)
- [Обратная связь](#обратная-связь)

</br>

# Установка
Семантика версионирования - [Открыть](https://gist.github.com/DCFApixels/e53281d4628b19fe5278f3e77a7da9e8#file-dcfapixels_versioning_ru-md)
## Окружение
Обязательные требования:
+ Минимальная версия C# 7.3;

Опционально:
+ Поддержка NativeAOT
+ Игровые движки с C#: Unity, Godot, MonoGame и т.д.

Протестировано:
+ **Unity:** Минимальная версия 2020.1.0;

## Установка для Unity
> Рекомендуется так же установить расширение [Интеграция с движком Unity](https://github.com/DCFApixels/DragonECS-Unity)
* ### Unity-модуль
Поддерживается установка в виде Unity-модуля в  при помощи добавления git-URL [в PackageManager](https://docs.unity3d.com/2023.2/Documentation/Manual/upm-ui-giturl.html) или ручного добавления в `Packages/manifest.json`: 
```
https://github.com/DCFApixels/DragonECS.git
```
* ### В виде исходников
Фреймворк так же может быть добавлен в проект в виде исходников.
</br>

# Основные концепции
## Entity
**Сущности** - это то к чему крепятся данные. Реализованы в виде идентификаторов, которых есть 2 вида:
* `int` - однократный идентификатор, применяется в пределах одного тика. Не рекомендуется хранить `int` идентификаторы, в место этого используйте `entlong`;
* `entlong` - долговременный идентификатор, содержит в себе полный набор информации для однозначной идентификации;
``` c#
// Создание новой сущности в мире.
int entityID = _world.NewEntity();

// Удаление сущности.
_world.DelEntity(entityID);

// Копирование компонентов одной сущности в другую.
_world.CopyEntity(entityID, otherEntityID);

// Клонирование сущности.
int newEntityID = _world.CloneEntity(entityID);
```

<details>
<summary>Работа с entlong</summary>
 
``` c#
// Конвертация int в entlong.
entlong entity = _world.GetEntityLong(entityID);
// или
entlong entity = (_world, entityID);

// Проверка что сущность еще жива.
if (entity.IsAlive) { }

// Конвертация entlong в int. Если сущность уже не существует, будет брошено исключение. 
int entityID = entity.ID;
// или
var (entityID, world) = entity;
 
// Конвертация entlong в int. Вернет true и ее int идентификатор, если сущность еще жива.
if (entity.TryGetID(out int entityID)) { }
```
 
 </details>
 
> Сущности не могут существовать без компонентов, пустые сущности будут автоматически удаляться сразу после удаления последнего компонента либо в конце тика.
## Component
**Компоненты** - это данные которые крепятся к сущностям.
```c#
// Компоненты IEcsComponent хранятся в обычном хранилище.
struct Health : IEcsComponent
{
    public float health;
    public int armor;
}
// Компоненты с IEcsTagComponent хранятся в оптимизированном для тегов хранилище.
struct PlayerTag : IEcsTagComponent {}
```

## System
**Системы** - это основная логика, тут задается поведение сущностей. Существуют в виде пользовательских классов, реализующих как минимум один из интерфейсов процессов. Основные процессы:
```c#
class SomeSystem : IEcsPreInit, IEcsInit, IEcsRun, IEcsDestroy
{
    // Будет вызван один раз в момент работы EcsPipeline.Init() и до срабатывания IEcsInit.Init().
    public void PreInit () { }
    
    // Будет вызван один раз в момент работы EcsPipeline.Init() и после срабатывания IEcsPreInit.PreInit().
    public void Init ()  { }
    
    // Будет вызван один раз в момент работы EcsPipeline.Run().
    public void Run () { }
    
    // Будет вызван один раз в момент работы EcsPipeline.Destroy().
    public void Destroy () { }
}
```
> Для реализации дополнительных процессов перейдите к разделу [Процессы](#Процессы).

</br>

# Концепции фреймворка
## Пайплайн
Контейнер и движок систем. Отвечает за настройку очереди вызова систем, предоставляет механизм для сообщений между системами и механизм внедрения зависимостей. Реализован в виде класса `EcsPipeline`.
### Построение
За построение пайплайна отвечает Builder. В Builder добавляются системы, а в конце строится пайплайн. Пример:
```c#
EcsPipeline pipeline = EcsPipeline.New() // Создает Builder пайплайна.
    // Добавляет систему System1 в очередь систем.
    .Add(new System1())
    // Добавляет System2 в очередь после System1.
    .Add(new System2())
    // Добавляет System3 в очередь после System2, но в единичном экземпляре.
    .AddUnique(new System3())
    // Завершает построение пайплайна и возвращает его экземпляр.
    .Build(); 
pipeline.Init(); // Инициализация пайплайна.
```

```c#
class SomeSystem : IEcsRun, IEcsPipelineMember
{
    // Получить экземпляр пайплайна к которому принадлежит система.
    public EcsPipeline Pipeline { get ; set; }

    public void Run () { }
}
```
> Для одновременного построения и инициализации есть метод Builder.BuildAndInit();
### Внедрение зависимостей
Фреймворк реализует внедрение зависимостей для систем. это процесс который запускается вместе с инициализацией пайплайна и внедряет данные переданные в Builder.

``` c#
class SomeDataA { /* ... */ }
class SomeDataB : SomeDataA { /* ... */ }

// ...
SomeDataB _someDataB = new SomeDataB();
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    // Внедрит _someDataB в системы реализующие IEcsInject<SomeDataB>.
    .Inject(_someDataB) 
    // Добавит системы реализующие IEcsInject<SomeDataA> в дерево инъекции,
    // теперь эти системы так же получат _someDataB.
    .Injector.AddNode<SomeDataA>()
    // ...
    .Add(new SomeSystem())
    // ...
    .BuildAndInit();

// ...
// Для внедрения используется интерфейс IEcsInject<T> и его метод Inject(T obj)
class SomeSystem : IEcsInject<SomeDataA>, IEcsRun
{
    SomeDataA _someDataA
    // obj будет экземпляром типа SomeDataB.
    public void Inject(SomeDataA obj) => _someDataA = obj;

    public void Run () 
    {
        _someDataA.DoSomething();
    }
}
```
> Использование встроенного внедрения зависимостей опционально. 

> Имеется [Расширение](#расширения) упрощающее синтаксис инъекций - [Автоматическое внедрение зависимостей](https://github.com/DCFApixels/DragonECS-AutoInjections).

### Модули
Группы систем реализующие общую фичу можно объединять в модули, и просто добавлять модули в Pipeline.
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

### Сортировка
Дла управления расположением систем в пайплайне, вне зависимости от порядка добавления, есть 2 способа: Слои и Порядок сортировки.
#### Слои
Слой определяет место в пайплайне для вставки систем. Например, если необходимо чтобы система была вставлена в конце пайплайна, эту систему можно добавить в слой `EcsConsts.END_LAYER`.
``` c#
const string SOME_LAYER = nameof(SOME_LAYER);
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    // Вставляет новый слой перед конечным слоем EcsConsts.END_LAYER
    .Layers.Insert(EcsConsts.END_LAYER, SOME_LAYER)
    // Система SomeSystem будет вставлена в слой SOME_LAYER
    .Add(New SomeSystem(), SOME_LAYER) 
    // ...
    .BuildAndInit();
```
Встроенные слои расположены в следующем порядке:
* `EcsConst.PRE_BEGIN_LAYER`
* `EcsConst.BEGIN_LAYER`
* `EcsConst.BASIC_LAYER` (По умолчанию системы добавляются сюда)
* `EcsConst.END_LAYER`
* `EcsConst.POST_END_LAYER`
#### Порядок сортировки
Для сортировки систем в рамках слоя используется int значение порядка сортировки. По умолчанию системы добавляются с `sortOrder = 0`.
``` c#
EcsPipeline pipeline = EcsPipeline.New()
    // ...
    // Система SomeSystem будет вставлена в слой EcsConsts.BEGIN_LAYER 
    // и расположена после систем с sortOrder меньше 10.
    .Add(New SomeSystem(), EcsConsts.BEGIN_LAYER, 10)
    // ...
    .BuildAndInit();
```

## Процессы
Процессы - это очереди систем реализующие общий интерфейс, например `IEcsRun`. Для запуска процессов используются Runner-ы. Встроенные процессы запускаются автоматически. Есть возможность реализации пользовательских процессов.

<details>
<summary>Встроенные процессы</summary>
 
* `IEcsPreInit`, `IEcsInit`, `IEcsRun`, `IEcsDestroy` - процессы жизненного цикла `EcsPipeline`.
* `IEcsInject<T>` - Процессы системы [внедрения зависимостей](#Внедрение-зависимостей).
* `IOnInitInjectionComplete` - Так же процесс системы [внедрения зависимостей](#Внедрение-зависимостей), но сигнализирует о завершении инициализирующей инъекции.

</details>
 
<details>
<summary>Пользовательские процессы</summary>
 
Для добавления нового процесса создайте интерфейс наследованный от `IEcsProcess` и создайте раннер для него. Раннер это класс реализующий интерфейс запускаемого процесса и наследуемый от `EcsRunner<TInterface>`. Пример:
``` c#
// Интерфейс процесса.
interface IDoSomethingProcess : IEcsProcess
{
    void Do();
}
```
``` c#
// Реализация раннера. Пример реализации можно так же посмотреть в встроенном процессе IEcsRun 
sealed class DoSomethingProcessRunner : EcsRunner<IDoSomethingProcess>, IDoSomethingProcess
{
    public void Do() 
    {
        foreach (var item in Process) item.Do();
    }
}
```
``` c#
// Добавление раннера при создании пайплайна.
_pipeline = EcsPipeline.New()
    // ...
    .AddRunner<DoSomethingProcessRunner>()
    // ...
    .BuildAndInit();

// Запуск раннера если раннер был добавлен.
_pipeline.GetRunner<IDoSomethingProcess>.Do()

// Или если раннер не был добавлен(Вызов GetRunnerInstance так же добавит раннер в пайплайн).
_pipeline.GetRunnerInstance<DoSomethingProcessRunner>.Do()
```

<details>
<summary>Расширенная реализация раннера</summary>

``` c#
internal sealed class DoSomethingProcessRunner : EcsRunner<IDoSomethingProcess>, IDoSomethingProcess
{
    // RunHelper упрощает реализацию по подобию реализации встроенных процессов. 
    // Автоматически вызывает маркер профайлера, а так же содержит try catch блок.
    private RunHelper _helper;
    protected override void OnSetup()
    {
        // Вторым аргументом задается имя маркера, если не указать, то имя будет выбрано автоматически.
        _helper = new RunHelper(this, nameof(Do));
    }
    public void Do()
    {
        _helper.Run(p => p.Do());
    }
}
```

</details>

> Требования реализации раннеров: 
> * Наследоваться от `EcsRunner<T>` можно только напрямую;
> * Раннер может содержать только один интерфейс(за исключением `IEcsProcess`);
> * Наследуемый класс `EcsRunner<T>`, должен так же реализовать интерфейс `T`;
    
> Не рекомендуется в цикле вызывать `GetRunner`, иначе кешируйте полученный раннер.

</details>

## Мир
Контейнер для сущностей и компонентов.
``` c#
// Создание экземпляра мира.
_world = new EcsDefaultWorld();

// Создание и удаление сущности по примеру из раздела Сущности.
var e = _world.NewEntity();
_world.DelEntity(e);

// Уничтожение мира и освобождение ресурсов. Обязательно вызывать, иначе он будет висеть в памяти.
_world.Destroy();
```
> Миры изолированы друг от друга и могут обрабатываться в отдельных потоках. Но мультипоточная обработка одного мира поддерживается только при отсутствии добавления/удаления компонентов у сущностей.

Для инициализации мира сразу необходимого размера и сокращения времени прогрева, в конструктор можно передать экземпляр `EcsWorldConfig`.

``` c#
EcsWorldConfig config = new EcsWorldConfig(
    // Предварительно инициализирует вместимость мира для 2000 сущностей.
    entitiesCapacity: 2000, 
    // Предварительно инициализирует вместимость пулов для 2000 компонентов.
    poolComponentsCapacity: 2000
    // ... Есть и другие параметры
    );  
_world = new EcsDefaultWorld(config);
```

## Пул
Хранилище для компонентов, пул предоставляет методы для добавления/чтения/редактирования/удаления компонентов на сущности. Есть несколько видов пулов, для разных видов компонентов:
* `EcsPool` - универсальный пул, хранит struct-компоненты реализующие интерфейс `IEcsComponent`;
* `EcsTagPool` - специальный пул, оптимизированный под компоненты-теги, хранит struct-компоненты с `IEcsTagComponent`;

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
> [!WARNING]
> В `Release` сборке отключаются проверки на исключения.

> Есть "безопасные" методы, которые сначала выполнят проверку наличия/отсутствия компонента, названия таких методов начинаются с `Try`.

<details>
<summary>Пользовательские пулы</summary>

Пулом выступает любой тип реализующий интерфейс `IEcsPoolImplementation<T>` и имеющий конструктор без параметров.

Ключевые моменты при реализации пула:
* За примером реализации пула можно обратиться к реализации встроенного пула `EcsPool<T>`.
* Интерфейс `IEcsPoolImplementation` и его члены не предназначены для публичного использования, члены интерфейса рекомендуется реализовывать явно.
* Подставленный в интерфейсе `IEcsPoolImplementation<T>` тип `T` и тип возвращаемый в свойствах `ComponentType` с `ComponentTypeID` должны совпадать.
* Обязательно регистрировать все изменения пула в экземпляре `EcsWorld.PoolsMediator` передаваемом в методе `OnInit`.
* `EcsWorld.PoolsMediator` предназначен только для использования внутри пула.
* Дефайн `DISABLE_POOLS_EVENTS` отключает реализуемые методы `AddListener` и `RemoveListener`.
* В статическом классе `EcsPoolThrowHelper` определены бросания наиболее распространенных видов исключений.
* В методе `OnReleaseDelEntityBuffer` происходит очистка удаленных сущностей.
* Рекомендуется определить интерфейс которым обозначаются компоненты для нового пула. На основе этого интерфейса можно реализовать методы расширения вроде `GetPool<T>()` для упрощенного доступа к пулам.
* Пулы должны реализовывать блокировку. Блокировка пула работает только в `Debug` режиме, и должна бросать исключения при попытке добавления или удаления компонента.

</details>

## Маска
Применяется для фильтрации сущностей по наличию или отсутствию компонентов.  
``` c#
// Создание маски которая проверяет что у сущностей есть компоненты 
// SomeCmp1 и SomeCmp2, но нет компонента SomeCmp3.
EcsMask mask = EcsMask.New(_world)
    // Inc - Условие наличия компонента.
    .Inc<SomeCmp1>()
    .Inc<SomeCmp2>()
    // Exc - Условие отсутствия компонента.
    .Exc<SomeCmp3>()
    .Build();
```

<details>
<summary>Статическая маска</summary>

`EcsMask` привязаны к конкретным экземплярам мира которые необходимо передавать в `EcsMask.New(world)`, но есть `EcsStaticMask` которую можно создать без привязки к миру.

``` c#
class SomeSystem : IEcsRun 
{
    // EcsStaticMask можно создавать в статических полях.
    static readonly EcsStaticMask _staticMask = EcsStaticMask.Inc<SomeCmp1>().Inc<SomeCmp2>().Exc<SomeCmp3>().Build();

    // ...
}
```
``` c#
// Конвертация в обычную маску.
EcsMask mask = _staticMask.ToMask(_world);
```

</details>

## Аспект
Пользовательские классы наследуемые от `EcsAspect` и используемые для взаимодействия с сущностями. Аспекты одновременно являются кешем пулов и содержат маску. Можно рассматривать аспекты как описание того с какими сущностями работает система.

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

<details>
<summary>Явный синтаксис (результат идентичен примеру выше):</summary>

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

</details>

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
Фильтруют сущности и выдают коллекции сущностей удовлетворяющие определенным условиям. Встроенный запрос `Where` фильтрует на соответствие условиям маски компонентов и имеет несколько перегрузок:
+ `EcsWorld.Where(EcsMask mask)` - Обычная фильтрация по маске;
+ `EcsWorld.Where<TAspect>(out TAspect aspect)` - Сочетает в себе фильтрацию по маске из аспекта и получение аспекта;

Запрос `Where` применим как к `EcsWorld` так и коллекциям фреймворка (в этом плане Where чем-то похож на аналогичный из Linq). Так же имеются перегрузки для сортировки сущностей по `Comparison<int>`. 

Пример системы:
``` c#
public class SomeDamageSystem : IEcsRun, IEcsInject<EcsDefaultWorld>
{
    class Aspect : EcsAspect
    {
        public EcsPool<Health> healths = Inc;
        public EcsPool<DamageSignal> damageSignals = Inc;
        public EcsTagPool<IsInvulnerable> isInvulnerables = Exc;
        // Наличие или отсутствие этого компонента не проверяется.
        public EcsTagPool<IsDiedSignal> isDiedSignals = Opt;
    }
    EcsDefaultWorld _world;
    public void Inject(EcsDefaultWorld world) => _world = world;

    public void Run()
    {
        foreach (var e in _world.Where(out Aspect a))
        {
            // Сюда попадают сущности с компонентами Health, DamageSignal и без IsInvulnerable.
            ref var health = ref a.healths.Get(e);
            if(health.points > 0)
            {
                health.points -= a.damageSignals.Get(e).points;
                if(health.points <= 0)
                { // Создаем сигнал другим системам о том что сущность умерла.
                    a.isDiedSignals.TryAdd(e);
                }
            }
        }
    }
}
```

> Имеется [Расширение](#расширения) упрощающее синтаксис запросов и обращения к компонентам - [Упрощенный синтаксис](https://github.com/DCFApixels/DragonECS-AutoInjections).
 
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
// поэтому будет переиспользована свободная или создана новая.
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
Группы являются множествами и реализуют интерфейс `ISet<int>`. Редактирующие методы имеет 2 варианта, с записью результата в groupA, либо с возвращением новой группы. 
                                
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
[MetaGroup("Abilities", "Passive", ...)] // или [MetaGroup("Abilities/Passive/")]

// Задает цвет типа в RGB кодировке, где каждый канал принимает значение от 0 до 255, по умолчанию белый. 
[MetaColor(MetaColor.Red)] // или [MetaColor(255, 0, 0)]
 
// Добавляет описание типу.
[MetaDescription("The quick brown fox jumps over the lazy dog")] 

// Добавляет строковый уникальный идентификатор.
[MetaID("8D56F0949201D0C84465B7A6C586DCD6")] // Строки должны быть уникальными, и не допускают символы ,<> .
 
// Добавляет строковые теги.
[MetaTags("Tag1", "Tag2", ...)] // [MetaTags(MetaTags.HIDDEN))] чтобы скрыть в редакторе 
public struct Component : IEcsComponent { /* ... */ }
```
``` c#
// Получение мета-информации:
TypeMeta typeMeta = someComponent.GetMeta();
// или
TypeMeta typeMeta = pool.ComponentType.ToMeta();

var name = typeMeta.Name; // [MetaName]
var group = typeMeta.Group; // [MetaGroup]
var color = typeMeta.Color; // [MetaColor]
var description = typeMeta.Description; // [MetaDescription]
var metaID = typeMeta.MetaID; // [MetaID]
var tags = typeMeta.Tags; // [MetaTags]
```
> Для автоматической генерации уникальных идентификаторов MetaID есть метод `MetaID.GenerateNewUniqueID()` и [Браузерный генератор](https://dcfapixels.github.io/DragonECS-MetaID_Generator_Online/)

## EcsDebug
Вспомогательный тип с набором методов для отладки и логирования. Реализован как статический класс вызывающий методы Debug-сервисов. Debug-сервисы - это посредники между EcsDebug и инструментами отладки среды. Такая реализация позволяет не изменяя отладочный код, менять его поведение или переносить проект в другие среды, достаточно только реализовать соответствующий Debug-сервис.

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

> По умолчанию используется `DefaultDebugService` который выводит логи в консоль. Для реализации пользовательского создайте класс наследуемый от `DebugService` и реализуйте абстрактные члены класса.

> `EcsDebug` потокобезопасен, за счет того что каждый поток использует свой изолированный экземпляр сервиса. Экземпляры для потоков создаются в абстрактном методе `DebugService.CreateThreadInstance`.

## Профилирование
За реализацию профайлера так же отвечает Debug-сервис. Для выделения участка кода используется `EcsProfilerMarker`;
``` c#
// Создание маркера с именем SomeMarker.
private static readonly EcsProfilerMarker _marker = new EcsProfilerMarker("SomeMarker");
```
``` c#
_marker.Begin();
// Код для которого замеряется скорость.
_marker.End();

// или

using (_marker.Auto())
{
    // Код для которого замеряется скорость.
}
```
> `DefaultDebugService` использует реализацию на основе `Stopwatch` и выводом в консоль.

</br>

# Define Symbols
+ `DRAGONECS_DISABLE_POOLS_EVENTS` - выключает реактивное поведение в пулах.
+ `DRAGONECS_ENABLE_DEBUG_SERVICE` - включает работу EcsDebug в релизном билде.
+ `DRAGONECS_STABILITY_MODE` - включает опускаемые в релизном билде проверки.
+ `DRAGONECS_DISABLE_CATH_EXCEPTIONS` - Выключает поведение по умолчанию по обработке исключений. По умолчанию фреймворк будет ловить исключения с выводом информации из исключений через EcsDebug и продолжать работу.
+ `REFLECTION_DISABLED` - Полностью ограничивает работу фреймворка с Reflection.
+ `DISABLE_DEBUG` - Для среды где не поддерживается ручное отключение DEBUG, например Unity.
+ `ENABLE_DUMMY_SPAN` - На случай если в среде не поддерживаются Span типы, включает его замену.

</br>

# Расширение фреймворка
Дополнительные инструменты полезные для создания расширений.

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

# Проекты на DragonECS
## С исходниками:

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

## Опубликованные проекты:

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

# Расширения
* Пакеты:
    * [Интеграция с движком Unity](https://github.com/DCFApixels/DragonECS-Unity)
    * [Автоматическое внедрение зависимостей](https://github.com/DCFApixels/DragonECS-AutoInjections)
    * [Классическая C# многопоточность](https://github.com/DCFApixels/DragonECS-ClassicThreads)
    * [Recursivity](https://github.com/DCFApixels/DragonECS-Recursivity)
    * [Hybrid](https://github.com/DCFApixels/DragonECS-Hybrid)
    * [Графы](https://github.com/DCFApixels/DragonECS-Graphs)
* Утилиты:
    * [Упрощенный синтаксис](https://gist.github.com/DCFApixels/d7bfbfb8cb70d141deff00be24f28ff0)
    * [Однокадровые компоненты](https://gist.github.com/DCFApixels/46d512dbcf96c115b94c3af502461f60)
    * [Шаблоны кода IDE](https://gist.github.com/ctzcs/0ba948b0e53aa41fe1c87796a401660b) и [для Unity](https://gist.github.com/ctzcs/d4c7730cf6cd984fe6f9e0e3f108a0f1)
> *Твое расширение? Если разрабатываешь расширение для DragonECS, пиши [сюда](#обратная-связь).

</br>
 
# FAQ
## 'ReadOnlySpan<>' could not be found
В версии Unity 2020.1.х в консоли может выпадать ошибка:
```
The type or namespace name 'ReadOnlySpan<>' could not be found (are you missing a using directive or an assembly reference?)
``` 
Чтобы починить добавьте директиву `ENABLE_DUMMY_SPAN` в `Project Settings/Player/Other Settings/Scripting Define Symbols`.

## Как Выключать/Включать системы?
Напрямую - никак. </br>
Обычно потребность выключить/включить систему появляется когда поменялось общее состояние игры, это может так же значить что нужно переключить сразу группу систем, все это в совокупности можно рассматривать как изменения процессов. Есть 2 решения：</br>
+ Если изменения процесса глобальные, то создать новый `EcsPipeline` и в цикле обновления движка запускать соответствующий пайплайн.
+ Разделить `IEcsRun` на несколько процессов и в цикле обновления движка запускать соответствующий процесс. Для этого создайте новый интерфейс процесса, раннер для запуска этого интерфейса и получайте раннер через `EcsPipeline.GetRunner<T>()`.
## Перечень рекомендаций [DragonECS-Vault](https://github.com/DCFApixels/DragonECS-Vault)
</br>

# Обратная связь
+ Discord (RU-EN) [https://discord.gg/kqmJjExuCf](https://discord.gg/kqmJjExuCf)
+ QQ (中文) [949562781](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781)

</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
</br></br></br>
<img width="0" src="https://github.com/user-attachments/assets/7bc29394-46d6-44a3-bace-0a3bae65d755"><!--Чтоб флаг подгружался в любом случае-->
