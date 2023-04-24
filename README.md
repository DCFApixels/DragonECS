
<p align="center">
<img src="https://user-images.githubusercontent.com/99481254/228309579-729cc600-af83-41e2-8474-96fb96859ae6.png">
</p>

# DragonECS - C# Entity Component System Framework

> **ВАЖНО!** Проект в стадии разработки. API может меняться. README так же не завершен.

# Основные концепции
## Сущьность
Сущьности реализованы двумя типами:
* `int` - явялется временным идентификатором, применяется в пределах одного тика
* `EcsEntity` - долговременный идентификатор сущьности, хранит в себе полный набор информации для однозначной идентификации

## Компонент
Компоненты - это даные для сущьностей. Могут быть тольно struct и обязаны наследовать один из интерфейсов который определяют тип компонента. самый базовый IEcsComponent.
```c#
struct Health : IEcsComponent
{
    public float health;
    public int armor;
}
```
## Система
Системы - это основная логика, тут задается поведение сущьностей. Существуют в виде пользовательских классов, реализующих как минимум один из IEcsInitSystem, IEcsDestroySystem, IEcsRunSystem интерфейсов.
```c#
class UserSystem : IEcsPreInitSystem, IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem 
{
    public void PreInit (EcsSession session) {
        // Будет вызван один раз в момент работы EcsSession.Init() и до срабатывания IEcsInitSystem.Init()
    }
    public void Init (EcsSession session) {
        // Будет вызван один раз в момент работы EcsSession.Init() и после срабатывания IEcsPreInitSystem.PreInit()
    }
    public void Run (EcsSession session) {
        // Будет вызван один раз в момент работы EcsSession.Run().
    }
    public void Destroy (EcsSession session) {
        // Будет вызван один раз в момент работы EcsSession.Destroy() и до срабатывания IEcsPostDestroySystem.PostDestroy()
    }
    
    //Для реализации дополнительных сообщений используйте Раннеры
}
```

## Pipeline
Является двжиком систем, определяя поочередность их вызова и предоставляющий механизм для сообщений между системами.

## Раннеры/Сообщения
Раннеры это система сообщений для систем. Может использоваться для создания реактивного поведения или для управления очередью вызовов систем. Сообщения это просто интерфейсы наследуемые от IEcsSystem, чтобы интерфейс работал как сообщение нужно реализоват соотсветующий раннер. 
Сообщения реализованные по умолчанию:
`IEcsPreInitSystem`, `IEcsInitSystem`, `IEcsRunSystem`, `IEcsDestroySystem` - сообщения жизненого цикла Pipeline
`IEcsPreInject`, `IEcsInject<T>` - сообщения системы внедрения зависимостей для Pipeline. Через них прокидываются зависимости
`IEcsPreInitInjectCallbacks` - Так же сообщение системы внедрения зависимостей, но работает в пределах до сообщения IEcsInitSystem, сигнализирует о инициализации предварительных внедрений и окончании.

## Реализация Раннеров и сообщений
Новые сообщения реализуются через интефейсы наследованные от IEcsSystem, например, стандартное IEcsRunSystem.Run(EcsSession session) запускается у всех систем при помощи Runner-а. Runner-а можно получить вызыввав EcsSession.GetRunner<TInterface>().
Однако чтобы этто метод вернул нужный Runner, нужно его реализовать, вот пример реаилзации Runner-а для IEcsRunSystem:
 ```c#
public sealed class EcsRunRunner : EcsRunner<IEcsRunSystem>, IEcsRunSystem
{
    void IEcsRunSystem.Run(EcsSession session)
    {
        foreach (var item in targets) item.Run(session);
    }
}
```
