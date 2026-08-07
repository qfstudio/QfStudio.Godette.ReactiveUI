# Reactive View Properties

The examples so far observe the *ViewModel*. A node can also expose observable properties of its own -- useful for reusable custom nodes that have no ViewModel of their own (e.g. an item view inside `ItemsBinder`, or a self-contained widget).

## How a view property becomes observable

Because Godot has no equivalent of WPF's `DependencyProperty` or Avalonia's `StyledProperty`, a view property becomes observable in one of two ways:

1. **It raises its own change notifications.** Making the node an `IReactiveObject` unlocks this path: `[Reactive]` properties emit change notifications that `WhenAnyValue` and friends pick up.
2. **It is polled every frame.** Engine-declared properties that have no notification signal of their own (like `Position`, `Rotation`, `Size`) fall back to this automatically.

The two paths are transparent to your observation code -- `WhenAnyValue(x => x.ClickCount)` and `WhenAnyValue(x => x.Position)` look identical; only the underlying mechanism differs.

## Example: a self-contained counter widget

Here is a reusable node with no ViewModel that observes both kinds of properties:

```csharp
// usings: Godot, ReactiveUI, ReactiveUI.SourceGenerators

[SceneTree(root: "_root")]
[IReactiveObject]  // from ReactiveUI.SourceGenerators -- generates the four IReactiveObject members
public partial class CustomNode : Control, IActivatableView
{
    // Observable AND inspector-editable
    [Reactive]
    [Export]
    public partial int ClickCount { get; set; }

    public CustomNode()
    {
        this.WhenActivated(d =>
        {
            // User-declared [Reactive] property
            this.WhenAnyValue(x => x.ClickCount)
                .Subscribe(count => CountLabel.Text = $"ClickCount: {count}")
                .DisposeWith(d);

            // Engine-declared property
            this.WhenAnyValue(x => x.Position)
                .Subscribe(pos => PositionLabel.Text = $"position: {pos}")
                .DisposeWith(d);
        });
    }

    public override void _Ready()
    {
        IncrementButton.Pressed += () => ClickCount++;
    }
}
```

What is going on here:

- `[IReactiveObject]` generates the four `IReactiveObject` members, turning the node into a change-notification source.
- `[Reactive]` makes `ClickCount` raise change notifications; `[Export]` additionally exposes it in the Godot inspector. The two can be combined freely.
- `Position` is an engine-declared property without its own notification signal, so the library polls it every frame (at most one frame of latency).
- `IActivatableView` is a marker interface -- it is all `WhenActivated` needs, so a node can use the activation lifecycle without a ViewModel. Views declared with `[GodotViewFor<T>]` already implement `IReactiveObject` and `IActivatableView`, so they support `[Reactive][Export]` out of the box.

## The silently unobservable property

On an `IReactiveObject` or `INotifyPropertyChanged` node, a plain CLR property is silently unobservable: if you observe a user-declared property that never raises change notifications, you get the initial value and nothing more -- no error, no updates. Always use `[Reactive]` or a manual `RaiseAndSetIfChanged` for user-declared properties you want to observe.

```csharp
// Wrong: observes the initial value only, never updates
public int Count { get; set; }

// Right: emits change notifications
[Reactive] public partial int Count { get; set; }
```

## Getting `IReactiveObject` onto a node

Either works and they are interchangeable:

```csharp
// 1. [IReactiveObject] attribute (ReactiveUI.SourceGenerators)
[IReactiveObject]
public partial class CustomNodeA : Control, IActivatableView { /* ... */ }
```

```csharp
// 2. Hand-written
public partial class CustomNodeB : Control, IReactiveObject, IActivatableView
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
    void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);
}
```

A shared abstract base carrying the `IReactiveObject` members is the third option. This comes closest to Avalonia, where a custom view inherits a framework-provided base class -- e.g. `class MyView : ReactiveUserControl<MyViewModel>`, with `IViewFor<T>` already implemented by the base. It is rarely practical in Godot, though: C# has no multiple inheritance, Godot does not support generic classes in script resources, and a view must derive from a Godot node, leaving no room to also inherit the shared abstract base. In practice, prefer one of the first two options, combined with the source generator and `[GodotViewFor<T>]`.
