# 帧运算符

由 `SceneTree.ProcessFrame` 驱动的帧感知响应式运算符（frame operators）：

```csharp
this.WhenActivated(d =>
{
    // 每帧触发（传入 RxSchedulers.PhysicsFrameScheduler 可改为物理帧）
    Observable.EveryUpdate()
        .Subscribe(_ => { /* 每帧执行 */ })
        .DisposeWith(d);

    // 延迟 N 帧
    Observable.AfterFrame(0)
        .DelayFrame(30)
        .Subscribe(_ => { /* 30 帧后触发 */ })
        .DisposeWith(d);

    // N 帧后触发一次，此后每 M 帧触发一次
    Observable.IntervalFrame(60)
        .Subscribe(_ => { /* 每 60 帧 */ })
        .DisposeWith(d);

    // 延迟 N 帧后发出单个值
    Observable.ReturnFrame("ready", 30)
        .Subscribe(msg => { /* 30 帧后触发 */ })
        .DisposeWith(d);

    // 防抖：静默 30 帧后发出
    input.DebounceFrame(30)
        .Subscribe(value => { /* ... */ })
        .DisposeWith(d);

    // 节流：每个 60 帧窗口内仅发出首个值
    input.ThrottleFirstFrame(60)
        .Subscribe(value => { /* ... */ })
        .DisposeWith(d);

    // 分批：收集值，每 30 帧发出一个列表
    input.ChunkFrame(30)
        .Subscribe(batch => { /* IList<T> */ })
        .DisposeWith(d);

    // 每帧轮询属性，值变化时发出
    Observable.PollEveryUpdate(this, v => v.FreeIcon.Position)
        .Subscribe(pos => { /* ... */ })
        .DisposeWith(d);
});
```

> [!NOTE]
> **`EveryUpdate` 不保证"父先于子"的调用顺序——这是刻意为之。** 与 Godot 原生 `_Process`（按树序默认父先于子）不同，`EveryUpdate` 的回调按订阅顺序执行。若要保证树序，调度器就得为每个订阅追踪其所属节点并在每帧重排工作项——为大多数订阅用不到的顺序付出每帧成本，还会把 Rx 流抽象耦合到场景树上。若逻辑确实依赖父子顺序，请在节点上覆写 `_Process`/`_PhysicsProcess`，Godot 会免费保证该顺序。
