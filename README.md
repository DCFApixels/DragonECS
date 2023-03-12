# DragonECS - C# Entity Component System фреймворк

> **ВАЖНО!** Проект в стадии разработки. README так же сейчас сущесвуте в виде наброска

# Основные концепции
## Сущьность
Сущьности реализованы двумя типами:
* `ent` - Представляет собой уникальный идентификатор сущьности, ее поколение и идентификатор мира. Так же содержит в себе методы для работы с компонентами. Обычно вам нужно работать только с этим типом.
* `Entity` - Испольщуются в перечислениях групп сущьностей, идентичен ent/

## Компонент
Компоненты - это даные для сущьностей. Могут быть тольно struct или значимого типа. Не должны содержать логики, только данные
```c#
struct Health {
    public float health;
    public int armor;
}
```
## Система
Системы - это основная логика, тут задается поведение сущьностей. Существуют в виде пользовательских классов, реализующих как минимум один из IEcsInitSystem, IEcsDestroySystem, IEcsRunSystem интерфейсов.
```c#
class UserSystem : IEcsPreInitSystem, IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem {
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

## Сессия
Является контейнером и контроллером систем и миров. Реализована классом `EcsSession`. Каждую сессию можно рассматривать как отдельную, по разному конфигурируемую, программу

## Раннеры
Раннеры это система сообщений для систем. Может использоваться для создания реактивного поведения или для управления очередью вызовов систем. Cообщения реализуются через интефейсы наследованные от IEcsSystem, например, стандартное IEcsRunSystem.Run(EcsSession session) так же явялется сообщением, а запускается оно у всех систем при помощи Runner-а. Runner-а можно получить вызыввав EcsSession.GetRunner<TInterface>().
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
