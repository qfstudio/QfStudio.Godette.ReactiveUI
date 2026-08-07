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

### View Reactivity

A Godot view becomes reactive in one of two ways:

- **Handwritten view base** — `public abstract partial class ViewBase : Node, IReactiveObject` (with the four members), with views deriving from it. This suits sharing `[Reactive]` properties and reusable child controls that have no ViewModel.
- **`[GodotViewFor<TViewModel>]`** — the Godot equivalent of WPF's `ReactiveUserControl<TViewModel> : UserControl, IViewFor<TViewModel>`. Since Godot does not support generic `Node` subclasses (see *Limitations for Godot*), the pattern is expressed through a source generator instead of a generic base class: it adds `IViewFor<TViewModel>`, `IActivatableView` and `IReactiveObject`, plus a push-observable `ViewModel` property.

The two compose: a `[GodotViewFor<TViewModel>]` view may derive from such a base, and the generator then omits the `IReactiveObject` members the base already provides.

#### Public contract

The shapes of userland view code this design supports:

- `[Reactive][Export] public partial T Prop { get; set; }` — an observable, inspector-editable view property in one line (dedup and change notification on a single storage).
- An observed user-declared property must raise change notifications (`[Reactive]` or a manual `RaiseAndSetIfChanged`); a plain CLR property that never raises is silently unobservable.
- Engine-declared properties (`Position`, `Size`, `Visible`, ...) need no signal and no `[Reactive]`: frame polling observes them automatically, at most once per frame.
- A reusable child control without a ViewModel (e.g. an item view inside `ItemsBinder`) opts into the same contract by implementing `IReactiveObject` — via the `[IReactiveObject]` attribute (`ReactiveUI.SourceGenerators`) or the four hand-written members.

#### Rationale 

WPF (`DependencyProperty`) and Avalonia (`StyledProperty`/`DirectProperty`) give the platform first-class observable view properties; Godot has no equivalent. A view property must therefore either raise its own change notifications (`IReactiveObject`/`INotifyPropertyChanged` or a Godot signal) or be observed by fixed-interval polling. Making the view `IReactiveObject` provides the push half, while engine-declared properties fall back to the polling half. Two alternatives were considered and rejected: no notification interface on views (loses View-layer `[Reactive]` and push `ViewModel`), and `INotifyPropertyChanged`-only (the same claiming problem, fewer capabilities).

#### Property Binders and Affinities

`IReactiveObject` claims the whole view type by affinity, shadowing the polling fallback for properties that never raise notifications (on a tie, the first registered binder wins):

| Affinity | Binder | Claims |
|----------|--------|--------|
| 15 | `GodotPropertyBinder` | built-in control properties observed via their signals |
| 12 | `GodotPollBasedPropertyBinder` | any `GodotObject` property that needs polling — engine-declared properties (`Position`, `Size`, `Visible`) on `IReactiveObject`/`INotifyPropertyChanged` objects, and all properties on other Godot objects |
| 10 | `IROObservableForProperty` | `IReactiveObject` user-declared properties (`[Reactive]` / `ViewModel`) — push |
| 5 | `INPCObservableForProperty` | `INotifyPropertyChanged` types |
| 1 | `POCOObservableForProperty` | fallback: emits the current value once |

## Frame-based Operator Design

Coding frame-based operators is a mindscrew. Several principles must be followed:

1. **Following the Rx.NET Contract** `(OnNext)*(OnCompleted|OnError)?` The implementation **must** folow the Rx.NET contract. Assume that upstream operators follow the contract.
2. **Thread-safety for Producer and Consumer Threads** The implementation **must** guarantee thread-safe interaction between the producer (`OnNext`, `OnError`, `OnCompleted`) and the consumer (`MoveNextCore`). However, by the Rx.NET contract, the implementation generally does not need to guarantee serialize concurrent producer invocations. If multiple threads call `OnNext` simultaneously, external synchronization (e.g., a lock or `Observable.Synchronize`) is required to prevent interleaved notifications, as required by the Rx grammar.
    - **Single-threaded Emission** All observer notifications (`OnNext`/`OnCompleted`/`OnError`) must only occur inside `MoveNextCore`, which runs on the main thread. Upstream callbacks should only update internal state, but never notify the observer directly. It eliminates cross-thread notification races.
3. **Operators must be cleaned-up properly**
