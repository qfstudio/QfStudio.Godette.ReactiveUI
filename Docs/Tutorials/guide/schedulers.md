# Schedulers

## Overview

`QfStudio.Godette.ReactiveUI` provides three schedulers that bridge Godot's frame loop to the Rx scheduling model:

| Scheduler | Alias | Source | Purpose |
|---|---|---|---|
| `GodotMainThreadScheduler` | `RxSchedulers.MainThreadScheduler` | `SynchronizationContext` | Main thread callbacks |
| `GodotSchedulers.ProcessFrameScheduler` | — | `_Process` | Per-frame scheduling |
| `GodotSchedulers.PhysicsFrameScheduler` | — | `_PhysicsProcess` | Physics frame scheduling |

## Initialization in Autoload

Create scheduler instances and register them in `RxAppBootstrapper`:

```csharp
var scheduler = GodotMainThreadScheduler.Create(SynchronizationContext.Current!);
GodotSchedulers.MainThreadScheduler = scheduler;
GodotSchedulers.ProcessFrameScheduler = _processFrameScheduler;
GodotSchedulers.PhysicsFrameScheduler = _physicsFrameScheduler;
```

`RxAppBuilder.BuildApp()` mirrors the scheduler registered via `.WithMainThreadScheduler(scheduler)` to `RxSchedulers.MainThreadScheduler`, so `ObserveOn(RxSchedulers.MainThreadScheduler)` resolves to the same `GodotMainThreadScheduler`.

## Frame Driving

```csharp
public override void _Process(double delta)
{
    _processFrameScheduler.NotifyProcess(delta);
}

public override void _PhysicsProcess(double delta)
{
    _physicsFrameScheduler.NotifyProcess(delta);
}
```

Calling `NotifyProcess` each frame drives the frame scheduler to execute queued operations. `GodotSchedulers` is the Godot-side alias for the same instances, used by frame operators and other Godot-specific APIs.
