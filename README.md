# QfStudio.Godette.ReactiveUI

> ReactiveUI integration for Godot Engine

[ReactiveUI](https://www.reactiveui.net/) is a composable, cross-platform MVVM (Model-View-ViewModel) framework for .NET. It uses reactive extensions to bind UI elements to ViewModel properties and commands, keeping views and business logic cleanly separated.

`QfStudio.Godette.ReactiveUI` provides the platform services that make ReactiveUI work with Godot Engine - scheduling, activation, property-change notification, and command binding. If you have used ReactiveUI with Avalonia or WPF, this is the same `this.Bind` / `this.BindCommand` / `WhenActivated` story, now wired to Godot nodes and signals. See [Developer.md](Docs/Developer.md) for implementation details.

In the current version of QfStudio.Godette.ReactiveUI, it is designed to work with ReactiveUI v23. ReactiveUI v24 released on July 26th this year (a few hours ago at the moment of writing) is not supported yet. This library is not optimized for zero-allocation; allocation reduction work is planned within roughly the next year together with the ReactiveUI v24 upgrade.

## Installation

```
dotnet add package QfStudio.Godette.ReactiveUI --prerelease
```

Two optional but recommended packages improve the development experience:

[GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators) provides the `[SceneTree]` attribute for type-safe scene loading and strongly-typed node access without `GetNode` calls.

```
dotnet add package GodotSharp.SourceGenerators
```

<details>
<summary>With vs Without <code>[SceneTree]</code></summary>

Provides the `[SceneTree]` attribute. Annotate a `.tscn` root script to get:
- A `TscnFilePath` static property for type-safe scene loading.
- Strongly-typed fields for nodes marked `unique_name_in_owner` -- no `GetNode` calls needed.

```csharp
// With [SceneTree] -- nodes are directly accessible as properties
[SceneTree(root: "_root")]
public partial class MyScene : Control
{
    public override void _Ready()
    {
        BackButton.Pressed += () => GetTree().ChangeSceneToFile(HomeScene.TscnFilePath);
        NameEdit.Text = "hello";
    }
}
```

```csharp
// Without [SceneTree] -- use GetNode with string paths
public partial class MyScene : Control
{
    public override void _Ready()
    {
        GetNode<Button>("BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://Views/HomeScene.tscn");
        GetNode<LineEdit>("NameEdit").Text = "hello";
    }
}
```

</details>

[ReactiveUI.SourceGenerators](https://github.com/reactiveui/ReactiveUI.SourceGenerators) provides the `[Reactive]` attribute to auto-generate `RaiseAndSetIfChanged` boilerplate for partial properties.

```
dotnet add package ReactiveUI.SourceGenerators
```

<details>
<summary>With vs Without <code>[Reactive]</code></summary>

```csharp
// With [Reactive]
public partial class MyViewModel : ReactiveObject
{
    [Reactive] public partial string Name { get; set; } = "";
}
```

```csharp
// Without [Reactive] -- manual backing field + RaiseAndSetIfChanged
public class MyViewModel : ReactiveObject
{
    private string _name = "";
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
}
```

</details>

### Autoload Setup

Create a bootstrapper class to initialize ReactiveUI services and add it as an Autoload in Godot:

```csharp
using System.Threading;
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;
using ReactiveUI.Builder;
using Splat;

public partial class RxAppBootstrapper : Godot.Node
{
    private readonly GodotFrameScheduler _processFrameScheduler = new();
    private readonly GodotFrameScheduler _physicsFrameScheduler = new();

    public RxAppBootstrapper()
    {
        var scheduler = GodotMainThreadScheduler.Create(SynchronizationContext.Current!);
        GodotSchedulers.MainThreadScheduler = scheduler;              
        GodotSchedulers.ProcessFrameScheduler = _processFrameScheduler; 
        GodotSchedulers.PhysicsFrameScheduler = _physicsFrameScheduler;

        var viewLocator = new GodotViewLocator();
        viewLocator.RegisterViewsFromAssemblyViaReflection(typeof(RxAppBootstrapper).Assembly, verbose: false);

        RxAppBuilder.CreateReactiveUIBuilder()
            .WithMainThreadScheduler(scheduler) 
            .WithRegistration(locator =>
            {
                locator.RegisterConstant(new GodotActivationFetcher(), typeof(IActivationForViewFetcher));
                locator.RegisterConstant(new GodotPropertyBinder(), typeof(ICreatesObservableForProperty));
                locator.RegisterConstant(new GodotPollBasedPropertyBinder(), typeof(ICreatesObservableForProperty));
                locator.RegisterConstant(new GodotCommandBinder(), typeof(ICreatesCommandBinding));
                locator.RegisterConstant(viewLocator, typeof(GodotViewLocator));
            })
            .WithConverter(new FloatToDoubleConverter())
            .WithConverter(new DoubleToFloatConverter())
            .WithCoreServices()
            .BuildApp();
    }

    public override void _Process(double delta)
    {
        _processFrameScheduler.NotifyProcess(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _physicsFrameScheduler.NotifyProcess(delta);
    }
}
```

In Godot Editor, go to **Project > Project Settings > Autoload** and add this script as an Autoload with a name like `RxAppBootstrapper`.

`RxAppBuilder.BuildApp()` mirrors the scheduler registered via `.WithMainThreadScheduler(...)` into ReactiveUI's `RxSchedulers.MainThreadScheduler`, so `ObserveOn(RxSchedulers.MainThreadScheduler)` (used in the examples below) resolves to the same `GodotMainThreadScheduler` set up here. `GodotSchedulers` is the Godot-side alias for the same instances, used by frame operators and other Godot-specific APIs.

Without the `FloatToDoubleConverter`/`DoubleToFloatConverter` shown above, bindings between Godot controls that expose `double` properties (e.g. `Range.Value`, `ColorPicker.Color`) and ViewModel `float` properties will throw `ConverterNotFoundException` at bind time. The library also ships `EnumToStringConverter<TEnum>`, `StringToEnumConverter<TEnum>`, and `Variant`-to/from-primitive converters -- register whichever ones you need via `.WithConverter(...)` in the builder above.

## Usage

### Concepts

**Activation semantics**: A view is activated (`true`) when its Godot `Node` is inside the scene tree **and** `IsNodeReady()` returns `true`. The `GodotActivationFetcher` emits `true` from three paths:
- the `Ready` signal (first entry, all children initialized);
- `TreeEntered` + `IsNodeReady()` (re-entry after the node was already ready);
- an initial `IsInsideTree() && IsNodeReady()` check at subscription time.

It emits `false` on `TreeExited`. This is semantically equivalent to Avalonia's `AttachedToVisualTree` / `DetachedFromVisualTree`.

Note: the C# virtual method `_Ready` runs **before** the `Ready` signal is emitted, so a `ViewModel` assigned in `_Ready` is already set by the time `WhenActivated` fires.

### `usings`

All examples below assume these usings are in scope:

```csharp
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;
using System.Reactive.Disposables; // for DisposeWith(d) used throughout
```

`[GodotViewFor<T>]` and the generated `ViewModel` property are emitted by this library's bundled source generator into the `QfStudio.Godette.ReactiveUI` namespace -- no extra `using` or package reference is required.

### Basic Setup

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

<details>
<summary>Why assign <code>ViewModel</code> in <code>_Ready</code>?</summary>

In Godot there is **no built-in UI/routing framework** that creates views and injects their `ViewModel` for you. Unlike Avalonia + ReactiveUI, where `RoutingState` and the platform's `IViewLocator` (resolved via DataTemplates / Splat) typically construct the View and set `ViewModel` for you during navigation, in Godot each scene's root script must instantiate its own ViewModel at some point. The recommended place is `_Ready`, because:
- Godot guarantees `_Ready` is called after all children are initialized, so `[SceneTree]`-generated node properties (e.g. `NameEdit`) are non-null here;
- Godot's C# virtual `_Ready` runs **before** the `Ready` signal is emitted, and `WhenActivated` subscribes to the `Ready` signal via `GodotActivationFetcher`, so `_Ready`'s `ViewModel = new MyViewModel();` completes before the `WhenActivated` callback fires.

If you wire navigation yourself (see [Routing](#routing) below), the `RoutedViewController` sets `view.ViewModel = viewModel` after resolving the view, so you don't need `_Ready` assignment for routed views -- only for top-level/root scenes.

</details>

### Data Binding

Two-way binding with `Bind`, one-way binding with `OneWayBind`:

```csharp
this.WhenActivated(d =>
{
    // Two-way: LineEdit.Text <-> ViewModel.Name
    this.Bind(ViewModel, vm => vm.Name, v => v.NameEdit.Text)
        .DisposeWith(d);

    // One-way with converter
    this.OneWayBind(ViewModel, vm => vm.Score, v => v.ScoreLabel.Text,
            score => $"{score:F1}")
        .DisposeWith(d);

    // Derived value
    this.WhenAnyValue(x => x.ViewModel!.Name, x => x.ViewModel!.Notes)
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Subscribe(tuple => { /* update UI */ })
        .DisposeWith(d);
});
```

### Command Binding

Bind a `ReactiveCommand` to a `BaseButton` (triggers on `Pressed`) or `LineEdit` (triggers on `TextSubmitted`). `CanExecute` automatically disables the control:

```csharp
this.WhenActivated(d =>
{
    // Button press executes the command
    this.BindCommand(ViewModel, vm => vm.SaveCommand, v => v.SaveButton)
        .DisposeWith(d);

    // LineEdit submits the command, passing the current text as parameter
    this.BindCommand(ViewModel, vm => vm.SearchCommand, v => v.SearchEdit,
            vm => vm.QueryString)
        .DisposeWith(d);

    // Conditional command
    this.Bind(ViewModel, vm => vm.IsEnabled, v => v.CheckButton.ButtonPressed)
        .DisposeWith(d);
    this.BindCommand(ViewModel, vm => vm.DoWorkCommand, v => v.WorkButton)
        .DisposeWith(d);
});
```

<details>
<summary>How it works</summary>

Two binders cooperate to deliver property-change notifications:

**`GodotPropertyBinder` -- signal-based**
Subscribes to built-in Godot signals so changes arrive instantly with no frame delay:

| Control type | Property | Godot signal |
|---|---|---|
| `Range` | `Value` | `ValueChanged` |
| `LineEdit` | `Text` | `TextChanged` |
| `TextEdit` | `Text` | `TextChanged` |
| `BaseButton` | `ButtonPressed` | `Toggled` |
| `TabContainer` | `CurrentTab` | `TabChanged` |
| `TabBar` | `CurrentTab` | `TabChanged` |
| `OptionButton` | `Selected` | `ItemSelected` |
| `ColorPicker` | `Color` | `ColorChanged` |
| `ColorPickerButton` | `Color` | `ColorChanged` |

**`GodotPollBasedPropertyBinder` — per-frame polling**
For any `GodotObject` property that does not have a dedicated signal, the binder reads the value every frame via `Observable.PollEveryUpdate` and emits when the value changes. Because it relies on polling, there is at most one frame of latency.

</details>

### Activation Lifecycle

When a view is activated (entering the scene tree and ready), `WhenActivated` fires. All subscriptions registered via `DisposeWith(d)` are cleaned up on deactivation:

```csharp
public MyScene()
{
    this.WhenActivated(d =>
    {
        // subscriptions are disposed when the view is deactivated
        this.WhenAnyValue(x => x.ViewModel!.Name)
            .Subscribe(name => GD.Print($"Name: {name}"))
            .DisposeWith(d);

        Disposable.Create(() => GD.Print("deactivated"))
            .DisposeWith(d);
    });
}
```

### Signal -> Observable

Bridge Godot signals to `IObservable<T>` with built-in extension methods:

```csharp
this.WhenActivated(d =>
{
    ToggleButton.ObserveToggled()
        .Subscribe(on => ViewModel!.IsToggled = on)
        .DisposeWith(d);

    LineEdit.ObserveTextChanged()
        .Subscribe(text => ViewModel!.InputText = text)
        .DisposeWith(d);

    GetTree().ObserveProcessFrame()
        .Subscribe(_ => ViewModel!.FrameCount++)
        .DisposeWith(d);

    // Custom signals (overloads for 0...7 typed arguments;
    // N-arg overloads emit ValueTuple<T1, ..., TN>, 0-arg emits Unit)

    // 0-arg signal -> IObservable<Unit>
    MyNode.ObserveSignal("my_signal")
        .Subscribe(_ => { /* fired with no payload */ })
        .DisposeWith(d);

    // 1-arg signal -> IObservable<ValueTuple<T1>>
    MyNode.ObserveSignal<string>("my_signal")
        .Subscribe(args => { /* args.Item1 */ })
        .DisposeWith(d);

    // 3-arg signal -> IObservable<ValueTuple<T1, T2, T3>>
    MyNode.ObserveSignal<int, string, bool>("my_signal")
        .Subscribe(args => { var (i, s, b) = args; /* ... */ })
        .DisposeWith(d);
});
```

### Collection Binding

Synchronize an `ObservableCollection<TViewModel>` to a Godot container. `ItemsBinder` maps ViewModels to child nodes; `ItemListBinder`, `OptionButtonBinder`, `TabBarBinder`, and `PopupMenuBinder` bind to their respective controls.

```csharp
// Node container: ObservableCollection<ItemViewModel> -> VBoxContainer children
var itemsBinder = new ItemsBinder<VBoxContainer, ItemLabel, ItemViewModel>(
    new GodotViewLocator());  // or Splat.Locator.Current.GetService<GodotViewLocator>()! if registered

this.WhenActivated(d =>
{
    itemsBinder.Connect(ItemsContainer, ViewModel!.Items)
        .DisposeWith(d);
});

// ItemList: text/icon per item
var itemListBinder = new ItemListBinder<ItemViewModel>(textSelector: vm => vm.Name);
this.WhenActivated(d =>
{
    itemListBinder.Connect(itemListControl, items)
        .DisposeWith(d);
    itemListBinder.ObserveSelection()
        .Subscribe(vm => { /* handle selection */ })
        .DisposeWith(d);
});

// OptionButton / TabBar / PopupMenu
var optionBinder = new OptionButtonBinder<ItemViewModel>(textSelector: vm => vm.Name);
this.WhenActivated(d =>
{
    optionBinder.Connect(optionButton, items)
        .DisposeWith(d);
    optionBinder.ObserveSelection()
        .Subscribe(vm => { /* handle selection */ })
        .DisposeWith(d);
});

```

`Connect(...)` returns an `IDisposable` that detaches the binder from the container and the collection. Always dispose it (typically via `DisposeWith(...)` inside `WhenActivated`) so cleanup happens on deactivation.

The index binders (`ItemListBinder`, `OptionButtonBinder`, `TabBarBinder`, `PopupMenuBinder`) accept `Expression<Func<TViewModel, string?>>` / `Expression<Func<TViewModel, Texture2D?>>` selectors. When `TViewModel` implements `INotifyPropertyChanged` (e.g. inherits `ReactiveObject`), the binder subscribes via ReactiveUI's `WhenAnyValue` and keeps the control's text/icon in sync as the VM's `[Reactive]` properties change. POCO view models that do not implement `INotifyPropertyChanged` only get the initial value written at add/replace time; subsequent property changes will not propagate.

<details>
<summary>Why a Binder instead of an <code>ItemsControl</code>?</summary>

In Avalonia/WPF, collection synchronization is built into the templating stack: you bind `ItemsControl.ItemsSource` and the framework's `ItemContainerGenerator` creates a container per item, applies the `DataTemplate`, and wires `DataContext`. Godot has no XAML/template engine and no `ItemsSource` property -- its `VBoxContainer`, `ItemList`, `OptionButton`, `Tree`, etc. are heterogeneous controls with completely different add/remove APIs (`AddChild`, `AddItem`, `AddItem`+`set_metadata`, `CreateItem`...). There is no shared "item generator" the binding layer can hook into.

The `*Binder` types fill that gap. Each binder encapsulates the control-specific add/remove/replace/move logic for one Godot control family and exposes a uniform `Connect(container, collection)` API. This keeps the view-side code declarative (the same shape as `WhenActivated` + `DisposeWith(d)` used elsewhere in ReactiveUI) while staying a thin adapter over Godot's native APIs -- no shadow visual tree, no intermediate "items host" node, no allocation-heavy template expansion. The trade-off is that you pick the binder matching your control (`ItemsBinder` for node containers, `ItemListBinder` for `ItemList`, ...), rather than one universal `ItemsControl`.

A second reason is structural: an Avalonia-style `ItemsControl<T>` would have to derive from a Godot control (`Godot.Node`), but Godot treats every `Godot.Node`-derived C# class as a script resource tied to a unique path inside the Godot project's source directory, and it does **not** support generic `Godot.Node` types at all (see [Developer.md § Limitations for Godot](../Docs/Developer.md#limitations-for-godot)). A reusable, generic collection host therefore cannot live in a third-party assembly nor be typed per item. The binder sidesteps both constraints -- it is a plain, generic C# class that drives an *existing* Godot control through a `Connect(container, ...)` call, which is exactly why it ships in this library while a generic `ItemsControl<TNode, TView, TViewModel>` cannot.

</details>

### Interaction

Bind a ViewModel's `Interaction<TInput, TOutput>` to a View-level handler (e.g. a dialog):

```csharp
// ViewModel
public Interaction<string, bool> ConfirmDelete { get; } = new();
DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
{
    var confirmed = await ConfirmDelete.Handle("Confirm to delete?");
    ResultText = confirmed ? "Confirmed" : "Canceled";
});

// View
this.WhenActivated(d =>
{
    this.BindInteraction(ViewModel, vm => vm.ConfirmDelete, async context =>
    {
        ConfirmDialog.DialogText = context.Input;
        ConfirmDialog.PopupCentered();
        var tcs = new TaskCompletionSource<bool>();
        ConfirmDialog.Confirmed += () => tcs.TrySetResult(true);
        ConfirmDialog.Canceled += () => tcs.TrySetResult(false);
        context.SetOutput(await tcs.Task);
    }).DisposeWith(d);
});
```

### Validation

[ReactiveUI.Validation](https://github.com/reactiveui/ReactiveUI.Validation) is a separate package -- install it first:

```
dotnet add package ReactiveUI.Validation
```

Then define rules on the ViewModel and bind error messages on the View:

```csharp
// usings: ReactiveUI, ReactiveUI.SourceGenerators,
//         ReactiveUI.Validation.Abstractions, ReactiveUI.Validation.Contexts,
//         ReactiveUI.Validation.Extensions

// ViewModel -- implement IActivatableViewModel and IValidatableViewModel
public partial class MyViewModel : ReactiveObject, IActivatableViewModel, IValidatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    [Reactive] public partial string Email { get; set; } = "";

    public MyViewModel()
    {
        this.ValidationRule(vm => vm.Email,
            email => !string.IsNullOrWhiteSpace(email) && email.Contains('@'),
            "Email must contain '@'.");
    }
}

// View
this.WhenActivated(d =>
{
    this.Bind(ViewModel, vm => vm.Email, v => v.EmailEdit.Text)
        .DisposeWith(d);
    this.BindValidation(ViewModel, vm => vm.Email, v => v.ErrorLabel.Text)
        .DisposeWith(d);
});
```

### View Location (`GodotViewLocator`)

`GodotViewLocator` is the bridge between ReactiveUI's view resolution and Godot's `PackedScene` system. In Avalonia, `IViewLocator` is typically wired via XAML `DataTemplates` -- the platform inspects a ViewModel's type at binding time and instantiates the matching `Control` declared in XAML. Godot has no equivalent of `DataTemplate`-driven view resolution; scenes are loaded by `GD.Load<PackedScene>(path).Instantiate()`. `GodotViewLocator` provides that mapping manually: register a ViewModel type against a `.tscn` path, and `ResolveView` will load and instantiate the scene as an `IViewFor<TViewModel>`.

Registering `GodotViewLocator` into the Splat locator (as the Autoload does) is **optional**. You can instead create one on demand wherever you need it -- for example `var locator = new GodotViewLocator(); locator.RegisterView<MyView, MyViewModel>(...);` -- and pass it directly to `RoutedViewController` / `ItemsBinder`. The Splat registration is only a convenience so that library components that resolve through `Locator.Current` can find a shared instance.

There are three ways to register views on a `GodotViewLocator` instance:

```csharp
var locator = new GodotViewLocator();

// 1. Explicit, view + viewmodel types
locator.RegisterView<MyView, MyViewModel>("res://Views/MyView.tscn");
// or If you have GodotSharp.SourceGenerators installed
locator.RegisterView<MyView, MyViewModel>(MyView.TscnFilePath);

// 2. Only the viewmodel type ( viewType inferred from the IViewFor<T> implementation )
locator.RegisterView<MyViewModel>("res://Views/MyView.tscn");

// 3. Reflect the whole assembly -- picks up every concrete type implementing
//    IViewFor<TViewModel> that also exposes a static TscnFilePath property.
locator.RegisterViewsFromAssemblyViaReflection(typeof(MyView).Assembly);
```

Option 3 relies on the `TscnFilePath` static property generated by **[GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators)** (`[SceneTree]`). That package is entirely optional -- options 1 and 2 accept a `"res://..."` path string directly and need no source generator. If you skip it, just call `RegisterView(...)` manually for each ViewModel/View pair; routing, `ItemsBinder`, and other view-resolution features work the same once the registrations are in place.

`RegisterView<TViewModel>(path)` only stores the ViewModel type and the `.tscn` path. At resolve time `GodotViewLocator` does `GD.Load<PackedScene>(path).Instantiate<IViewFor<TViewModel>>()` -- the actual view class is whatever the `.tscn` root script implements (it must implement `IViewFor<TViewModel>`). The `<TView>` type argument in `RegisterView<TView, TViewModel>(...)` is used only for compile-time validation and does not influence resolution.

`ResolveView` is normally called by ReactiveUI (or the sample `RoutedViewController` below) rather than your own code; you just keep the registrations up to date.

### Routing

ReactiveUI's `RoutingState` works with `GodotViewLocator` for page navigation. The library provides the view locator and registration API; you still need a small adapter to swap child nodes on navigation -- `RoutedViewController` below is **not** part of the NuGet package, it is sample code you can copy from [IntegrationTests/Views/Routing/RoutedViewController.cs](QfStudio.Godette.IntegrationTests/Views/Routing/RoutedViewController.cs):

```csharp
// Setup (in _Ready or constructor)
var locator = new GodotViewLocator();
locator.RegisterView<PageAView, PageAViewModel>(PageAView.TscnFilePath);
locator.RegisterView<PageBViewModel>(PageBView.TscnFilePath);

var shell = new ShellViewModel(); // implements IScreen with RoutingState
var router = new RoutedViewController(shell.Router, locator); // sample adapter, see note above
router.Connect(ContentContainer);

// Navigate
shell.Router.Navigate.Execute(new PageAViewModel(shell));
shell.Router.NavigateBack.Execute().Subscribe();
```

### Frame Operators

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

## Alternatives

If this library isn't a fit for your needs, consider:

- [**R3**](https://github.com/Cysharp/R3) zero-allocation Rx.NET reimplementation by the author of UniRx. Good fit if you prefer `ReactiveProperty` over full MVVM or want frame operators as core. Can run alongside ReactiveUI (e.g. ReactiveUI at UI layer, R3 at business-logic layer).

---

## License

MIT License

## Development

See [Developer.md](Docs/Developer.md).

## AI Disclosure

This project uses AI-assisted coding for suggestions and trivial tasks only. 
All code is vetted with best-effort human review. 
No dubious code is committed.
