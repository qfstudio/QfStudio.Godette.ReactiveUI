# Developer Docs

## Limitations for Godot

- In the current version of Godot, any C# class that inherits from `Godot.Node` is treated as a script resource. As a result, all such code files must be placed within the Godot project's source directory. This means that creating a third‑party library containing classes that derive from `Godot.Node` will not work.
- From the engine's perspective, every script resource must have a unique path within the source directory. Consequently, generic types that inherit from `Godot.Node` are not supported.

## ReactiveUI Integration

Generally, it takes 4 steps to implement `ReactiveUI` for a custom platform.

- Implement `IScheduler` for the UI thread.
- Implement `IActivationForViewFetcher` for view activation.
- Implement `ICreatesObservableForProperty` for property-change notification.
- Implement `ICreatesCommandBinding` for command binding.

QfStudio.Godette.ReactiveUI implements all of the above for the Godot Engine.

### Activation Semantics

Activation (`true`) means the node is in the scene tree **and** ready (all children initialized). This is semantically equivalent to Avalonia's `AttachedToVisualTree` / `DetachedFromVisualTree`.

Three paths emit `true`:
- `Ready` — first entry, all children initialized
- `TreeEntered` + `IsNodeReady()` — re-entry (node was previously ready)
- Initial check `IsInsideTree() && IsNodeReady()` — already in tree at subscription time

## Frame-based Operator Design

Coding frame-based operators is a mindscrew. Several principles must be followed:

1. **Following the Rx.NET Contract** `(OnNext)*(OnCompleted|OnError)?` The implementation **must** folow the Rx.NET contract. Assume that upstream operators follow the contract.
2. **Thread-safety for Producer and Consumer Threads** The implementation **must** guarantee thread-safe interaction between the producer (`OnNext`, `OnError`, `OnCompleted`) and the consumer (`MoveNextCore`). However, by the Rx.NET contract, the implementation generally does not need to guarantee serialize concurrent producer invocations. If multiple threads call `OnNext` simultaneously, external synchronization (e.g., a lock or `Observable.Synchronize`) is required to prevent interleaved notifications, as required by the Rx grammar.
    - **Single-threaded Emission** All observer notifications (`OnNext`/`OnCompleted`/`OnError`) must only occur inside `MoveNextCore`, which runs on the main thread. Upstream callbacks should only update internal state, but never notify the observer directly. It eliminates cross-thread notification races.
3. **Operators must be cleaned-up properly**
