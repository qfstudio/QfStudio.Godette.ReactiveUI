# Collection Binding

Synchronize an `ObservableCollection<TViewModel>` to a Godot container. `ItemsBinder` maps ViewModels to child nodes; `ItemListBinder`, `OptionButtonBinder`, `TabBarBinder`, and `PopupMenuBinder` bind to their respective controls.

## Node Container

```csharp
// ObservableCollection<ItemViewModel> -> VBoxContainer children
var itemsBinder = new ItemsBinder<VBoxContainer, ItemLabel, ItemViewModel>(
    new GodotViewLocator());  // or Splat.Locator.Current.GetService<GodotViewLocator>()! if registered

this.WhenActivated(d =>
{
    itemsBinder.Connect(ItemsContainer, ViewModel!.Items)
        .DisposeWith(d);
});
```

Custom viewModelBinder for nodes that are not `IViewFor<TViewModel>`:

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
        .Subscribe(vm => { /* handle selection */ })
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
        .Subscribe(vm => { /* handle selection */ })
        .DisposeWith(d);
});
```

## PopupMenu with Commands

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
        .Subscribe(vm => { /* handle selection */ })
        .DisposeWith(d);
});
```

`PopupMenuBinder` supports `ICommand` binding via `commandSelector` and `commandParameterSelector`. When `commandSelector` is provided, the binder:
1. Tracks each command's `CanExecuteChanged` event and automatically calls `Container.SetItemDisabled` to reflect `CanExecute` state.
2. Subscribes to `Container.ObserveIdPressed()` and executes the corresponding command with its parameter when a menu item is clicked.

## Disposal

`Connect(...)` returns an `IDisposable` that detaches the binder from the container and the collection. Always dispose it (typically via `DisposeWith(...)` inside `WhenActivated`) so cleanup happens on deactivation.

## Selector Sync

The index binders (`ItemListBinder`, `OptionButtonBinder`, `TabBarBinder`, `PopupMenuBinder`) accept `Expression<Func<TViewModel, string?>>` / `Expression<Func<TViewModel, Texture2D?>>` selectors. When `TViewModel` implements `INotifyPropertyChanged` (e.g. inherits `ReactiveObject`), the binder subscribes via ReactiveUI's `WhenAnyValue` and keeps the control's text/icon in sync as the VM's `[Reactive]` properties change. POCO view models that do not implement `INotifyPropertyChanged` only get the initial value written at add/replace time; subsequent property changes will not propagate.

## Why a Binder instead of an ItemsControl?

In Avalonia/WPF, collection synchronization is built into the templating stack: you bind `ItemsControl.ItemsSource` and the framework's `ItemContainerGenerator` creates a container per item, applies the `DataTemplate`, and wires `DataContext`. Godot has no XAML/template engine and no `ItemsSource` property -- its `VBoxContainer`, `ItemList`, `OptionButton`, `Tree`, etc. are heterogeneous controls with completely different add/remove APIs. There is no shared "item generator" the binding layer can hook into.

The `*Binder` types fill that gap. Each binder encapsulates the control-specific add/remove/replace/move logic for one Godot control family and exposes a uniform `Connect(container, collection)` API. This keeps the view-side code declarative while staying a thin adapter over Godot's native APIs.

A second reason is structural: an Avalonia-style `ItemsControl<T>` would have to derive from a Godot control (`Godot.Node`), but Godot treats every `Godot.Node`-derived C# class as a script resource tied to a unique path inside the Godot project's source directory, and it does **not** support generic `Godot.Node` types at all. A reusable, generic collection host therefore cannot live in a third-party assembly nor be typed per item. The binder sidesteps both constraints -- it is a plain, generic C# class that drives an *existing* Godot control through a `Connect(container, ...)` call.
