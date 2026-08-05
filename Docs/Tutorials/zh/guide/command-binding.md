# 命令绑定

将 `ReactiveCommand` 绑定到 `BaseButton`（在 `Pressed` 时触发）或 `LineEdit`（在 `TextSubmitted` 时触发）。`CanExecute` 会自动禁用控件：

```csharp
this.WhenActivated(d =>
{
    // Button press executes the command
    this.BindCommand(ViewModel, vm => vm.SaveCommand, v => v.SaveButton)
        .DisposeWith(d);

    // LineEdit submits the command, passing the current text as parameter
    this.BindCommand(ViewModel, vm => vm.SearchCommand, v => v.SearchEdit,
            vm => vm.QueryString)
        .DisposeWith(d);

    // Conditional command
    this.Bind(ViewModel, vm => vm.IsEnabled, v => v.CheckButton.ButtonPressed)
        .DisposeWith(d);
    this.BindCommand(ViewModel, vm => vm.DoWorkCommand, v => v.WorkButton)
        .DisposeWith(d);
});
```

## 支持的控件

| 控件 | 触发方式 |
|---|---|
| `BaseButton` | `Pressed` 信号 |
| `LineEdit` | `TextSubmitted` 信号 |

当 `CanExecute` 返回 `false` 时，绑定的控件会被自动禁用。
