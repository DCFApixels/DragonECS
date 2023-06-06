<p align="center">
<img width="660" src="https://github.com/DCFApixels/DragonECS/assets/99481254/c09e385e-08c1-4c04-904a-36ad7e25e45b.png">
</p>

<p align="center">
<img alt="Version" src="https://img.shields.io/github/package-json/v/DCFApixels/DragonECS?color=%23ff4e85&style=for-the-badge">
<img alt="License" src="https://img.shields.io/github/license/DCFApixels/DragonECS?color=ff4e85&style=for-the-badge">
<!--<img alt="Discord" src="https://img.shields.io/discord/1111696966208999525?color=%23ff4e85&label=Discord&logo=Discord&logoColor=%23ff4e85&style=for-the-badge">-->
</p>

# DragonECS - C# Entity Component System Framework

| Languages: | [Русский](https://github.com/DCFApixels/DragonECS/blob/main/README-RU.md) | [English(WIP)](https://github.com/DCFApixels/DragonECS) |
| :--- | :--- | :--- |

Данный [ECS](https://en.wikipedia.org/wiki/Entity_component_system) Фреймворк нацелен на максимальную удобность, модульность, расширяемость и производительность динамического изменения сущностей. Без генерации кода и зависимостей.
> **NOTICE:** Проект в стадии разработки. API может меняться.  
> Readme еще не завершен

## Оглавление
* [Установка](#Установка)
  * [Unity-модуль](#Unity-модуль)
  * [В виде иходников](#В-виде-иходников)
  * [Версионирование](#Версионирование)
* [Основные концепции](#Основные-концепции)
  * [Entity](#Entity)
  * [Component](#Component)
  * [System](#System)
* [Концепции фреймворка](#Концепции-фреймворка)
  * [Пайплайн](#Пайплайн)
    * [Построение](#Построение)
    * [Внедрение зависимостей](#Внедрение-зависимостей)
    * [Модули](#Модули)
    * [Слои](#Слои)
  * [Процессы](#Процессы)
  * [Мир](#Мир)
  * [Группа](#Группа)
  * [Субъект](#Субъект)
  * [Запрос](#Запрос)
  * [Модули](#Модули)
* [Debug](#Debug)
  * [Debug-Атрибуты](#Debug-Атрибуты)
  * [Debug-Сервис](#Debug-Сервис)
* [Расширения](#Расширения)

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

# Основные концепции
## Entity
**Сущности** - это то к чему крепятся данные. Реализованы в виде идентификаторов, которых есть 2 вида:
* `int` - однократный идентификатор, применяется в пределах одного тика. Не рекомендуется хранить `int` идентификаторы, в место этого используйте `entlong`;
* `entlong` - долговременный идентификатор, содержит в себе полный набор информации для однозначной идентификации;
``` csharp
// Создание новой сущности в мире.
int entityID = _world.NewEmptyEntity();

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
> Компоненты-теги хоть и не имеют данных, само наличие или отсутствие компонента-тега у сущности уже несет информацию и может применяться для определения типа сущности.

## System
**Системы** - это основная логика, тут задается поведение сущностей. Существуют в виде пользовательских классов, реализующих как минимум один из интерфейсов процессов. Основные процессы:
```c#
class SomeSystem : IEcsPreInitProcess, IEcsInitProcess, IEcsRunProcess, IEcsDestroyProcess
{
    // Будет вызван один раз в момент работы EcsPipeline.Init() и до срабатывания IEcsInitProcess.Init()
    public void PreInit (EcsPipeline pipeline) { }
    
    // Будет вызван один раз в момент работы EcsPipeline.Init() и после срабатывания IEcsPreInitProcess.PreInit()
    public void Init (EcsPipeline pipeline)  { }
    
    // Будет вызван один раз в момент работы EcsPipeline.Run().
    public void Run (EcsPipeline pipeline) { }
    
    // Будет вызван один раз в момент работы EcsPipeline.Destroy()
    public void Destroy (EcsPipeline pipeline) { }
}
```
> Для реализации дополнительных процессов перейдите к разделу [Процессы](#Процессы).
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
> Есть упрощенная инициализация пайплайна сразу в момент построение, для используйте Builder.BuildAndInit();
### Внедрение зависимостей
Внедрение зависимостей - это процесс который запускается вместе с инициализацией пайплайна и внедряет данные переданные в Builder.
``` c#
EcsPipelone pipeline = EcsPipeline.New()
    //...
    .Inject(_someData) // Внедрит в системы экземпляр _someData
    //...
    .BuildAndInit();
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
    //...
    .BuildAndInit();
```
Встроенные слои расположены в следующем порядке:
* `EcsConst.PRE_BEGIN_LAYER`
* `EcsConst.BEGIN_LAYER`
* `EcsConst.BASIC_LAYER` (Если при добавблении системы не казать слой, то она будет доавблена сюда)
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
 
Для добавления нового процесса создайте интерфейс наследованный от `IEcsProcess` и создайте раннер для него. Раннер это класс реализующий интерфейс запускаемого процесса и наследуемый от EcsRunner<TInterface>. Пример:
 ```c#
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
С помощью компонентов можно прикреплять дополнительные данные к мирам. Компоненты можно применять создания расширений в связке с методами расширений.
``` csharp
WorldComponent component = _world.Get<WorldComponent>();
```
    
## Пул
Является контейнером для компонентов, предоставляет методы для добавления/чтения/редактирования/удаления компонентов на сущности. Есть несколько видов пулов, для разных целей
* `EcsPool` - универсальный пул, хранит struct-компоненты реализующие интерфейс IEcsComponent;
* `EcsTagPool` - подходит для хранения пустых компонентов-тегов, в сравнении с EcsPool имеет лучше оптимизацию памяти и действий с пулом, хранит в себе struct-компоненты реализующие IEcsTagComponent;
    
Имеется возможность реализации пользовательского пула
> эта функция будет описана в ближайшее время
 
## Субъект
Это пользовательские классы наследуемые от `EcsSubject`, которые используются как посредник для взаимодействия с сущностями. Субъекты одновременно являются кешем пулов и ограничением для фильтрации сущностей.
``` csharp
using DCFApixels.DragonECS;
...
class Subject : EcsSubject
{
    public EcsPool<Pose> poses;
    public EcsPool<Velocity> velocities;
 
    // вместо конструктора можно использовать виртуальную функцию Init(Builder b)
    public Subject(Builder b) 
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
В субъекты можно добавлять другие субъекты, тем самым комбинируя их. Ограничения так же будут скомбинированы
``` csharp
using DCFApixels.DragonECS;
...
class Subject : EcsSubject
{
    public OtherSubject1 someSubject1;
    public OtherSubject2 someSubject2;
    public EcsPool<Pose> poses;
 
    // функция Init аналогична конструктору Subject(Builder b)
    protected override void Init(Builder b)
    {
        // комбинирует с SomeSubject1.
        otherSubject1 = b.Combine<OtherSubject1>(1);
        // хотя для SomeSubject1 метод Combine был вызван раньше, сначала будет скомбинирован с OtherSubject2, так как по умолчанию order = 0.
        otherSubject2 = b.Combine<OtherSubject2>();
        // если в OtherSubject1 или в OtherSubject2 было ограничение b.Exclude<Pose>() тут оно будет заменено на b.Include<Pose>().
        poses = b.Include<Pose>();
    }
}
```
Если будут конфликтующие ограничения у комбинируемых субъектов, то новые ограничения будут заменять добавленные ранее. Базовые ограничения всегда заменяют ограничения из добавленных субъектов. Визуальный пример комбинации ограничений:
| | cmp1 | cmp2 | cmp3 | cmp4 | cmp5 | разрешение конфликтных ограничений|
| :--- | :--- | :--- | :--- | :--- | :--- |:--- |
| OtherSubject2 | :heavy_check_mark: | :x: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | |
| OtherSubject1 | :heavy_minus_sign: | :heavy_check_mark: | :heavy_minus_sign: | :x: | :heavy_minus_sign: | Для `cmp2` будет выбрано :heavy_check_mark: |
| Subject | :x: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | Для `cmp1` будет выбрано :x: |
| Итоговые ограничения | :x: | :heavy_check_mark: | :heavy_minus_sign: | :x: | :heavy_check_mark: | |
 

## Запросы
Используйте метод-запрос `EcsWorld.Where<TSubject>(out TSubject subject)` для получения необходимого системе набора сущностей. Запросы работают в связке с субъектами, субъекты определяют ограничения запросов, результатом запроса становится группа сущностей удовлетворяющая условиям субъекта. По умолчанию запрос делает выборку из всех сущностей в мире, но так же можно сделать выборку из определенной группы сущностей, для этого используйте `EcsWorld.WhereFor<TSubject>(EcsReadonlyGroup sourceGroup, out TSubject subject)`
 
## Группа
Группы это структуры данных для хранения множества сущностей с быстрыми операциями добавления/удаления/проверки наличия и т.д. Реализованы классом `EcsGroup` и структурой `EcsReadonlyGroup`. 

``` c#
//Получем новую группу. EcsWorld содержит в себе пул групп,
//поэтому будет создана новая или переиспользована свободная.
EcsGroup group = EcsGroup.New(_world);
//Освобождаем группу.
group.Release();
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
Так как группы это множества, они содержат операции над множествами. Каждый метод имеет 2 варианта, с записью результата в groupA, либо с возвращением новой группы:

<details>
<summary>Визуализация методов</summary>
 
![Визуализация методов группы](https://github.com/DCFApixels/DragonECS/assets/99481254/f2c85a9f-949c-4908-9a02-acc3c883a22b)

</details>                    
                                
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
 
# Debug
Фреймворк предоставляет дополнительные инструменты для отладки и логирования, не зависящие от среды.
## Debug-Атрибуты
В чистом виде дебаг-атрибуты не имеют применения, но используются в интеграциях с движками для задания отображения в отладочных инструментах и редакторах.
``` c#
using DCFApixels.DragonECS;

// Задает пользовательское название типа, по умолчанию используется имя типа.
[DebugName("SomeComponent")] 
 
// Задает цвет типа в системе rgb, где каждый канал принимает значение от 0 до 255, по умолчанию белый. 
[DebugColor(DebugColor.Red)] // или [DebugColor(255, 0, 0)]
 
// Добавляет описание типу.
[DebugDescription("The quick brown fox jumps over the lazy dog")] 
 
// Скрывает тип.
[DebugHide] 
public struct Component { }
```
## EcsDebug
Имеет набор методов для отладки и логирования. Реализован как статический класс вызывающий методы Debug-сервисов. Debug-сервисы являются посредниками между системами отладки среды и EcsDebug. Это позволяет не изменяя отладочный код проекта, переносить его на другие движки, достаточно только реализовать специальный Debug-сервис.

По умолчанию используется `DefaultDebugService` который выводит логи в консоль. Для реализации пользовательского создайте класс наследуемый от `DebugService` и реализуйте абстрактные члены класса.
# Расширения
* [Автоматическое внедрение зависимостей](https://github.com/DCFApixels/DragonECS-AutoInjections)
* Интеграция с движком Unity (Work in progress)
