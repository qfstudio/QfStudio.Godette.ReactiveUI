# 调度器

## 概述

`QfStudio.Godette.ReactiveUI` 提供三个调度器，将 Godot 的帧循环桥接到 Rx 调度模型：

| 调度器 | 别名 | 来源 | 用途 |
|---|---|---|---|
| `GodotMainThreadScheduler` | `RxSchedulers.MainThreadScheduler` | `SynchronizationContext` | 主线程回调 |
| `GodotSchedulers.ProcessFrameScheduler` | — | `_Process` | 每帧调度 |
| `GodotSchedulers.PhysicsFrameScheduler` | — | `_PhysicsProcess` | 物理帧调度 |

## Autoload 中的初始化

创建调度器实例并在 `RxAppBootstrapper` 中注册：

```csharp
var scheduler = GodotMainThreadScheduler.Create(SynchronizationContext.Current!);
GodotSchedulers.MainThreadScheduler = scheduler;
GodotSchedulers.ProcessFrameScheduler = _processFrameScheduler;
GodotSchedulers.PhysicsFrameScheduler = _physicsFrameScheduler;
```

`RxAppBuilder.BuildApp()` 会将 `.WithMainThreadScheduler(scheduler)` 注册的调度器镜像到 `RxSchedulers.MainThreadScheduler`，因此 `ObserveOn(RxSchedulers.MainThreadScheduler)` 解析到同一个 `GodotMainThreadScheduler`。

## 帧驱动

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

每帧调用 `NotifyProcess` 推动帧调度器执行排队的操作。`GodotSchedulers` 是这些相同实例在 Godot 侧的别名，供帧运算符和其他 Godot 专用 API 使用。
