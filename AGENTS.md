# DragonECS Agent Notes

DragonECS is a finished C# ECS framework. In user projects, treat it as a
plugin/library: use it, do not modify it, unless the user explicitly asks to
change DragonECS itself. If unsure about API details, read `README.md`.

## First Rule

- Write user code: components, systems, aspects, modules, ECS root/bootstrap.
- Do not patch DragonECS package source for ordinary gameplay/app tasks.
- Do not copy framework internals into user code.
- Do not rename framework APIs or change package metadata.

## Optional Extensions

When optional DragonECS extensions are relevant to the current task, ask the
user at most once per project whether to add them. Do not install them silently.

- Unity integration: `DragonECS-Unity`
  (`https://github.com/DCFApixels/DragonECS-Unity`). Suggest only when working
  in a Unity project and the integration package is not already available.
- Dependency autoinjections: `DragonECS-AutoInjections`
  (`https://github.com/DCFApixels/DragonECS-AutoInjections`). Optional
  Reflection-based way to reduce manual injection boilerplate.
- Simplified Syntax:
  `https://gist.github.com/DCFApixels/d7bfbfb8cb70d141deff00be24f28ff0`.
  Offer only if the user wants a simpler, more object-oriented style such as
  `ref var cmp = ref entity.Get<T>()`.
- Other extensions are listed in the `Extensions` section of `README.md`.

## Namespaces

- `DCFApixels.DragonECS`: main public API; default choice.
- `DCFApixels.DragonECS.Core`: public low-level API for advanced extensions.
- `DCFApixels.DragonECS.Core.Unchecked`: public unsafe-like low-level API; avoid
  unless the task clearly needs it and invariants are manually preserved.
- `DCFApixels.DragonECS.Core.Internal`: implementation detail; do not use in
  normal user code.

Default import:

```csharp
using DCFApixels.DragonECS;
```

## Worlds

- `EcsWorld` stores entities/components and is enough for one-world pipelines.
- `EcsDefaultWorld` and `EcsEventWorld` behave like `EcsWorld`; they exist as
  built-in strongly typed DI markers.
- Always call `Destroy()` on worlds and pipelines when done.

Typical root:

```csharp
_world = new EcsWorld();
_pipeline = EcsPipeline.New()
    .Inject(_world)
    .Add(new SomeSystem())
    .BuildAndInit();

// update:
_pipeline.Run();

// cleanup:
_pipeline.Destroy();
_world.Destroy();
```

## Components And Pools

Use data-only structs:

- `IEcsComponent` -> `EcsPool<T>`
- `IEcsTagComponent` -> `EcsTagPool<T>`
- `IEcsValueComponent` -> `EcsValuePool<T>` for unmanaged/job-friendly data

```csharp
public struct Health : IEcsComponent { public float Value; }
public struct PlayerTag : IEcsTagComponent { }
public struct Position : IEcsValueComponent { public float X, Y; }
```

Pool basics:

```csharp
var healths = world.GetPool<Health>();
ref var health = ref healths.Add(e);
if (healths.Has(e)) { ref var h = ref healths.Get(e); }
healths.Del(e);
```

In Release, invalid `Add`/`Get`/`Del` checks may be removed. Prefer `Has`,
`TryAddOrGet`, and `TryDel` when the component presence is uncertain.

Entity invariant: entities cannot exist without components; removing the last
component deletes the entity.

## Systems And DI

Systems are classes implementing process interfaces:

- `IEcsInit`
- `IEcsRun`
- `IEcsDestroy`
- `IEcsInject<T>` for dependencies

```csharp
public sealed class SomeSystem : IEcsRun, IEcsInject<EcsWorld>
{
    private EcsWorld _world;
    public void Inject(EcsWorld world) { _world = world; }
    public void Run() { }
}
```

Pass dependencies via `EcsPipeline.New().Inject(value)`. Prefer constructor-free
systems when using DragonECS injection.

## Aspects And Queries

Use aspects for query masks and cached pools:

```csharp
private class Aspect : EcsAspect
{
    public EcsPool<Health> Healths = Inc;
    public EcsTagPool<PlayerTag> Players = Opt;
}

foreach (int e in world.Where(out Aspect a))
{
    ref var health = ref a.Healths.Get(e);
}
```

Markers:

- `Inc`: must have component.
- `Exc`: must not have component.
- `Any`: must have at least one marked component.
- `Opt`: cache pool only; no filter.

Use `WhereToGroup` for stable cached results. Use `WhereUnsafe` only for
low-level/native workflows such as Unity Jobs.

## Entities

- Use `int` entity ids only in short-lived local code.
- Use `entlong` for storage across frames, events, async boundaries, or external
  references.
- Prefer unpack overloads with the expected world or mask.

```csharp
entlong handle = world.GetEntityLong(e);

if (handle.TryGetID(out int aliveEntity)) { }
if (handle.TryUnpack(mask, out int matchedEntity)) { }
```

## Modules And Ordering

Group feature systems with `IEcsModule`:

```csharp
public sealed class GameplayModule : IEcsModule
{
    public void Import(EcsPipeline.Builder b)
    {
        b.Add(new InputSystem());
        b.Add(new MovementSystem());
    }
}
```

Use layers and `sortOrder` for ordering. Built-in layers:
`PRE_BEGIN_LAYER`, `BEGIN_LAYER`, `BASIC_LAYER`, `END_LAYER`,
`POST_END_LAYER`. Lower `sortOrder` runs earlier.

## Events And Requests

Recommended convention for component messages between systems:

- `*Event`: created by one system, read by many; cleaned by the creator or at
  end of update.
- `*Request`: created by many systems, handled by one; cleaned by the handler or
  at end of update.
- `*Answer`: optional response for a request; follows the same naming rules.
- Add `Self` before `Event`/`Request`/`Answer` when the message is attached to
  the target entity itself.
- Use plain `Event`/`Request`/`Answer` when the target is stored in a field.

```csharp
public struct DamageSelfRequest : IEcsComponent { public float Damage; }
public struct DamageRequest : IEcsComponent { public entlong Target; public float Damage; }
public struct DamagedEvent : IEcsComponent { public float AppliedDamage; }
```

## Metadata And Debug

Meta attributes (`MetaName`, `MetaGroup`, `MetaColor`, etc.) are for tools,
inspectors, debug views, and docs. Add them in user projects only when the user
asks or the project already uses that convention.

Use `EcsDebug` for reusable ECS logging; otherwise follow the project's logging
style.

## Multithreading

Read-only iteration can run in parallel only when no structural changes happen.
Structural changes include creating/deleting entities, adding/removing
components, clearing pools, and mutating active groups.

For Unity Jobs-style code, prefer unmanaged `IEcsValueComponent`,
`EcsValuePool<T>.AsNative()`, and `WhereUnsafe`.

## Avoid

- Using `Core.Internal` in user code.
- Using `Core.Unchecked` for convenience.
- Storing raw `int` entity ids long-term.
- Calling pool `Get` before proving the component exists.
- Editing framework arrays, masks, or registrars directly.
- Structural changes during active query iteration unless the flow is known
  safe; cache to a group when unsure.
- Adding metadata attributes without user/project need.
- Forgetting `Destroy()` on worlds/pipelines.

## If Asked To Modify DragonECS

Switch to contributor mode: read `README.md` and relevant `src/` files, preserve
C# 7.3/Unity/package compatibility, keep `.meta` files, and validate with:

```powershell
dotnet build DragonECS.csproj
```
