# 快速开始

## 安装

```
dotnet add package QfStudio.Godette.ReactiveUI --prerelease
```

两个可选但推荐的包可以改善开发体验：

[GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators) 提供 `[SceneTree]` 特性，用于类型安全的场景加载以及无需 `GetNode` 调用的强类型节点访问。

```
dotnet add package GodotSharp.SourceGenerators
```

使用 `[SceneTree]` 标注 `.tscn` 根脚本后，可获得：
- 一个 `TscnFilePath` 静态属性，用于类型安全的场景加载。
- 为标记了 `unique_name_in_owner` 的节点生成强类型字段 —— 无需 `GetNode` 调用。

```csharp
// 使用 [SceneTree] — 节点可直接作为属性访问
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
// 不使用 [SceneTree] — 需要通过字符串路径调用 GetNode
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

[ReactiveUI.SourceGenerators](https://github.com/reactiveui/ReactiveUI.SourceGenerators) 提供 `[Reactive]` 特性，为 partial 属性自动生成 `RaiseAndSetIfChanged` 样板代码。

```
dotnet add package ReactiveUI.SourceGenerators
```

```csharp
// 使用 [Reactive]
public partial class MyViewModel : ReactiveObject
{
    [Reactive] public partial string Name { get; set; } = "";
}
```

```csharp
// 不使用 [Reactive] — 手动编写 backing field + RaiseAndSetIfChanged
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

## Autoload 配置

创建一个引导类来初始化 ReactiveUI 服务，并将其在 Godot 中注册为 Autoload：

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

在 Godot 编辑器中，进入 **项目 > 项目设置 > Autoload**，将该脚本以 `RxAppBootstrapper` 之类的名称添加为 Autoload。

`RxAppBuilder.BuildApp()` 会将通过 `.WithMainThreadScheduler(...)` 注册的调度器同步到 ReactiveUI 的 `RxSchedulers.MainThreadScheduler`，因此 `ObserveOn(RxSchedulers.MainThreadScheduler)` 会解析为这里设置的同一个 `GodotMainThreadScheduler`。`GodotSchedulers` 是同一组实例在 Godot 侧的别名，供帧运算符及其他 Godot 专用 API 使用。

如果不加上前面所示的 `FloatToDoubleConverter`/`DoubleToFloatConverter`，那么在暴露 `double` 属性的 Godot 控件（例如 `Range.Value`、`ColorPicker.Color`）与 ViewModel 的 `float` 属性之间建立绑定时，会在绑定时抛出 `ConverterNotFoundException`。本库还附带 `EnumToStringConverter<TEnum>`、`StringToEnumConverter<TEnum>` 以及 `Variant` 与基元类型互转的转换器 —— 在上面的 builder 中通过 `.WithConverter(...)` 注册你需要的那部分即可。
