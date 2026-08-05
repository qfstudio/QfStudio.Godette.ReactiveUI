# Signal to Observable

For most Godot controls that fire a signal to notify value or state changes, the library provides corresponding `ObserveXxx()` extension methods covering `BaseButton`, `Range`, `LineEdit`, `TextEdit`, `ItemList`, `OptionButton`, `TabBar`, `TabContainer`, `ColorPicker`, `ColorPickerButton`, `Tree`, `PopupMenu`, `FileDialog`, and `SceneTree`:

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

## Custom Signals

For any `GodotObject`, bridge custom signals to `IObservable<T>` with built-in extension methods:

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
