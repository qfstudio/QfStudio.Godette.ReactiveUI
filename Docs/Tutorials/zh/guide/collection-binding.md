# 集合绑定

将 `ObservableCollection<TViewModel>` 同步到 Godot 容器。`ItemsBinder` 将 ViewModel 映射为子节点；`ItemListBinder`、`OptionButtonBinder`、`TabBarBinder` 和 `PopupMenuBinder` 分别绑定到对应的控件。

## Node 容器

```csharp
// 节点容器：ObservableCollection<ItemViewModel> -> VBoxContainer 子节点
var itemsBinder = new ItemsBinder<VBoxContainer, ItemLabel, ItemViewModel>(
    new GodotViewLocator());  // 若已注册，也可用 Splat.Locator.Current.GetService<GodotViewLocator>()!

this.WhenActivated(d =>
{
    itemsBinder.Connect(ItemsContainer, ViewModel!.Items)
        .DisposeWith(d);
});
```

为非 `IViewFor<TViewModel>` 的节点自定义 viewModelBinder：

```csharp
var labelBinder = new ItemsBinder<VBoxContainer, Label, ItemViewModel>(
    () => new Label(),
    (label, vm) => label.Text = vm.Name);
this.WhenActivated(d =>
{
    labelBinder.Connect(LabelContainer, ViewModel!.Items)
        .DisposeWith(d);
});
```

## ItemList

```csharp
var itemListBinder = new ItemListBinder<ItemViewModel>(textSelector: vm => vm.Name);
this.WhenActivated(d =>
{
    itemListBinder.Connect(itemListControl, items)
        .DisposeWith(d);
    itemListBinder.ObserveSelection()
        .Subscribe(vm => { /* 处理选中 */ })
        .DisposeWith(d);
});
```

## OptionButton / TabBar

```csharp
var optionBinder = new OptionButtonBinder<ItemViewModel>(textSelector: vm => vm.Name);
this.WhenActivated(d =>
{
    optionBinder.Connect(optionButton, items)
        .DisposeWith(d);
    optionBinder.ObserveSelection()
        .Subscribe(vm => { /* 处理选中 */ })
        .DisposeWith(d);
});
```

## 带命令的 PopupMenu

```csharp
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

`PopupMenuBinder` 通过 `commandSelector` 和 `commandParameterSelector` 支持 `ICommand` 绑定。当提供 `commandSelector` 时，binder 会：
1. 跟踪每个命令的 `CanExecuteChanged` 事件，并自动调用 `Container.SetItemDisabled` 以反映 `CanExecute` 状态。
2. 订阅 `Container.ObserveIdPressed()`，在点击菜单项时使用对应参数执行相应命令。

## 释放

`Connect(...)` 返回一个 `IDisposable`，用于将 binder 从容器和集合上解绑。请始终将其释放（通常在 `WhenActivated` 中通过 `DisposeWith(...)` 完成），以便在停用时执行清理。

## 选择器同步

索引类 binder（`ItemListBinder`、`OptionButtonBinder`、`TabBarBinder`、`PopupMenuBinder`）接受 `Expression<Func<TViewModel, string?>>` / `Expression<Func<TViewModel, Texture2D?>>` 选择器。当 `TViewModel` 实现 `INotifyPropertyChanged`（例如继承 `ReactiveObject`）时，binder 会通过 ReactiveUI 的 `WhenAnyValue` 进行订阅，并在 VM 的 `[Reactive]` 属性变化时保持控件的文本/图标同步。不实现 `INotifyPropertyChanged` 的 POCO view model 仅在添加/替换时写入初始值；后续的属性变更不会传播。

## 为什么用 Binder 而不是 ItemsControl？

在 Avalonia/WPF 中，集合同步内建于模板化体系：你只需绑定 `ItemsControl.ItemsSource`，框架的 `ItemContainerGenerator` 会为每个项创建容器、应用 `DataTemplate` 并接好 `DataContext`。Godot 没有 XAML/模板引擎，也没有 `ItemsSource` 属性 —— 它的 `VBoxContainer`、`ItemList`、`OptionButton`、`Tree` 等都是异构控件，增删 API 各不相同。不存在绑定层可以接入的共享"条目生成器"（item generator）。

`*Binder` 类型填补了这一空白。每个 binder 封装了某一 Godot 控件族专有的添加/移除/替换/移动逻辑，并对外暴露统一的 `Connect(container, collection)` API。这让视图侧代码保持声明式，同时作为 Godot 原生 API 之上的一层薄适配器。

第二个原因在于结构：Avalonia 风格的 `ItemsControl<T>` 必须派生自某个 Godot 控件（`Godot.Node`），但 Godot 会把每个派生自 `Godot.Node` 的 C# 类视作绑定到 Godot 项目源目录内某条唯一路径的脚本资源，并且 **完全不** 支持泛型 `Godot.Node` 类型。因此，一个可复用、泛型的集合宿主既无法存在于第三方程序集中，也无法按项进行类型化。binder 同时绕开了这两个约束 —— 它是一个普通的泛型 C# 类，通过一次 `Connect(container, ...)` 调用来驱动一个 *已存在* 的 Godot 控件。
