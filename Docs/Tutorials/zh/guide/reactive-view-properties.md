# 响应式视图属性

前面的示例观察的都是 *ViewModel*。节点自身也能暴露可观察属性——这对没有独立 ViewModel 的可复用自定义节点很有用（例如 `ItemsBinder` 里的条目视图，或自包含的小部件）。

## 视图属性如何变得可观察

Godot 没有与 WPF `DependencyProperty`、Avalonia `StyledProperty` 对应的机制，视图属性要变成可观察属性，有两条路：

1. **自行发出变更通知。** 节点实现 `IReactiveObject` 即可开启这条路：`[Reactive]` 属性会发出变更通知，供 `WhenAnyValue` 等订阅。
2. **每帧轮询。** 没有内置通知信号的引擎属性（如 `Position`、`Rotation`、`Size`）会自动退回这条路。

两条路径对观察代码完全透明——`WhenAnyValue(x => x.ClickCount)` 与 `WhenAnyValue(x => x.Position)` 写起来一模一样，只是底层机制不同。

## 示例：自包含的计数器小部件

下面是一个没有 ViewModel 的可复用节点，同时观察两类属性：

```csharp
// usings: Godot, ReactiveUI, ReactiveUI.SourceGenerators

[SceneTree(root: "_root")]
[IReactiveObject]  // 来自 ReactiveUI.SourceGenerators -- 生成 IReactiveObject 的四个成员
public partial class CustomNode : Control, IActivatableView
{
    // 可观察，且可在 Godot 检查器中编辑
    [Reactive]
    [Export]
    public partial int ClickCount { get; set; }

    public CustomNode()
    {
        this.WhenActivated(d =>
        {
            // 用户声明的 [Reactive] 属性
            this.WhenAnyValue(x => x.ClickCount)
                .Subscribe(count => CountLabel.Text = $"ClickCount: {count}")
                .DisposeWith(d);

            // 引擎声明的属性
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

各部分的作用：

- `[IReactiveObject]` 生成 `IReactiveObject` 的四个成员，让节点成为变更通知源。
- `[Reactive]` 让 `ClickCount` 发出变更通知；`[Export]` 额外把属性暴露到 Godot 检查器中，两者可自由组合。
- `Position` 是没有内置通知信号的引擎属性，库会逐帧轮询它（至多一帧延迟）。
- `IActivatableView` 是标记接口——`WhenActivated` 只需要它，因此节点无需 ViewModel 也能使用激活生命周期。用 `[GodotViewFor<T>]` 声明的视图已经实现了 `IReactiveObject` 和 `IActivatableView`，开箱即用地支持 `[Reactive][Export]`。

## 静默不可观察的属性

在实现 `IReactiveObject` 或 `INotifyPropertyChanged` 的节点上，普通 CLR 属性是静默不可观察的：观察一个从不发出变更通知的用户声明属性，你只会得到初始值，之后不再有任何变化——没有报错，也没有更新。想观察用户声明的属性，请务必使用 `[Reactive]`，或手写 `RaiseAndSetIfChanged`。

```csharp
// 错误：只能观察到初始值，之后不再更新
public int Count { get; set; }

// 正确：发出变更通知
[Reactive] public partial int Count { get; set; }
```

## 如何让节点实现 IReactiveObject

两种方式皆可，且可互换：

```csharp
// 1. [IReactiveObject] 特性（ReactiveUI.SourceGenerators）
[IReactiveObject]
public partial class CustomNodeA : Control, IActivatableView { /* ... */ }
```

```csharp
// 2. 手写实现
public partial class CustomNodeB : Control, IReactiveObject, IActivatableView
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
    void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);
}
```

第三种方式是由共享抽象基类承载 `IReactiveObject` 成员。这与 Avalonia 的用法最接近——在 Avalonia 中自定义 View 继承框架提供的基类，例如 `class MyView : ReactiveUserControl<MyViewModel>`，`IViewFor<T>` 已由基类实现。不过在 Godot 中这条路并不实用：C# 不支持多重继承，Godot 也不支持在 Script 资源中使用泛型，而视图必须继承 Godot 节点，自然也就无法再继承这个共享抽象基类。实践中还是推荐采用前两种方式，配合源生成器与 `[GodotViewFor<T>]` 使用。
