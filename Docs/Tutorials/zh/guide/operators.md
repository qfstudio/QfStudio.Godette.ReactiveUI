# 帧级操作符

由 `SceneTree.ProcessFrame` 驱动的帧感知响应式操作符：

```csharp
this.WhenActivated(d =>
{
    // Emit every frame (or pass RxSchedulers.PhysicsFrameScheduler for physics frame)
    Observable.EveryUpdate()
        .Subscribe(_ => { /* per-frame work */ })
        .DisposeWith(d);

    // Delay by frames
    Observable.AfterFrame(0)
        .DelayFrame(30)
        .Subscribe(_ => { /* fires after 30 frames */ })
        .DisposeWith(d);

    // Emit once after N frames, then every M frames (interval in frames)
    Observable.IntervalFrame(60)
        .Subscribe(_ => { /* every 60 frames */ })
        .DisposeWith(d);

    // Emit a single value after N frames
    Observable.ReturnFrame("ready", 30)
        .Subscribe(msg => { /* fires after 30 frames */ })
        .DisposeWith(d);

    // Debounce: emit after 30 frames of silence
    input.DebounceFrame(30)
        .Subscribe(value => { /* ... */ })
        .DisposeWith(d);

    // Throttle: emit first value per 60-frame window
    input.ThrottleFirstFrame(60)
        .Subscribe(value => { /* ... */ })
        .DisposeWith(d);

    // Chunk: collect values and emit a list every 30 frames
    input.ChunkFrame(30)
        .Subscribe(batch => { /* IList<T> */ })
        .DisposeWith(d);

    // Poll a property every frame, emit on change
    Observable.PollEveryUpdate(this, v => v.FreeIcon.Position)
        .Subscribe(pos => { /* ... */ })
        .DisposeWith(d);
});
```
