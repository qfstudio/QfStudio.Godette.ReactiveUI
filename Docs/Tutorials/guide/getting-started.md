# Quick Start

## Installation

```
dotnet add package QfStudio.Godette.ReactiveUI --prerelease
```

Two optional but recommended packages improve the development experience:

[GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators) provides the `[SceneTree]` attribute for type-safe scene loading and strongly-typed node access without `GetNode` calls.

```
dotnet add package GodotSharp.SourceGenerators
```

With `[SceneTree]`, annotate a `.tscn` root script to get:
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

[ReactiveUI.SourceGenerators](https://github.com/reactiveui/ReactiveUI.SourceGenerators) provides the `[Reactive]` attribute to auto-generate `RaiseAndSetIfChanged` boilerplate for partial properties.

```
dotnet add package ReactiveUI.SourceGenerators
```

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

## Autoload Setup

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

`RxAppBuilder.BuildApp()` mirrors the scheduler registered via `.WithMainThreadScheduler(...)` into ReactiveUI's `RxSchedulers.MainThreadScheduler`, so `ObserveOn(RxSchedulers.MainThreadScheduler)` resolves to the same `GodotMainThreadScheduler` set up here. `GodotSchedulers` is the Godot-side alias for the same instances, used by frame operators and other Godot-specific APIs.

Without the `FloatToDoubleConverter`/`DoubleToFloatConverter` shown above, bindings between Godot controls that expose `double` properties (e.g. `Range.Value`, `ColorPicker.Color`) and ViewModel `float` properties will throw `ConverterNotFoundException` at bind time. The library also ships `EnumToStringConverter<TEnum>`, `StringToEnumConverter<TEnum>`, and `Variant`-to/from-primitive converters -- register whichever ones you need via `.WithConverter(...)` in the builder above.
