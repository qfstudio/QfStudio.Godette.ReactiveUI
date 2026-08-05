# 核心概念

## QfStudio.Godette.ReactiveUI 是什么

[ReactiveUI](https://www.reactiveui.net/) 是一个可组合、跨平台的 .NET MVVM（Model-View-ViewModel）框架。它使用响应式扩展将 UI 元素绑定到 ViewModel 的属性和命令，使视图与业务逻辑保持清晰的分离。

`QfStudio.Godette.ReactiveUI` 提供让 ReactiveUI 在 Godot Engine 上运行所需的平台服务 —— 调度、激活、属性变更通知以及命令绑定。如果你曾在 Avalonia 或 WPF 上使用过 ReactiveUI，那么这里就是同样的 `this.Bind` / `this.BindCommand` / `WhenActivated` 故事，只是接到了 Godot 的节点与信号上。

## 激活语义

当视图对应的 Godot `Node` 位于场景树中 **且** `IsNodeReady()` 返回 `true` 时，视图被激活（`true`）。`GodotActivationFetcher` 从以下三条路径发出 `true`：

- `Ready` 信号（首次进入，所有子节点已初始化）；
- `TreeEntered` + `IsNodeReady()`（节点已就绪后的重新进入）；
- 订阅时的初始检查 `IsInsideTree() && IsNodeReady()`。

在 `TreeExited` 时发出 `false`。其语义等价于 Avalonia 的 `AttachedToVisualTree` / `DetachedFromVisualTree`。

注意：C# 虚方法 `_Ready` 在 `Ready` 信号发出 **之前** 执行，因此在 `_Ready` 中赋值的 `ViewModel` 在 `WhenActivated` 触发时已经就绪。

## `usings`

本指南中的所有示例均假定以下 using 已在作用域内：

```csharp
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;
using System.Reactive.Disposables; // for DisposeWith(d) used throughout
```

`[GodotViewFor<T>]` 以及生成的 `ViewModel` 属性由本库自带的源生成器生成，位于 `QfStudio.Godette.ReactiveUI` 命名空间下 —— 无需额外的 `using` 或包引用。

## 基本设置

ViewModel 实现 `IActivatableViewModel`。View 使用 `[GodotViewFor<T>]` 源生成器特性来实现 `IViewFor<T>`。在构造函数中通过 `WhenActivated` 声明绑定：

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

### 为什么在 `_Ready` 中赋值 `ViewModel`？

Godot 中 **没有内置的 UI/路由框架** 来替你创建视图并注入其 `ViewModel`。不同于 Avalonia + ReactiveUI —— 在那里 `RoutingState` 与平台的 `IViewLocator` 通常会在导航期间构造 View 并为你设置 `ViewModel` —— 在 Godot 中，每个场景的根脚本必须在某个时机自行实例化自己的 ViewModel。推荐的位置是 `_Ready`，原因如下：

- Godot 保证 `_Ready` 在所有子节点初始化之后才被调用，因此此处 `[SceneTree]` 生成的节点属性（例如 `NameEdit`）非空；
- Godot 的 C# 虚方法 `_Ready` 在 `Ready` 信号发出 **之前** 执行，而 `WhenActivated` 通过 `GodotActivationFetcher` 订阅的是 `Ready` 信号，因此 `_Ready` 中的 `ViewModel = new MyViewModel();` 会在 `WhenActivated` 回调触发之前完成。

如果你自行实现导航（参见 [Routing](./routing)），`RoutedViewController` 会在解析出视图后设置 `view.ViewModel = viewModel`，因此对于路由的视图无需在 `_Ready` 中赋值 —— 仅顶层/根场景需要。
