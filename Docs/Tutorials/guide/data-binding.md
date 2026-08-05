# Data Binding

Two-way binding with `Bind`, one-way binding with `OneWayBind`:

```csharp
this.WhenActivated(d =>
{
    // Two-way: LineEdit.Text <-> ViewModel.Name
    this.Bind(ViewModel, vm => vm.Name, v => v.NameEdit.Text)
        .DisposeWith(d);

    // One-way with converter
    this.OneWayBind(ViewModel, vm => vm.Score, v => v.ScoreLabel.Text,
            score => $"{score:F1}")
        .DisposeWith(d);

    // Derived value
    this.WhenAnyValue(x => x.ViewModel!.Name, x => x.ViewModel!.Notes)
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Subscribe(tuple => { /* update UI */ })
        .DisposeWith(d);
});
```

## How Property Change Notification Works

Two binders cooperate to deliver property-change notifications:

### GodotPropertyBinder -- signal-based

Subscribes to built-in Godot signals so changes arrive instantly with no frame delay:

| Control type | Property | Godot signal |
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

### GodotPollBasedPropertyBinder -- per-frame polling

For any `GodotObject` property that does not have a dedicated signal, the binder reads the value every frame via `Observable.PollEveryUpdate` and emits when the value changes. Because it relies on polling, there is at most one frame of latency.

## Type Converters

Without the `FloatToDoubleConverter`/`DoubleToFloatConverter`, bindings between Godot controls that expose `double` properties (e.g. `Range.Value`, `ColorPicker.Color`) and ViewModel `float` properties will throw `ConverterNotFoundException` at bind time. The library also ships `EnumToStringConverter<TEnum>`, `StringToEnumConverter<TEnum>`, and `Variant`-to/from-primitive converters -- register whichever ones you need via `.WithConverter(...)` in the Autoload bootstrapper.
