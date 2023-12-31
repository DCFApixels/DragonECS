<p align="center">
<img width="660" src="https://github.com/DCFApixels/DragonECS/assets/99481254/c09e385e-08c1-4c04-904a-36ad7e25e45b.png">
</p>

<p align="center">
<img alt="Version" src="https://img.shields.io/github/package-json/v/DCFApixels/DragonECS?color=%23ff4e85&style=for-the-badge">
<img alt="License" src="https://img.shields.io/github/license/DCFApixels/DragonECS?color=ff4e85&style=for-the-badge">
<a href="https://discord.gg/kqmJjExuCf"><img alt="Discord" src="https://img.shields.io/discord/1111696966208999525?color=%2300b269&label=Discord&logo=Discord&logoColor=%23ffffff&style=for-the-badge"></a>
</p>

# DragonECS - C# Entity Component System Framework

| Languages: | [Русский](https://github.com/DCFApixels/DragonECS/blob/main/README-RU.md) | [English(WIP)](https://github.com/DCFApixels/DragonECS) |
| :--- | :--- | :--- |

Данный [ECS](https://en.wikipedia.org/wiki/Entity_component_system) Фреймворк нацелен на максимальную удобность, модульность, расширяемость и производительность динамического изменения сущностей. Без генерации кода и зависимостей. Вднохновлен [LeoEcs](https://github.com/Leopotam/ecslite).

> [!IMPORTANT]
> И с Новым Годом

> [!WARNING]
> Проект в стадии разработки. API может меняться.  
> Readme еще не завершен

## Оглавление
- [DragonECS - C# Entity Component System Framework](#dragonecs---c-entity-component-system-framework)
  - [Оглавление](#оглавление)
- [Установка](#установка)
    - [Версионирование](#версионирование)
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
    - [Компоненты мира](#компоненты-мира)
  - [Пул](#пул)
  - [Аспект](#аспект)
  - [Запросы](#запросы)
  - [Группа](#группа)
  - [Корень ECS](#корень-ecs)
    - [Пример для Unity](#пример-для-unity)
    - [Общий пример](#общий-пример)
  - [Гибридность](#гибридность)
- [Debug](#debug)
  - [Атрибуты](#атрибуты)
  - [EcsDebug](#ecsdebug)
  - [Профилирование](#профилирование)
- [Расширения](#расширения)
- [FAQ](#faq)
  - ['ReadOnlySpan\<\>' could not be found](#readonlyspan-could-not-be-found)
- [Обратная связь](#обратная-связь)

</br>

# Установка
* ### Unity-модуль
Поддерживается установка в виде Unity-модуля в  при помощи добавления git-URL [в PackageManager](https://docs.unity3d.com/2023.2/Documentation/Manual/upm-ui-giturl.html) или ручного добавления в `Packages/manifest.json`: 
```
https://github.com/DCFApixels/DragonECS.git 
```
* ### В виде иходников
Фреймворк так же может быть добавлен в проект в виде исходников.

### Версионирование
В DragonECS применяется следующая семантика версионирования: [Открыть](https://gist.github.com/DCFApixels/e53281d4628b19fe5278f3e77a7da9e8#file-dcfapixels_versioning_ru-md)

</br>

# Основные концепции
## Entity
**Сущности** - это то к чему крепятся данные. Реализованы в виде идентификаторов, которых есть 2 вида:
* `int` - однократный идентификатор, применяется в пределах одного тика. Не рекомендуется хранить `int` идентификаторы, в место этого используйте `entlong`;
* `entlong` - долговременный идентификатор, содержит в себе полный набор информации для однозначной идентификации;
``` csharp
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
 
``` csharp
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
* `IEcsComponent` - Компоненты с данными.
* `IEcsTagComponent` - Компоненты-теги. Без данных.
* `IEcsHybridComponent` - Гибридные компоненты. Испольщуются для реализации [гибридности](#Гибридность).

## System
**Системы** - это основная логика, тут задается поведение сущностей. Существуют в виде пользовательских классов, реализующих как минимум один из интерфейсов процессов. Основные процессы:
```c#
class SomeSystem : IEcsPreInitProcess, IEcsInitProcess, IEcsRunProcess, IEcsDestroyProcess
{
    // Будет вызван один раз в момент работы EcsPipeline.Init() и до срабатывания IEcsInitProcess.Init()
    public void PreInit () { }
    
    // Будет вызван один раз в момент работы EcsPipeline.Init() и после срабатывания IEcsPreInitProcess.PreInit()
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
> Для одновременного построения и инициализации есть метод Builder.BuildAndInit();
### Внедрение зависимостей
Внедрение зависимостей - это процесс который запускается вместе с инициализацией пайплайна и внедряет данные переданные в Builder.

> [!WARNING]
> Внедрение идет параллельно с PreInit, поэтому в PreInit инъекция - не гарантируется.
> [!WARNING]
> Экземпляр EcsPipeline автоматически внедряется до еще до PreInit.
``` c#
SomeData _someData;
//...
EcsPipelone pipeline = EcsPipeline.New()
    //...
    .Inject(_someData) // Внедрит в системы экземпляр _someData
    //...
    .BuildAndInit();

//...

class SomeSystem : IInject<SomeData>, IEcsRunProcess
{
    // Для внедрения используется интерфейс IInject<T> и его метод Inject(T obj)
    SomeData _someData
    public void Inject(SomeData obj) => _someData = obj;

    public void PreInit ()
    {
        // тут возможно еще не внедрен _someData
    }
    public void Init ()
    {
        // тут можно пользовать _someData
    }
    public void Run ()
    {
        // тут можно пользовать _someData
    }
    public void Destroy () 
    {
        // тут можно пользовать _someData
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
``` csharp
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
Процессы - это очереди систем реализующие общий интерфейс, например `IEcsRunProcess`. Для запуска процессов используются Runner-ы. Встроенные процессы вызываются автоматически, для запуска пользовательских процессов используйте раннеры получаемые из `EcsPipeline.GetRunner<TInterface>()`.
> Рекомендуется кешировать полученные через GetRunner раннеры.

<details>
<summary>Встроенные процессы</summary>
 
* `IEcsPreInitProcess`, `IEcsInitProcess`, `IEcsRunProcess`, `IEcsDestroyProcess` - процессы жизненого цикла `EcsPipeline`.
* `IEcsPreInject`, `IEcsInject<T>` - Процессы системы [внедрения зависимостей](#Внедрение-зависимостей).
* `IEcsPreInitInjectProcess` - Так же процесс системы [внедрения зависимостей](#Внедрение-зависимостей), но работает в пределах до выполнения IEcsInitProcess, сигнализирует о начале и окончании предварительных внедрений.

</details>
 
<details>
<summary>Пользовательские процессы</summary>
 
Для добавления нового процесса создайте интерфейс наследованный от `IEcsProcess` и создайте раннер для него. Раннер это класс реализующий интерфейс запускаемого процесса и наследуемый от EcsRunner<TInterface>. А после к интерфейсу добавте атрибут `BindWithEcsRunner` для связи. Пример:
 ```c#
[BindWithEcsRunner(typeof(DoSomethingProcessRunner))]
interface IDoSomethingProcess : IEcsProcess
{
    void Do();
}
sealed class DoSomethingProcessRunner : EcsRunner<IDoSomethingProcess>, IDoSomethingProcess
{
    public void Do()
    {
        foreach (var item in targets) item.Do();
    }
}
```
> Раннеры имеют ряд требований к реализации: 
> * Для одного интерфейса может быть только одна реализация раннера;
> * Наследоваться от `EcsRunner<TInterface>` можно только напрямую;
> * Раннер может содержать только один интерфейс(за исключением `IEcsSystem`);
> * Наследуемый класс `EcsRunner<TInterface>,` в качестве `TInterface` должен принимать реализованный интерфейс;
> * Раннер не может быть размещен внутри другого класса.
    
</details>

## Мир
Является контейнером для сущностей и компонентов.
> **NOTICE:** Необходимо вызывать EcsWorld.Destroy() у экземпляра мира если он больше не нужен.
### Компоненты мира
С помощью компонентов можно прикреплять дополнительные данные к мирам. В качестве компонентов используются `struct` типы.
``` csharp
ref WorldComponent component = ref _world.Get<WorldComponent>();
```
Реализация компонента:
``` csharp
public struct WorldComponent
{
    // Данные.
}
```
Или:
``` csharp
public struct WorldComponent : IEcsWorldComponent<WorldComponent>
{
    // Данные.
    void IEcsWorldComponent<WorldComponent>.Init(ref WorldComponent component, EcsWorld world)
    {
        // Действия при инициализации компонента. Вызывается до первого возвращения из EcsWorld.Get
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
``` csharp
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

> Компоненты можно применять для создания расширений в связке с методами расширений.
## Пул
Является контейнером для компонентов, предоставляет методы для добавления/чтения/редактирования/удаления компонентов на сущности. Есть несколько видов пулов, для разных целей:
* `EcsPool` - универсальный пул, хранит struct-компоненты реализующие интерфейс `IEcsComponent`;
* `EcsTagPool` - подходит для хранения пустых компонентов-тегов, в сравнении с `EcsPool` имеет лучше оптимизацию памяти и скорости, хранит struct-компоненты `IEcsTagComponent`;
* `EcsHybridPool` - пул для гибридных компонентов. Испольщуются для реализации [гибридности](#Гибридность), хранит struct-компоненты `IEcsHybridComponent`;

Пулы имеют 5 основных метода и их разновидности:
``` csharp
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
Это пользовательские классы наследуемые от `EcsAspect`, которые используются как посредник для взаимодействия с сущностями. Аспекты одновременно являются кешем пулов и ограничением для фильтрации сущностей.
``` csharp
using DCFApixels.DragonECS;
...
class Aspect : EcsAspect
{
    public EcsPool<Pose> poses;
    public EcsPool<Velocity> velocities;
 
    // вместо конструктора можно использовать виртуальную функцию Init(Builder b)
    public Aspect(Builder b) 
    {
        // кешируется пул и Pose добавляется во включающее ограничение.
        poses = b.Include<Pose>();
 
        // кешируется пул и Velocity добавляется во включающее ограничение.
        velocities = b.Include<Velocity>();
 
        // FreezedTag добавляется в исключающее ограничение.
        b.Exclude<FreezedTag>();
    }
}
```
В аспекты можно добавлять другие аспекты, тем самым комбинируя их. Ограничения так же будут скомбинированы
``` csharp
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
 

## Запросы
Используйте метод-запрос `EcsWorld.Where<TAspcet>(out TAspcet aspect)` для получения необходимого системе набора сущностей. Запросы работают в связке с аспектами, аспекты определяют ограничения запросов, результатом запроса становится группа сущностей удовлетворяющая условиям аспекта. По умолчанию запрос делает выборку из всех сущностей в мире, но так же можно сделать выборку из определенной группы сущностей, для этого используйте `EcsWorld.WhereFor<TAspcet>(EcsReadonlyGroup sourceGroup, out TAspcet aspect)`
 
## Группа
Группы это структуры данных для хранения множества сущностей с быстрыми операциями добавления/удаления/проверки наличия и т.д. Реализованы классом `EcsGroup` и структурой `EcsReadonlyGroup`. 

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
//Итерируемся через foreach или for.
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
``` csharp
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
        //Иницивлизация пайплайна и запуск IEcsPreInitProcess.PreInit
        //и IEcsInitProcess.Init у всех добавленных систем
        _pipeline.Init();
    }
    private void Update()
    {
        //Запуск IEcsRunProcess.Run у всех добавленных систем
        _pipeline.Run();
    }
    private void OnDestroy()
    {
        //Запускает IEcsDestroyInitProcess.Destroy у всех добавленных систем
        _pipeline.Destroy();
        _pipeline = null;
        //Обязательно удалять миры которые больше не будут использованы 
        _world.Destroy();
        _world = null;
    }
}
```
### Общий пример
``` csharp
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
        // Иницивлизация пайплайна и запуск IEcsPreInitProcess.PreInit
        // и IEcsInitProcess.Init у всех добавленных систем.
        _pipeline.Init();
    }

    // Update-цикл движка.
    public void Update()
    {
        // Запуск IEcsRunProcess.Run у всех добавленных систем.
        _pipeline.Run();
    }

    // Очистка окружения.
    public void Destroy()
    {
        // Запускает IEcsDestroyInitProcess.Destroy у всех добавленных систем.
        _pipeline.Destroy();
        _pipeline = null;
        // Обязательно удалять миры которые больше не будут использованы.
        _world.Destroy();
        _world = null;
    }
}
```
## Гибридность
Для смешивания архитектурных подходов классического OOP и ECS используется специальный пул `EcsHybridPool<T>`. Принцип работы этого пула несколько отличается от других и он упрощает поддержу наследования и полиморфизма. 

<details>
<summary>Как это работает?</summary>

При добавлении элемента в пул, пул сканирует его иерархию наследования и реализуемые интерфейсы в поиске типов у которых есть интерфес `IEcsHybridComponent` и автоматически добавляет компонент в соответсвующие этим типам пулы. Таким же образом происходит удаление. Сканирвоание просиходит не для типа T а для типа экземпляра, поэтому в примере ниже строчка в `_world.GetPool<ITransform>().Add(entity, _rigidbody);` добавляет не только в пул `EcsHybridPool<ITransform>` но и в остальные.

</details>

Пример использования:
``` csharp
public interface ITransform : IEcsHybridComponent
{
    Vector3 Position { get; set; }
    // ...
}
public class Transform : ITransform
{
    public Vector3 Position { get; set; }
    // ...
}
public class Rigidbody : Transform
{
    public Vector3 Position { get; set; }
    public float Mass { get; set; }
    // ...
}
public class Camera : ITransform
{
    Vector3 Position { get; set; }
    // ...
}
public TransformAspect : EcsAspect
{
    public EcsHybridPool<Transform> transforms;
    public Aspect(Builder b) 
    {
        transforms = b.Include<Transform>();
    }
}
// ...

EcsWorld _world;
Rigidbody _rigidbody;
// ...

// Создадим пустую сущность.
int entity = _world.NewEmptyEntity();
// Получаем пул EcsHybridPool<ITransform> и добавляем в него для сущности компонент _rigidbody.
// Если вместо ITransform подставить Transform или Rigidbody, то результат будет одинаковый
_world.GetPool<ITransform>().Add(entity, _rigidbody);
// ...

//Все эти строчки вернут экземпляр _rigidbody.
ITransform iTransform = _world.GetPool<ITransform>().Get(entity);  
Transform transform = _world.GetPool<Transform>().Get(entity);  
Rigidbody rigidbody = _world.GetPool<Rigidbody>().Get(entity);
//Исключение - отсутсвует компонент. Camera не является наследником или наследуемым классом для _rigidbody.
Camera camera = _world.GetPool<Camera>().Get(entity);

//Вернет True. Поэтому фишка гибридных пулов будет работать и в запросах сущностей
bool isMatches = _world.GetAspect<TransformAspect>().IsMatches(entity);

//Все эти строчки вернут True.
bool isITransform = _world.GetPool<ITransform>().Has(entity);  
bool isTransform = _world.GetPool<Transform>().Has(entity);  
bool isRigidbody = _world.GetPool<Rigidbody>().Has(entity);
//Эта строчка вернет False.
bool isCamera = _world.GetPool<Camera>().Has(entity);
// ...

// Удалим у сущности компонент.
_world.GetPool<ITransform>().Del(entity);
// ...
//Все эти строчки вернут False.
bool isITransform = _world.GetPool<ITransform>().Has(entity);  
bool isTransform = _world.GetPool<Transform>().Has(entity);  
bool isRigidbody = _world.GetPool<Rigidbody>().Has(entity);
bool isCamera = _world.GetPool<Camera>().Has(entity);
// ...
```

</br>

# Debug
Фреймворк предоставляет дополнительные инструменты для отладки и логирования, не зависящие от среды. Так же многие типы имеют свой DebuggerProxy для более информативного отображения в IDE.
## Атрибуты
В чистом виде мета-атрибуты не имеют применения, но могут быть использованы для генерации автоматической документации и используются в интеграциях с движками для задания отображения в отладочных инструментах и редакторах.
``` c#
using DCFApixels.DragonECS;

// Задает пользовательское название типа, по умолчанию используется имя типа.
[MetaName("SomeComponent")]

// Используется для группировки типов.
[MetaGroup("Abilities/Passive/")]

// Задает цвет типа в системе rgb, где каждый канал принимает значение от 0 до 255, по умолчанию белый. 
[MetaColor(MetaColor.Red)] // или [DebugColor(255, 0, 0)]
 
// Добавляет описание типу.
[MetaDescription("The quick brown fox jumps over the lazy dog")] 
 
// Добавляет строковые теги.
[MetaTags(...)]  // [MetaTags(MetaTags.HIDDEN))] чтобы скрыть в редакторе 
public struct Component { }
```
## EcsDebug
Имеет набор методов для отладки и логирования. Реализован как статический класс вызывающий методы Debug-сервисов. Debug-сервисы - это посредники между системами отладки среды и EcsDebug. Это позволяет не изменяя отладочный код проекта, переносить проект на другие движки, достаточно только реализовать специальный Debug-сервис.

По умолчанию используется `DefaultDebugService` который выводит логи в консоль. Для реализации пользовательского создайте класс наследуемый от `DebugService` и реализуйте абстрактные члены класса.

``` csharp
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
``` csharp
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

# Расширения
* [Автоматическое внедрение зависимостей](https://github.com/DCFApixels/DragonECS-AutoInjections)
* [Поддержка классической C# многопоточности](https://github.com/DCFApixels/DragonECS-ClassicThreads)
* Отношения (Work in progress)
* Интеграция с движком Unity (Work in progress)
<!--* Твое расширение? Если разрабатываешь свои расширения для DragonECS, дай знать и они будут добавлены сюда-->

</br>
 
# FAQ
## 'ReadOnlySpan<>' could not be found
В версии юнити 2020.1.х в консоли может выпадать ошибка:
```
The type or namespace name 'ReadOnlySpan<>' could not be found (are you missing a using directive or an assembly reference?)
``` 
Чтобы починить добавте директиву `ENABLE_DUMMY_SPAN` в `Project Settings/Player/Other Settings/Scripting Define Symbols`.

</br>

# Обратная связь
Discord для дискуссий [https://discord.gg/kqmJjExuCf](https://discord.gg/kqmJjExuCf)

</br></br></br>
