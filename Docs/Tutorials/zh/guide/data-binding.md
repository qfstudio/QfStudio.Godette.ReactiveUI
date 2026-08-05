# 数据绑定

使用 `Bind` 进行双向绑定，使用 `OneWayBind` 进行单向绑定：

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

## 属性变更通知的工作原理

两个 binder 协同工作来传递属性变更通知：

### GodotPropertyBinder —— 基于信号

订阅 Godot 内置信号，使变更即时到达，没有帧延迟：

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

### GodotPollBasedPropertyBinder —— 逐帧轮询

对于没有专用信号的任意 `GodotObject` 属性，该 binder 会通过 `Observable.PollEveryUpdate` 每帧读取值，并在值变化时发出通知。由于依赖轮询，至多存在一帧的延迟。

## 类型转换器

如果不加上 `FloatToDoubleConverter`/`DoubleToFloatConverter`，那么在暴露 `double` 属性的 Godot 控件（例如 `Range.Value`、`ColorPicker.Color`）与 ViewModel 的 `float` 属性之间建立绑定时，会在绑定时抛出 `ConverterNotFoundException`。本库还附带 `EnumToStringConverter<TEnum>`、`StringToEnumConverter<TEnum>` 以及 `Variant` 与基元类型互转的转换器 —— 在 Autoload bootstrapper 中通过 `.WithConverter(...)` 注册你需要的那部分即可。
