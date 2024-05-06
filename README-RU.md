<p align="center">
<img width="660" src="https://github.com/DCFApixels/DragonECS/assets/99481254/c09e385e-08c1-4c04-904a-36ad7e25e45b">
</p>

<p align="center">
<img alt="Version" src="https://img.shields.io/github/package-json/v/DCFApixels/DragonECS?color=%23ff4e85&style=for-the-badge">
<img alt="License" src="https://img.shields.io/github/license/DCFApixels/DragonECS?color=ff4e85&style=for-the-badge">
<a href="https://discord.gg/kqmJjExuCf"><img alt="Discord" src="https://img.shields.io/discord/1111696966208999525?color=%2300b269&label=Discord&logo=Discord&logoColor=%23ffffff&style=for-the-badge"></a>
<a href="http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=IbDcH43vhfArb30luGMP1TMXB3GCHzxm&authKey=s%2FJfqvv46PswFq68irnGhkLrMR6y9tf%2FUn2mogYizSOGiS%2BmB%2B8Ar9I%2Fnr%2Bs4oS%2B&noverify=0&group_code=949562781"><img alt="QQ" src="https://img.shields.io/badge/QQ-JOIN-00b269?logo=tencentqq&style=for-the-badge"></a>
</p>

# DragonECS - C# Entity Component System Framework
| Languages: | [Русский](https://github.com/DCFApixels/DragonECS/blob/main/README-RU.md) | [English(WIP)](https://github.com/DCFApixels/DragonECS) |
| :--- | :--- | :--- |

DragonECS - это [ECS](https://en.wikipedia.org/wiki/Entity_component_system) фреймворк нацеленный на максимальную удобность, модульность, расширяемость и производительность динамического изменения сущностей. Разработан на чистом C#, без зависимостей и генерации кода. Вднохновлен [LeoEcs](https://github.com/Leopotam/ecslite).

> [!WARNING]
> Проект предрелизной версии, поэтому API может меняться. В ветке main акутальная и рабочая версия. </br>
> Readme еще не завершен, если есть не ястные моменты, вопросы можно задать тут [Обратная связь](#обратная-связь)

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
    - [Слои](#слои)
  - [Процессы](#процессы)
  - [Мир](#мир)
  - [Пул](#пул)
  - [Аспект](#аспект)
  - [Запросы](#запросы)
  - [Коллекции](#Коллекции)
  - [Корень ECS](#корень-ecs)
- [Debug](#debug)
  - [Мета-Атрибуты](#мета-атрибуты)
  - [EcsDebug](#ecsdebug)
  - [Профилирование](#профилирование)
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

Протестированно:
+ **Unity:** Минимальная версия 2020.1.0;

## Установка для Unity
> Рекомендуется так же установить расширение [Интеграция с движком Unity](https://github.com/DCFApixels/DragonECS-Unity)
* ### Unity-модуль
Поддерживается установка в виде Unity-модуля в  при помощи добавления git-URL [в PackageManager](https://docs.unity3d.com/2023.2/Documentation/Manual/upm-ui-giturl.html) или ручного добавления в `Packages/manifest.json`: 
```
https://github.com/DCFApixels/DragonECS.git
```
* ### В виде иходников
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
entlong entity = entityID.ToEntityLong(_world);

// Проверка что сущность еще жива.
if (entity.IsAlive) { }

// Конвертация entlong в int. Если сущность уже не существует, будет брошено исключение. 
int entityID = entity.ID;

// Конвертация entlong в int. Вернет true и ее int идентификатор, если сущность еще жива.
if (entity.TryGetID(out int entityID)) { }
```
 
 </details>
 
> **NOTICE:** Сущности не могут существовать без компонентов, пустые сущности будут автоматически удаляться сразу после удаления последнего компонента либо в конце тика.
## Component
**Компоненты** - это данные для сущностей. Обязаны реализовывать интерфейс `IEcsComponent` или другой указываюший вид компонента. 
```c#
struct Health : IEcsComponent
{
    public float health;
    public int armor;
}
struct PlayerTag : IEcsTagComponent {}
```
Встроенные виды компонентов:
* `IEcsComponent` - Компоненты с данными. Универсальный тип компонентов.
* `IEcsTagComponent` - Компоненты-теги. Без данных.

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
    
    // Будет вызван один раз в момент работы EcsPipeline.Destroy()
    public void Destroy () { }
}
```
> Для реализации дополнительных процессов перейдите к разделу [Процессы](#Процессы).

</br>

# Концепции фреймворка
## Пайплайн
Является контейнером и движком систем, определяя поочередность их вызова, предоставляющий механизм для сообщений между системами и механизм внедрения зависимостей. Реализован в виде класса `EcsPipeline`.
### Построение
За построение пайплайна отвечает Builder. В Builder добавляются системы, а в конце строится пайплайн. Пример:
```c#
EcsPipelone pipeline = EcsPipeline.New() //Создает Builder пайплайна
    // Добавляет систему System1 в очередь систем
    .Add(new System1())
    // Добавляет System2 в очередь после System1
    .Add(new System2())
    // Добавляет System3 в очередь после System2, но в единичном экземпляре
    .AddUnique(new System3())
    // Завершает построение пайплайна и возвращает его экземпляр 
    .Build(); 
pipeline.Init(); // Инициализация пайплайна
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
> Использование встроенного внедрения зависимостей опционально. 
``` c#
class SomeDataA { /*...*/ }
class SomeDataB : SomeDataA { /*...*/ }

//...
SomeDataB _someDataB = new SomeDataB();
EcsPipelone pipeline = EcsPipeline.New()
    //...
    // Внедрит _someDataB в системы реализующие IEcsInject<SomeDataB>.
    .Inject(_someDataB) 
    // Добавит системы реализующие IEcsInject<SomeDataA> в дерево инъекции
    // теперь эти системы так же получат _someDataB.
    .Injector.AddNode<SomeDataA>() // 
    //...
    .BuildAndInit();

//...
// Для внедрения используется интерфейс IEcsInject<T> и его метод Inject(T obj)
class SomeSystem : IEcsInject<SomeDataA>, IEcsRun
{
    SomeDataA _someDataA
    //obj будет экземпляром типа SomeDataB
    public void Inject(SomeDataA obj) => _someDataA = obj;

    public void Run () 
    {
        _someDataA.DoSomething();
    }
}
```
### Модули
Группы систем реализующие общую фичу можно объединять в модули, и просто добавлять модули в Pipeline.
``` c#
using DCFApixels.DragonECS;
class Module : IEcsModule 
{
    public void Import(EcsPipeline.Builder b) 
    {
        b.Add(new System1());
        b.Add(new System2(), EcsConsts.END_LAYER); // данная система будет добавлена в слой EcsConsts.END_LAYER
        b.Add(new System3());
    }
}
```
``` c#
EcsPipelone pipeline = EcsPipeline.New()
    //...
    .AddModule(new Module())
    //...
    .BuildAndInit();
```
### Слои
Очередь систем можно разбить на слои. Слой определяет место в очереди для вставки систем. Если необходимо чтобы какая-то система была вставлена в конце очереди, вне зависимости от места добавления, эту систему можно добавить в слой EcsConsts.END_LAYER.
``` c#
const string SOME_LAYER = nameof(SOME_LAYER);
EcsPipelone pipeline = EcsPipeline.New()
    //...
    .Layers.Insert(EcsConsts.END_LAYER, SOME_LAYER) // Вставляет новый слой перед конечным слоем EcsConsts.END_LAYER
    .Add(New SomeSystem(), SOME_LAYER) // Система SomeSystem будет вставлена в слой SOME_LAYER
    //...
    .BuildAndInit();
```
Встроенные слои расположены в следующем порядке:
* `EcsConst.PRE_BEGIN_LAYER`
* `EcsConst.BEGIN_LAYER`
* `EcsConst.BASIC_LAYER` (Если при добавблении системы не указать слой, то она будет доавблена сюда)
* `EcsConst.END_LAYER`
* `EcsConst.POST_END_LAYER`

## Процессы
Процессы - это очереди систем реализующие общий интерфейс, например `IEcsRun`. Для запуска процессов используются Runner-ы. Втроенные процессы запускаются автоматически. Есть возможность реализации пользовательских процессов.

<details>
<summary>Встроенные процессы</summary>
 
* `IEcsPreInit`, `IEcsInit`, `IEcsRun`, `IEcsDestroy` - процессы жизненого цикла `EcsPipeline`.
* `IEcsInject<T>` - Процессы системы [внедрения зависимостей](#Внедрение-зависимостей).
* `IOnInitInjectionComplete` - Так же процесс системы [внедрения зависимостей](#Внедрение-зависимостей), но сигнализирует о завершении инициализирующей инъекции.

</details>
 
<details>
<summary>Пользовательские процессы</summary>
 
Для добавления нового процесса создайте интерфейс наследованный от `IEcsProcess` и создайте раннер для него. Раннер это класс реализующий интерфейс запускаемого процесса и наследуемый от `EcsRunner<TInterface>`. Пример:
``` c#
//Интерфейс.
interface IDoSomethingProcess : IEcsProcess
{
    void Do();
}
//Реализация раннера. Пример реализации можно так же посмотреть в встроенных процессах 
sealed class DoSomethingProcessRunner : EcsRunner<IDoSomethingProcess>, IDoSomethingProcess
{
    public void Do() 
    {
        foreach (var item in Process) item.Do();
    }
}
//...

//Добавление раннера при создании пайплайна.
_pipeline = EcsPipeline.New()
    //...
    .AddRunner<DoSomethingProcessRunner>()
    //...
    .BuildAndInit();

//Запуск раннера если раннер был добавлен.
_pipeline.GetRunner<IDoSomethingProcess>.Do()

//Или если раннер небыл добавлен(Вызов GetRunnerInstance так же добавит раннер в пайплайн).
_pipeline.GetRunnerInstance<DoSomethingProcessRunner>.Do()
```
> Раннеры имеют ряд требований к реализации: 
> * Наследоваться от `EcsRunner<T>` можно только напрямую;
> * Раннер может содержать только один интерфейс(за исключением `IEcsProcess`);
> * Наследуемый класс `EcsRunner<T>,` далжен так же реализовавыть интерфейс `T`;
    
> Не рекомендуется в цикле вызывать `GetRunner`, иначе кешируйте полученный раннер.
</details>

## Мир
Является контейнером для сущностей и компонентов.
``` c#
//Создание экземпляра мира
_world = new EcsDefaultWorld();
//Пример из раздела Сущности
var e = _world.NewEntity();
_world.DelEntity(e);
```
> **NOTICE:** Необходимо вызывать EcsWorld.Destroy() у экземпляра мира если он больше не используется, иначе он будет висеть в памяти.

### Конфигурация мира
При создании мира, в конструктор можно передать экземпляр `EcsWorldConfig`.

``` c#
EcsWorldConfig config = new EcsWorldConfig(
    //предварительно инициализирует вместимость мира для 2000 сущностей
    entitiesCapacity: 2000, 
    //предварительно инициализирует вместимость пулов для 2000 компонентов
    poolComponentsCapacity: 2000);  
_world = new EcsDefaultWorld(config);
```

> Компоненты и конфиги можно применять для создания расширений в связке с методами расширений.
## Пул
Является контейнером для компонентов, предоставляет методы для добавления/чтения/редактирования/удаления компонентов на сущности. Есть несколько видов пулов, для разных целей:
* `EcsPool` - универсальный пул, хранит struct-компоненты реализующие интерфейс `IEcsComponent`;
* `EcsTagPool` - подходит для хранения пустых компонентов-тегов, в сравнении с `EcsPool` имеет лучше оптимизацию памяти и скорости, хранит struct-компоненты с `IEcsTagComponent`;

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
> Есть "безопасные" методы, которые сначала выполнят проверку наличия/отсутствия компонента, названия таких методов начинаются с `Try`
    
Имеется возможность реализации пользовательского пула
> эта функция будет описана в ближайшее время
 
## Аспект
Это пользовательские классы наследуемые от `EcsAspect` и используемые для взаимодействия с сущностями. Аспекты одновременно являются кешем пулов и маской компонентов для фильтрации сущностей. Можно рассматривать аспекты как описание того с какими сущностями работает система.

Упрощенный синтаксис:
``` c#
using DCFApixels.DragonECS;
...
class Aspect : EcsAspect
{
    // Кешируется пул и Pose добавляется во включающее ограничение.
    public EcsPool<Pose> poses = Inc;
    // Кешируется пул и Velocity добавляется во включающее ограничение.
    public EcsPool<Velocity> velocities = Inc;
    // Кешируется пул и FreezedTag добавляется в исключающее ограничение.
    public EcsTagPool<FreezedTag> freezedTags = Exc;

    // При запросах будет проверяться наличие компонентов
    // из включающего ограничения маски и отсутсвие из исключющего.
    // Так же есть Opt - только кеширует пул, не влияя на маску. 
}
```

Явный синтаксис (результат идентичен примеру выше):
``` c#
using DCFApixels.DragonECS;
...
class Aspect : EcsAspect
{
    public EcsPool<Pose> poses;
    public EcsPool<Velocity> velocities;
    // вместо виртуальной функции, можно использовать конструктор Aspect(Builder b) 
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
...
class Aspect : EcsAspect
{
    public OtherAspect1 otherAspect1;
    public OtherAspect2 otherAspect2;
    public EcsPool<Pose> poses;
 
    // функция Init аналогична конструктору Aspect(Builder b)
    protected override void Init(Builder b)
    {
        // комбинирует с SomeAspect1.
        otherAspect1 = b.Combine<OtherAspect1>(1);
        // хотя для OtherAspect1 метод Combine был вызван раньше, сначала будет скомбинирован с OtherAspect2, так как по умолчанию order = 0.
        otherAspect2 = b.Combine<OtherAspect2>();
        // если в OtherAspect1 или в OtherAspect2 было ограничение b.Exclude<Pose>() тут оно будет заменено на b.Include<Pose>().
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
Что бы получить необходимый набор сущностей используется метод-запрос `EcsWorld.Where<TAspcet>(out TAspcet aspect)`. В качестве `TAspcet` указывается аспект, сущности будут отфильтрованны по маске указанного аспекта. Запрос `Where` применим как к `EcsWorld` так и коллекциям фреймворка (в этом плане Where чемто похож на аналогичный из Linq).
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
            // Сюда попадают сущности с компонентами Health, DamageSignal и без IsInvulnerable
            a.healths.Get(e).points -= a.damageSignals.Get(e).points;
        }
    }
}
```
 
## Коллекции

### EcsSpan
Коллекция сущностей, доступная только для чтения и выделяемая только в стеке. Состоит из ссылки на массив, длинны и идентификатора мира. Аналог `ReadOnlySpan<int>`.
``` c#
//Запрос Where возвращает сущности в виде EcsSpan
EcsSpan es = _world.Where(out Aspect a);
//Итерироваться можно по foreach и for
foreach (var e in es)
{
    // ...
}
for (int i = 0; i < es.Count; i++)
{
    int e = es[i];
    //...
}
```
> Хотя `EcsSpan` является просто массивом, в нем не допускается дублирование сущностей. 

### EcsGroup
Вспомогательная коллекция основаная на spase set для хранения множества сущностей с O(1) операциями добавления/удаления/проверки и т.д.
``` c#
//Получем новую группу. EcsWorld содержит в себе пул групп,
//поэтому будет создана новая или переиспользована свободная.
EcsGroup group = EcsGroup.New(_world);
//Освобождаем группу.
group.Dispose();
```
``` c#
//Добвялем сущность entityID.
group.Add(entityID);
//Проверяем наличие сущности entityID.
group.Has(entityID);
//Удялем сущность entityID.
group.Remove(entityID);
```
``` c#
//Запрос WhereToGroup возвращает сущности в виде группы только для чтения EcsReadonlyGroup
EcsReadonlyGroup group = _world.WhereToGroup(out Aspect a);
//Итерироваться можно по foreach и for
foreach (var e in group) 
{ 
    //...
}
for (int i = 0; i < group.Count; i++)
{
    int e = group[i];
    //...
}
```
Так как группы это множества, они содержат методы аналогичные `ISet<T>`. Редактирующие методы имеет 2 варианта, с записью результата в groupA, либо с возвращением новой группы:            
                                
``` c#
// Объединение groupA и groupB
groupA.UnionWith(groupB);
EcsGroup newGroup = EcsGroup.Union(groupA, groupB);

// Пересечение groupA и groupB
groupA.IntersectWith(groupB);
EcsGroup newGroup = EcsGroup.Intersect(groupA, groupB);

// Разность groupA и groupB
groupA.ExceptWith(groupB);
EcsGroup newGroup = EcsGroup.Except(groupA, groupB);

// Симметрическая разность groupA и groupB
groupA.SymmetricExceptWith(groupB);
EcsGroup newGroup = EcsGroup.SymmetricExcept(groupA, groupB);

//Разница всех сущностей в мире и groupA
groupA.Inverse();
EcsGroup newGroup = EcsGroup.Inverse(groupA);
```

## Корень ECS
Это пользовательский класс который явялестя точкой входа для ECS. Основное назначение инициализация, запуск систем на каждый Update движка и очистка по окончанию сипользования.
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
        //Создание мира для сущностей икомпонентов
        _world = new EcsDefaultWorld();
        //Создание пайплайна длясистем
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
        //Иницивлизация пайплайна и запуск IEcsPreInit.PreInit
        //и IEcsInit.Init у всех добавленных систем
        _pipeline.Init();
    }
    private void Update()
    {
        //Запуск IEcsRun.Run у всех добавленных систем
        _pipeline.Run();
    }
    private void OnDestroy()
    {
        //Запускает IEcsDestroyInit.Destroy у всех добавленных систем
        _pipeline.Destroy();
        _pipeline = null;
        //Обязательно удалять миры которые больше не будут использованы 
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
        //Создание мира для сущностей икомпонентов.
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
        // Иницивлизация пайплайна и запуск IEcsPreInit.PreInit
        // и IEcsInit.Init у всех добавленных систем.
        _pipeline.Init();
    }

    // Update-цикл движка.
    public void Update()
    {
        // Запуск IEcsRun.Run у всех добавленных систем.
        _pipeline.Run();
    }

    // Очистка окружения.
    public void Destroy()
    {
        // Запускает IEcsDestroyInit.Destroy у всех добавленных систем.
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
В чистом виде мета-атрибуты не имеют применения, но могут быть использованы для генерации автоматической документации и используются в интеграциях с движками для задания отображения в отладочных инструментах и редакторах.
``` c#
using DCFApixels.DragonECS;

// Задает пользовательское название типа, по умолчанию используется имя типа.
[MetaName("SomeComponent")]

// Используется для группировки типов.
[MetaGroup("Abilities/Passive/")] // или [MetaGroup("Abilities", "Passive")]

// Задает цвет типа в системе rgb, где каждый канал принимает значение от 0 до 255, по умолчанию белый. 
[MetaColor(MetaColor.Red)] // или [MetaColor(255, 0, 0)]
 
// Добавляет описание типу.
[MetaDescription("The quick brown fox jumps over the lazy dog")] 
 
// Добавляет строковые теги.
[MetaTags("Tag1", "Tag2", ...)]  // [MetaTags(MetaTags.HIDDEN))] чтобы скрыть в редакторе 
public struct Component { }
```
Получение мета-информации. С помощью TypeMeta:
``` c#
TypeMeta typeMeta = someComponent.GetMeta();
// или
TypeMeta typeMeta = pool.ComponentType.ToMeta();

var name = typeMeta.Name;
var color = typeMeta.Color;
var description = typeMeta.Description;
var group = typeMeta.Group;
var tags = typeMeta.Tags;
```
С помощью EcsDebugUtility:
``` c#
EcsDebugUtility.GetMetaName(someComponent);
EcsDebugUtility.GetColor(someComponent);
EcsDebugUtility.GetDescription(someComponent);
EcsDebugUtility.GetGroup(someComponent);
EcsDebugUtility.GetTags(someComponent);
```
## EcsDebug
Имеет набор методов для отладки и логирования. Реализован как статический класс вызывающий методы Debug-сервисов. Debug-сервисы - это посредники между системами отладки среды и EcsDebug. Это позволяет не изменяя отладочный код проекта, переносить проект на другие движки, достаточно только реализовать специальный Debug-сервис.

По умолчанию используется `DefaultDebugService` который выводит логи в консоль. Для реализации пользовательского создайте класс наследуемый от `DebugService` и реализуйте абстрактные члены класса.

``` c#
// Логирование.
EcsDebug.Print("Message");

// Логирование с тегом.
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

// или

using (marker.Auto())
{
    // Код для которого замеряется скорость.
}
```

</br>

# Расширение фреймворка
Для большей расширемости фреймворкеа есть дополнительные инструменты.

## Конфиги
Конструкторы классов `EcsWorld` и `EcsPipeline` могут принимать контейнеры конфигов реализующие интерфейс `IConfigContainer` или `IConfigContainerWriter`. С помощью этих контейнеров можно передавать данные и зависимости. Встроенная реализация контейнера - `ConfigContainer`, но можно так же использовать свою реализацию.</br>
Пример использования конфигов для мира:
``` c#
var configs = new ConfigContainer()
    .Set(new EcsWorldConfig(entitiesCapacity: 2000, poolsCapacity: 2000)
    .Set(new SomeDataA(/* ... */))
    .Set(new SomeDataB(/* ... */)));
EcsDefaultWorld _world = new EcsDefaultWorld(configs);
//...
var _someDataA = _world.Configs.Get<SomeDataA>();
var _someDataB = _world.Configs.Get<SomeDataB>();
```
Пример использования конфигов для пайплайна:
``` c#
_pipeline = EcsPipeline.New()// аналогично _pipeline = EcsPipeline.New(new ConfigContainer())
    .Configs.Set(new SomeDataA(/* ... */))
    .Configs.Set(new SomeDataB(/* ... */))
    //...
    .BuildAndInit();
//...
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
    private SomeReusedClass _resusedObject; // Объект который будет переиспользоваться.
    public SomeClass Object => _object;
    public SomeReusedClass ResusedObject => _resusedObject;
    void IEcsWorldComponent<WorldComponent>.Init(ref WorldComponent component, EcsWorld world)
    {
        if (component._resusedObject == null)
            component._resusedObject = new SomeReusedClass();
        component._object = new SomeClass();
        // Теперь при получении компонента через EcsWorld.Get, _resusedObject и _object уже будут созданы.
    }
    void IEcsWorldComponent<WorldComponent>.OnDestroy(ref WorldComponent component, EcsWorld world)
    {
        // Утилизируем не нужный объект, и освобождаем ссылку на него, чтобы GC мог его собрать.
        component._object.Dispose();
        component._object = null;
        
        // Как вариант тут можно сделать сброс значений у переиспользуемого объекта.
        //component._resusedObject.Reset();
        
        //Так как в этом примере не нужно полное обнуление компонента, то строчка ниже не нужна.
        //component = default;
    }
}
```
</details>

</br>

# Проекты на DragonECS
* [3D Platformer (Example)](https://github.com/DCFApixels/3D-Platformer-DragonECS)
![alt text](https://i.ibb.co/hm7Lrm4/Platformer.png)

</br>

# Расширения
* [Интеграция с движком Unity](https://github.com/DCFApixels/DragonECS-Unity)
* [Автоматическое внедрение зависимостей](https://github.com/DCFApixels/DragonECS-AutoInjections)
* [Классическоя C# многопоточность](https://github.com/DCFApixels/DragonECS-ClassicThreads)
* [Hybrid](https://github.com/DCFApixels/DragonECS-Hybrid)
* [One-Frame Components](https://gist.github.com/DCFApixels/46d512dbcf96c115b94c3af502461f60)
* Графы (Work in progress)
<!--* Твое расширение? Если разрабатываешь свои расширения для DragonECS, дай знать и они будут добавлены сюда-->

</br>
 
# FAQ
## 'ReadOnlySpan<>' could not be found
В версии юнити 2020.1.х в консоли может выпадать ошибка:
```
The type or namespace name 'ReadOnlySpan<>' could not be found (are you missing a using directive or an assembly reference?)
``` 
Чтобы починить добавте директиву `ENABLE_DUMMY_SPAN` в `Project Settings/Player/Other Settings/Scripting Define Symbols`.

## Как Выключать/Включать системы?
Напряму - никак. </br>
Обычно потребность выключить/включить систему появляется когда поменялось общее состояние игры, это может так же значить что нужно переключить сразу группу систем, все это в совокупности можно рассматривать как измннеия процессов. Есть 2 решения：</br>
+ Если измненеия процесса глобальные, то создать новый `EcsPipeline` и в цикле обновления движка запускать соотвествующий пайплайн.
+ Разделить `IEcsRun` на несколько процессов и в цикле обновления движка запускать соотвествующий процесс. Для этого создайте новый интерфейс процесса, раннер для запуска этого интерфейса и получайте раннер через `EcsPipeline.GetRunner<T>()`.
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
