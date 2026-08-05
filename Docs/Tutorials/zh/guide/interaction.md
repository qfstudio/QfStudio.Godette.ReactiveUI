# 交互

将 ViewModel 的 `Interaction<TInput, TOutput>` 绑定到 View 层的处理器（例如对话框）：

```csharp
// ViewModel
public Interaction<string, bool> ConfirmDelete { get; } = new();
DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
{
    var confirmed = await ConfirmDelete.Handle("Confirm to delete?");
    ResultText = confirmed ? "Confirmed" : "Canceled";
});

// View
this.WhenActivated(d =>
{
    this.BindInteraction(ViewModel, vm => vm.ConfirmDelete, async context =>
    {
        ConfirmDialog.DialogText = context.Input;
        ConfirmDialog.PopupCentered();
        var tcs = new TaskCompletionSource<bool>();
        ConfirmDialog.Confirmed += () => tcs.TrySetResult(true);
        ConfirmDialog.Canceled += () => tcs.TrySetResult(false);
        context.SetOutput(await tcs.Task);
    }).DisposeWith(d);
});
```
