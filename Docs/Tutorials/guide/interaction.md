# Interaction

Bind a ViewModel's `Interaction<TInput, TOutput>` to a View-level handler (e.g. a dialog):

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
