# Signal 转 Observable

对于大多数通过发射信号来通知值或状态变更的 Godot 控件，本库提供了对应的 `ObserveXxx()` 扩展方法，覆盖 `BaseButton`、`Range`、`LineEdit`、`TextEdit`、`ItemList`、`OptionButton`、`TabBar`、`TabContainer`、`ColorPicker`、`ColorPickerButton`、`Tree`、`PopupMenu`、`FileDialog` 和 `SceneTree`：

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

## 自定义信号

对于任何 `GodotObject`，可使用内置扩展方法将自定义信号桥接为 `IObservable<T>`：

```csharp
this.WhenActivated(d =>
{
    // Overloads for 0...7 typed arguments;
    // N-arg overloads emit ValueTuple<T1, ..., TN>, 0-arg emits Unit

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
