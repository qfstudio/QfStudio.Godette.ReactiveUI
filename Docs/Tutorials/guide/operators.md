# Frame Operators

Frame-aware reactive operators powered by `SceneTree.ProcessFrame`:

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

> [!NOTE]
> **`EveryUpdate` does not guarantee parent-before-child ordering -- and this is intentional.** Unlike Godot's native `_Process`, which runs parents before children by tree order, `EveryUpdate` callbacks run in subscription order. Guaranteeing tree order would force the scheduler to track each subscription's owning node and re-sort work items every frame -- a per-frame cost for a guarantee most subscriptions never need -- and would couple the Rx stream abstraction to the scene tree. If your logic depends on parent/child ordering, override `_Process`/`_PhysicsProcess` on the nodes instead; Godot provides that ordering for free.
