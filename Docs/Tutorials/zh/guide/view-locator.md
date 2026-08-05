# View 定位器

`GodotViewLocator` 是连接 ReactiveUI 视图解析与 Godot `PackedScene` 系统的桥梁。在 Avalonia 中，`IViewLocator` 通常通过 XAML `DataTemplates` 接入——平台在绑定时检查 ViewModel 的类型，并实例化 XAML 中声明的匹配 `Control`。Godot 没有等价的 `DataTemplate` 驱动的视图解析机制；场景通过 `GD.Load<PackedScene>(path).Instantiate()` 加载。`GodotViewLocator` 手动提供这一映射：将 ViewModel 类型注册到某个 `.tscn` 路径，`ResolveView` 便会加载并将该场景实例化为 `IViewFor<TViewModel>`。

将 `GodotViewLocator` 注册到 Splat 定位器（正如 Autoload 所做的那样）是**可选的**。你也可以在需要时按需创建一个实例——例如 `var locator = new GodotViewLocator(); locator.RegisterView<MyView, MyViewModel>(...);`——并直接传给 `RoutedViewController` / `ItemsBinder`。Splat 注册只是为了方便，让那些通过 `Locator.Current` 解析的库组件能找到共享实例。

## 注册方法

在 `GodotViewLocator` 实例上注册 View 有三种方式：

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

方式 3 依赖由 **[GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators)**（`[SceneTree]`）生成的 `TscnFilePath` 静态属性。该包完全是可选的——方式 1 和 2 直接接受 `"res://..."` 路径字符串，无需源生成器。如果跳过它，只需为每个 ViewModel/View 对手动调用 `RegisterView(...)`；只要注册到位，路由、`ItemsBinder` 及其他视图解析功能的工作方式完全相同。

## 解析原理

`RegisterView<TViewModel>(path)` 仅存储 ViewModel 类型和 `.tscn` 路径。在解析时，`GodotViewLocator` 执行 `GD.Load<PackedScene>(path).Instantiate<IViewFor<TViewModel>>()`——实际的 View 类由 `.tscn` 根脚本所实现的类型决定（它必须实现 `IViewFor<TViewModel>`）。`RegisterView<TView, TViewModel>(...)` 中的 `<TView>` 类型参数仅用于编译期校验，不影响解析结果。

`ResolveView` 通常由 ReactiveUI（或下文的示例 `RoutedViewController`）调用，而非你自己的代码；你只需保持注册信息是最新的即可。
