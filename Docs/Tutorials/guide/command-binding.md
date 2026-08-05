# Command Binding

Bind a `ReactiveCommand` to a `BaseButton` (triggers on `Pressed`) or `LineEdit` (triggers on `TextSubmitted`). `CanExecute` automatically disables the control:

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

## Supported Controls

| Control | Trigger |
|---|---|
| `BaseButton` | `Pressed` signal |
| `LineEdit` | `TextSubmitted` signal |

When `CanExecute` returns `false`, the bound control is automatically disabled.
