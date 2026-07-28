# QfStudio.Godette.ReactiveUI

> ReactiveUI 的 Godot 引擎集成

[ReactiveUI](https://www.reactiveui.net/) 是一个面向 .NET 的跨平台 MVVM（Model-View-ViewModel）框架，以组合式风格构建。它利用响应式扩展（Reactive Extensions）将 UI 元素绑定到 ViewModel 的属性和命令，让视图和业务逻辑各司其职。

`QfStudio.Godette.ReactiveUI` 提供了让 ReactiveUI 在 Godot 引擎中运行所需的一套平台服务，包括调度器、视图激活、属性变更通知和命令绑定。如果你在 Avalonia 或 WPF 上用过 ReactiveUI，那么 `this.Bind` / `this.BindCommand` / `WhenActivated` 这些用法完全一样，只是底层对接的是 Godot 的节点和信号。实现细节见 [Developer.md](Docs/Developer.md)。

当前版本的 QfStudio.Godette.ReactiveUI 兼容 ReactiveUI v23，暂不支持今年 7 月 26 日刚发布的 ReactiveUI v24。本库尚未做零分配（zero-allocation）优化，预计未来一年内随 ReactiveUI v24 的升级一并完成减少内存分配的工作。

## 安装

```
dotnet add package QfStudio.Godette.ReactiveUI --prerelease
```

还有两个推荐安装的可选包，能大幅改善开发体验：

[GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators) 提供 `[SceneTree]` 特性，用于类型安全的场景加载和强类型节点访问，无需手写 `GetNode` 调用。

```
dotnet add package GodotSharp.SourceGenerators
```

<details>
<summary>使用与不使用 <code>[SceneTree]</code> 的对比</summary>

将该特性标注在 `.tscn` 的根脚本上，即可获得：
- `TscnFilePath` 静态属性，用于类型安全的场景加载。
- `unique_name_in_owner` 节点的强类型字段，无需手写 `GetNode`。

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

</details>

[ReactiveUI.SourceGenerators](https://github.com/reactiveui/ReactiveUI.SourceGenerators) 提供 `[Reactive]` 特性，为 partial 属性自动生成 `RaiseAndSetIfChanged` 样板代码。

```
dotnet add package ReactiveUI.SourceGenerators
```

<details>
<summary>使用与不使用 <code>[Reactive]</code> 的对比</summary>

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

</details>

### Autoload 配置

创建一个引导类来初始化 ReactiveUI 服务，然后在 Godot 中将其注册为 Autoload：

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

在 Godot 编辑器中，进入 **项目 > 项目设置 > Autoload**，将该脚本添加为 Autoload，建议命名为 `RxAppBootstrapper`。

`RxAppBuilder.BuildApp()` 会把 `.WithMainThreadScheduler(...)` 注册的调度器同步到 ReactiveUI 的 `RxSchedulers.MainThreadScheduler`，所以下文示例里的 `ObserveOn(RxSchedulers.MainThreadScheduler)` 指的就是这里设的 `GodotMainThreadScheduler`。`GodotSchedulers` 是同一组实例在 Godot 端的别名，供帧运算符和其他 Godot 专用 API 使用。

如果少了上面代码里的 `FloatToDoubleConverter` / `DoubleToFloatConverter`，在绑定暴露 `double` 属性的 Godot 控件（比如 `Range.Value`、`ColorPicker.Color`）和 ViewModel 的 `float` 属性时，绑定时会抛出 `ConverterNotFoundException`。另外本库还提供了 `EnumToStringConverter<TEnum>`、`StringToEnumConverter<TEnum>` 以及 `Variant` 与基本类型间的转换器——按需在上面的构建器里用 `.WithConverter(...)` 注册即可。

## 用法

### 核心概念

**激活语义**：视图激活（`true`）的条件是它的 Godot `Node` 在场景树**且** `IsNodeReady()` 返回 `true`。`GodotActivationFetcher` 通过三条路径发出 `true`：
- `Ready` 信号（首次进入场景树，所有子节点已初始化完毕）；
- `TreeEntered` + `IsNodeReady()`（节点已就绪后重新进入）；
- 订阅时立刻检查 `IsInsideTree() && IsNodeReady()`。

`TreeExited` 时发出 `false`，语义和 Avalonia 的 `AttachedToVisualTree` / `DetachedFromVisualTree` 一样。

注意：C# 中的 `_Ready` 虚方法在 `Ready` 信号触发**之前**执行，因此在 `_Ready` 中赋值的 `ViewModel` 在 `WhenActivated` 触发时已就绪。

### `usings`

以下示例均假定已引入这些命名空间：

```csharp
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;
using System.Reactive.Disposables; // 用于贯穿全文的 DisposeWith(d)
```

`[GodotViewFor<T>]` 生成的 `ViewModel` 属性由本库内置的源生成器发布到 `QfStudio.Godette.ReactiveUI` 命名空间——无需额外编写 `using` 或添加包引用。

### 基本设置

ViewModel 需要实现 `IActivatableViewModel`。视图使用 `[GodotViewFor<T>]` 源生成器特性实现 `IViewFor<T>`。在构造函数的 `WhenActivated` 回调中进行绑定：

```csharp
// ViewModel
public partial class MyViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    [Reactive] public partial string Name { get; set; } = "";
}

// View（.tscn 根脚本）
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
<summary>为什么要在 <code>_Ready</code> 中赋值 <code>ViewModel</code>？</summary>

在 Godot 中，**没有内置的 UI/路由框架**来自动创建视图并注入 `ViewModel`。Avalonia + ReactiveUI 通过 `RoutingState` 和平台的 `IViewLocator`（由 DataTemplates / Splat 解析）在导航时自动构建视图并设置 `ViewModel`。Godot 则不同，每个场景的根脚本都必须自己实例化 ViewModel。推荐在 `_Ready` 中完成，原因如下：
- Godot 保证 `_Ready` 在所有子节点初始化完后才调用，因此 `[SceneTree]` 生成的节点属性（如 `NameEdit`）在此处不为 null；
- Godot 的 C# 虚方法 `_Ready` 在 `Ready` 信号触发**之前**执行，而 `WhenActivated` 通过 `GodotActivationFetcher` 订阅的是 `Ready` 信号，因此 `_Ready` 中的 `ViewModel = new MyViewModel();` 在 `WhenActivated` 回调之前就已完成。

如果你自行处理路由（参见下文 [路由](#路由)），`RoutedViewController` 在解析视图后会设置 `view.ViewModel = viewModel`，因此路由视图无需在 `_Ready` 中赋值——仅顶层/根场景需要。

</details>

### 数据绑定

使用 `Bind` 进行双向绑定，使用 `OneWayBind` 进行单向绑定：

```csharp
this.WhenActivated(d =>
{
    // 双向：LineEdit.Text <-> ViewModel.Name
    this.Bind(ViewModel, vm => vm.Name, v => v.NameEdit.Text)
        .DisposeWith(d);

    // 单向 + 转换器
    this.OneWayBind(ViewModel, vm => vm.Score, v => v.ScoreLabel.Text,
            score => $"{score:F1}")
        .DisposeWith(d);

    // 派生值
    this.WhenAnyValue(x => x.ViewModel!.Name, x => x.ViewModel!.Notes)
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Subscribe(tuple => { /* 更新 UI */ })
        .DisposeWith(d);
});
```

### 命令绑定

将 `ReactiveCommand` 绑定到 `BaseButton`（在 `Pressed` 时触发）或 `LineEdit`（在 `TextSubmitted` 时触发）。`CanExecute` 会自动禁用控件：

```csharp
this.WhenActivated(d =>
{
    // 按钮按下时执行命令
    this.BindCommand(ViewModel, vm => vm.SaveCommand, v => v.SaveButton)
        .DisposeWith(d);

    // LineEdit 提交时执行命令，将当前文本作为参数传递
    this.BindCommand(ViewModel, vm => vm.SearchCommand, v => v.SearchEdit,
            vm => vm.QueryString)
        .DisposeWith(d);

    // 条件命令
    this.Bind(ViewModel, vm => vm.IsEnabled, v => v.CheckButton.ButtonPressed)
        .DisposeWith(d);
    this.BindCommand(ViewModel, vm => vm.DoWorkCommand, v => v.WorkButton)
        .DisposeWith(d);
});
```

<details>
<summary>工作原理</summary>

两个绑定器协同工作，投递属性变更通知：

**`GodotPropertyBinder`——基于信号**
订阅 Godot 内置信号，变更即时到达，无帧延迟：

| 控件类型 | 属性 | Godot 信号 |
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

**`GodotPollBasedPropertyBinder`——逐帧轮询**
没有专用信号的 `GodotObject` 属性，绑定器通过 `Observable.PollEveryUpdate` 每帧读取值，变化时发出通知。由于采用轮询机制，最多存在一帧的延迟。

</details>

### 激活生命周期

视图激活（进入场景树并就绪）时 `WhenActivated` 触发。所有通过 `DisposeWith(d)` 注册的订阅在停用时自动清理：

```csharp
public MyScene()
{
    this.WhenActivated(d =>
    {
        // 视图停用时自动释放订阅
        this.WhenAnyValue(x => x.ViewModel!.Name)
            .Subscribe(name => GD.Print($"Name: {name}"))
            .DisposeWith(d);

        Disposable.Create(() => GD.Print("deactivated"))
            .DisposeWith(d);
    });
}
```

### 信号 -> Observable

本库为大部分通过信号通知变更的 Godot 控件提供了对应的 `ObserveXxx()` 扩展方法，覆盖 `BaseButton`、`Range`、`LineEdit`、`TextEdit`、`ItemList`、`OptionButton`、`TabBar`、`TabContainer`、`ColorPicker`、`ColorPickerButton`、`Tree`、`PopupMenu`、`FileDialog` 以及 `SceneTree`：

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
});
```

对于任意 `GodotObject`，通过内置的扩展方法把自定义信号转成 `IObservable<T>`：

```csharp
this.WhenActivated(d =>
{
    // 提供了 0...7 个类型化参数的重载；
    // N 参数重载发出 ValueTuple<T1, ..., TN>，0 参数重载发出 Unit

    // 0 参数信号 -> IObservable<Unit>
    MyNode.ObserveSignal("my_signal")
        .Subscribe(_ => { /* 触发时无载荷 */ })
        .DisposeWith(d);

    // 1 参数信号 -> IObservable<ValueTuple<T1>>
    MyNode.ObserveSignal<string>("my_signal")
        .Subscribe(args => { /* args.Item1 */ })
        .DisposeWith(d);

    // 3 参数信号 -> IObservable<ValueTuple<T1, T2, T3>>
    MyNode.ObserveSignal<int, string, bool>("my_signal")
        .Subscribe(args => { var (i, s, b) = args; /* ... */ })
        .DisposeWith(d);
});
```

### 集合绑定

将 `ObservableCollection<TViewModel>` 同步到 Godot 容器。`ItemsBinder` 将 ViewModel 映射为子节点；`ItemListBinder`、`OptionButtonBinder`、`TabBarBinder` 和 `PopupMenuBinder` 分别绑定到对应的控件。

```csharp
// 节点容器：ObservableCollection<ItemViewModel> -> VBoxContainer 子节点
var itemsBinder = new ItemsBinder<VBoxContainer, ItemLabel, ItemViewModel>(
    new GodotViewLocator());  // 若已注册，也可用 Splat.Locator.Current.GetService<GodotViewLocator>()!

this.WhenActivated(d =>
{
    itemsBinder.Connect(ItemsContainer, ViewModel!.Items)
        .DisposeWith(d);
});

// ItemList：每个条目的文本/图标
var itemListBinder = new ItemListBinder<ItemViewModel>(textSelector: vm => vm.Name);
this.WhenActivated(d =>
{
    itemListBinder.Connect(itemListControl, items)
        .DisposeWith(d);
    itemListBinder.ObserveSelection()
        .Subscribe(vm => { /* 处理选中 */ })
        .DisposeWith(d);
});

// OptionButton / TabBar
var optionBinder = new OptionButtonBinder<ItemViewModel>(textSelector: vm => vm.Name);
this.WhenActivated(d =>
{
    optionBinder.Connect(optionButton, items)
        .DisposeWith(d);
    optionBinder.ObserveSelection()
        .Subscribe(vm => { /* 处理选中 */ })
        .DisposeWith(d);
});

// PopupMenu 带命令绑定 -- 每个条目执行各自的 ICommand
var menuBinder = new PopupMenuBinder<ItemViewModel>(
    textSelector: vm => vm.Label,
    iconSelector: vm => vm.Icon,
    commandSelector: vm => vm.ActionCommand,
    commandParameterSelector: vm => vm.Parameter);
this.WhenActivated(d =>
{
    menuBinder.Connect(popupMenu, menuItems)
        .DisposeWith(d);
    menuBinder.ObserveSelection()
        .Subscribe(vm => { /* 处理选中 */ })
        .DisposeWith(d);
});

```

`Connect(...)` 返回一个 `IDisposable`，用于断开绑定器与容器和集合之间的连接。请务必释放它（通常在 `WhenActivated` 内通过 `DisposeWith(...)` 完成），这样才能在停用时正确清理。

索引绑定器（`ItemListBinder`、`OptionButtonBinder`、`TabBarBinder`、`PopupMenuBinder`）接受 `Expression<Func<TViewModel, string?>>` / `Expression<Func<TViewModel, Texture2D?>>` 选择器。如果 `TViewModel` 实现了 `INotifyPropertyChanged`（如继承 `ReactiveObject`），绑定器会通过 ReactiveUI 的 `WhenAnyValue` 订阅变更，在 ViewModel 的 `[Reactive]` 属性变化时自动同步控件的文本或图标。未实现 `INotifyPropertyChanged` 的 POCO ViewModel 仅在添加或替换时写入初始值，后续属性变化不会传播。

<details>
<summary>PopupMenu 命令绑定</summary>

`PopupMenuBinder` 支持通过两个额外的构造函数参数绑定 `ICommand`：

- `commandSelector` —— `Expression<Func<TViewModel, ICommand?>>`，为每个菜单项选择对应的命令。
- `commandParameterSelector` —— 可选的 `Expression<Func<TViewModel, object?>>`，为每个菜单项选择传递给 `CanExecute` 和 `Execute` 的参数。

当提供 `commandSelector` 时，绑定器会：
1. 追踪每个命令的 `CanExecuteChanged` 事件，自动调用 `Container.SetItemDisabled` 以反映 `CanExecute` 状态。
2. 订阅 `Container.ObserveIdPressed()`，在菜单项被点击时执行对应的命令及其参数。

```csharp
// ViewModel -- 每个菜单项携带各自的命令
public partial class MenuItemViewModel : ReactiveObject
{
    [Reactive] public partial string Label { get; set; } = "";
    [Reactive] public partial Texture2D? Icon { get; set; }
    public ICommand? ActionCommand { get; set; }
    public object? Parameter { get; set; }
}

// 命令绑定
var menuBinder = new PopupMenuBinder<MenuItemViewModel>(
    textSelector: vm => vm.Label,
    iconSelector: vm => vm.Icon,
    commandSelector: vm => vm.ActionCommand,
    commandParameterSelector: vm => vm.Parameter);
```

</details>

<details>
<summary>为什么使用 Binder 而非 <code>ItemsControl</code>？</summary>

在 Avalonia/WPF 中，集合同步是模板系统自带的：绑定 `ItemsControl.ItemsSource`，框架的 `ItemContainerGenerator` 依次为每个条目创建容器、应用 `DataTemplate`、连接 `DataContext`。Godot 没有 XAML/模板引擎，也没有 `ItemsSource`——其 `VBoxContainer`、`ItemList`、`OptionButton`、`Tree` 等是异构控件，增删 API 各不相同（`AddChild`、`AddItem`、`AddItem`+`set_metadata`、`CreateItem`……），绑定层无法接入一个统一的"条目生成器"。

`*Binder` 类型正是为此设计的。每个绑定器封装了一类 Godot 控件特有的增删替换逻辑，对外暴露统一的 `Connect(container, collection)` 接口。这样视图代码保持了声明式风格（与 ReactiveUI 中其他地方使用的 `WhenActivated` + `DisposeWith(d)` 一致），同时对 Godot 原生 API 仅是一层轻量适配——没有影子视觉树、没有中间"项宿主"节点、没有大开销的模板展开。代价是需要根据控件选择对应的绑定器（节点容器用 `ItemsBinder`，`ItemList` 用 `ItemListBinder`……），而非一个万能的 `ItemsControl`。

第二个原因是架构上的：Avalonia 风格的 `ItemsControl<T>` 需要继承 Godot 控件（`Godot.Node`），但 Godot 将每个派生自 `Godot.Node` 的 C# 类视为关联至项目源码目录中唯一路径的脚本资源，且**完全不支持**泛型 `Godot.Node` 类型（见 [Developer.md § Godot 限制](../Docs/Developer.md#limitations-for-godot)）。因此一个可复用的泛型集合宿主既无法放在第三方程序集中，也无法按条目类型化。绑定器规避了这两个限制——它是一个普通的泛型 C# 类，通过 `Connect(container, ...)` 驱动一个*已有的* Godot 控件，这正是它能随本库一起发布而泛型 `ItemsControl<TNode, TView, TViewModel>` 无法做到的原因。

</details>

### Interaction（交互对话框）

将 ViewModel 的 `Interaction<TInput, TOutput>` 绑定到视图层的处理器（如对话框）：

```csharp
// ViewModel
public Interaction<string, bool> ConfirmDelete { get; } = new();
DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
{
    var confirmed = await ConfirmDelete.Handle("确认删除？");
    ResultText = confirmed ? "已确认" : "已取消";
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

### 验证

[ReactiveUI.Validation](https://github.com/reactiveui/ReactiveUI.Validation) 是一个独立包——需先安装：

```
dotnet add package ReactiveUI.Validation
```

然后在 ViewModel 上定义验证规则，在 View 上绑定错误消息：

```csharp
// 引入：ReactiveUI, ReactiveUI.SourceGenerators,
//       ReactiveUI.Validation.Abstractions, ReactiveUI.Validation.Contexts,
//       ReactiveUI.Validation.Extensions

// ViewModel — 实现 IActivatableViewModel 和 IValidatableViewModel
public partial class MyViewModel : ReactiveObject, IActivatableViewModel, IValidatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    [Reactive] public partial string Email { get; set; } = "";

    public MyViewModel()
    {
        this.ValidationRule(vm => vm.Email,
            email => !string.IsNullOrWhiteSpace(email) && email.Contains('@'),
            "邮箱必须包含 '@'。");
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

### 视图定位（`GodotViewLocator`）

`GodotViewLocator` 连接了 ReactiveUI 的视图解析和 Godot 的 `PackedScene` 系统。在 Avalonia 里，`IViewLocator` 通常通过 XAML `DataTemplates` 接入——绑定时平台检查 ViewModel 类型，然后实例化 XAML 里对应的 `Control`。Godot 没有和 `DataTemplate` 类似的视图解析机制，场景都是 `GD.Load<PackedScene>(path).Instantiate()` 加载的。`GodotViewLocator` 手动做了这个映射：把 ViewModel 类型注册到一个 `.tscn` 路径上，`ResolveView` 就会加载并实例化该场景作为 `IViewFor<TViewModel>`。

将 `GodotViewLocator` 注册到 Splat 容器中（如 Autoload 所示）是**可选的**。你也可以在需要时按需创建——例如 `var locator = new GodotViewLocator(); locator.RegisterView<MyView, MyViewModel>(...);`——然后直接传递给 `RoutedViewController` / `ItemsBinder`。Splat 注册仅为方便，使通过 `Locator.Current` 解析的库组件能找到同一个实例。

向 `GodotViewLocator` 注册视图有三种方式：

```csharp
var locator = new GodotViewLocator();

// 1. 显式注册视图 + ViewModel 类型
locator.RegisterView<MyView, MyViewModel>("res://Views/MyView.tscn");
// 若安装了 GodotSharp.SourceGenerators，也可：
locator.RegisterView<MyView, MyViewModel>(MyView.TscnFilePath);

// 2. 仅指定 ViewModel 类型（视图类型从 IViewFor<T> 实现推断）
locator.RegisterView<MyViewModel>("res://Views/MyView.tscn");

// 3. 通过反射扫描整个程序集——选取所有实现了
//    IViewFor<TViewModel> 且暴露了静态 TscnFilePath 属性的具体类型
locator.RegisterViewsFromAssemblyViaReflection(typeof(MyView).Assembly);
```

方式 3 依赖 **[GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators)**（`[SceneTree]`）生成的 `TscnFilePath` 静态属性。该包完全是可选的——方式 1 和 2 直接传递 `"res://..."` 路径字符串即可，无需源生成器。不使用该包时，手动为每对 ViewModel/View 调用 `RegisterView(...)` 即可；注册完成后，路由、`ItemsBinder` 等视图解析功能照常工作。

`RegisterView<TViewModel>(path)` 仅存储 ViewModel 类型和 `.tscn` 路径。解析时 `GodotViewLocator` 执行 `GD.Load<PackedScene>(path).Instantiate<IViewFor<TViewModel>>()`——实际返回的视图类由 `.tscn` 的根脚本决定（必须实现 `IViewFor<TViewModel>`）。`RegisterView<TView, TViewModel>(...)` 中的 `<TView>` 类型参数仅用于编译期检查，不影响解析结果。

`ResolveView` 通常由 ReactiveUI（或下面示例中的 `RoutedViewController`）调用，而非用户代码——只需保持注册信息的最新即可。

### 路由

ReactiveUI 的 `RoutingState` 配合 `GodotViewLocator` 实现页面导航。本库提供了视图定位器和注册 API；导航时切换子节点仍需一个适配器——下面这个 `RoutedViewController` **不包含**在 NuGet 包中，是可以从 [IntegrationTests/Views/Routing/RoutedViewController.cs](QfStudio.Godette.IntegrationTests/Views/Routing/RoutedViewController.cs) 复制使用的示例代码：

```csharp
// 初始化（在 _Ready 或构造函数中）
var locator = new GodotViewLocator();
locator.RegisterView<PageAView, PageAViewModel>(PageAView.TscnFilePath);
locator.RegisterView<PageBViewModel>(PageBView.TscnFilePath);

var shell = new ShellViewModel(); // 实现 IScreen，包含 RoutingState
var router = new RoutedViewController(shell.Router, locator); // 示例适配器，见上方说明
router.Connect(ContentContainer);

// 导航
shell.Router.Navigate.Execute(new PageAViewModel(shell));
shell.Router.NavigateBack.Execute().Subscribe();
```

### 帧运算符

基于 `SceneTree.ProcessFrame` 的帧感知响应式运算符：

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

## 替代方案

如果本库不适合你，可以看看：

- [**R3**](https://github.com/Cysharp/R3)——UniRx 作者开发的零分配 Rx.NET 重新实现。如果你更倾向于使用 `ReactiveProperty` 而非完整的 MVVM，或希望帧运算符作为核心功能，R3 是个不错的选择。它可以与 ReactiveUI 并行使用（例如 ReactiveUI 负责 UI 层，R3 负责业务逻辑层）。

---

## 许可证

MIT 许可证

## 开发

参阅 [Developer.md](Docs/Developer.md)。

## AI 使用声明

本项目使用了 AI 辅助编码，AI 的作用仅限于提供代码建议和处理琐碎任务。
所有代码均已尽最大努力进行人工审阅把关。
无不妥代码入库。
