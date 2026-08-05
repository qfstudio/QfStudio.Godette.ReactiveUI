# Core Concepts

## What is QfStudio.Godette.ReactiveUI

[ReactiveUI](https://www.reactiveui.net/) is a composable, cross-platform MVVM (Model-View-ViewModel) framework for .NET. It uses reactive extensions to bind UI elements to ViewModel properties and commands, keeping views and business logic cleanly separated.

`QfStudio.Godette.ReactiveUI` provides the platform services that make ReactiveUI work with Godot Engine - scheduling, activation, property-change notification, and command binding. If you have used ReactiveUI with Avalonia or WPF, this is the same `this.Bind` / `this.BindCommand` / `WhenActivated` story, now wired to Godot nodes and signals.

## Activation Semantics

A view is activated (`true`) when its Godot `Node` is inside the scene tree **and** `IsNodeReady()` returns `true`. The `GodotActivationFetcher` emits `true` from three paths:

- the `Ready` signal (first entry, all children initialized);
- `TreeEntered` + `IsNodeReady()` (re-entry after the node was already ready);
- an initial `IsInsideTree() && IsNodeReady()` check at subscription time.

It emits `false` on `TreeExited`. This is semantically equivalent to Avalonia's `AttachedToVisualTree` / `DetachedFromVisualTree`.

Note: the C# virtual method `_Ready` runs **before** the `Ready` signal is emitted, so a `ViewModel` assigned in `_Ready` is already set by the time `WhenActivated` fires.

## `usings`

All examples in this guide assume these usings are in scope:

```csharp
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;
using System.Reactive.Disposables; // for DisposeWith(d) used throughout
```

`[GodotViewFor<T>]` and the generated `ViewModel` property are emitted by this library's bundled source generator into the `QfStudio.Godette.ReactiveUI` namespace -- no extra `using` or package reference is required.

## Basic Setup

A ViewModel implements `IActivatableViewModel`. A View uses the `[GodotViewFor<T>]` source generator attribute to implement `IViewFor<T>`. Bind in the constructor inside `WhenActivated`:

```csharp
// ViewModel
public partial class MyViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    [Reactive] public partial string Name { get; set; } = "";
}

// View (.tscn root script)
[GodotViewFor<MyViewModel>]
public partial class MyScene : Control
{
    public MyScene()
    {
        this.WhenActivated(d =>
        {
            this.Bind(ViewModel, vm => vm.Name, v => v.NameEdit.Text)
                .DisposeWith(d);
        });
    }

    public override void _Ready()
    {
        ViewModel = new MyViewModel();
    }
}
```

### Why assign `ViewModel` in `_Ready`?

In Godot there is **no built-in UI/routing framework** that creates views and injects their `ViewModel` for you. Unlike Avalonia + ReactiveUI, where `RoutingState` and the platform's `IViewLocator` typically construct the View and set `ViewModel` for you during navigation, in Godot each scene's root script must instantiate its own ViewModel at some point. The recommended place is `_Ready`, because:

- Godot guarantees `_Ready` is called after all children are initialized, so `[SceneTree]`-generated node properties (e.g. `NameEdit`) are non-null here;
- Godot's C# virtual `_Ready` runs **before** the `Ready` signal is emitted, and `WhenActivated` subscribes to the `Ready` signal via `GodotActivationFetcher`, so `_Ready`'s `ViewModel = new MyViewModel();` completes before the `WhenActivated` callback fires.

If you wire navigation yourself (see [Routing](./routing)), the `RoutedViewController` sets `view.ViewModel = viewModel` after resolving the view, so you don't need `_Ready` assignment for routed views -- only for top-level/root scenes.
