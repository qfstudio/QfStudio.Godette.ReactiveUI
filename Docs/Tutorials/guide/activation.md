# Activation Lifecycle

When a view is activated (entering the scene tree and ready), `WhenActivated` fires. All subscriptions registered via `DisposeWith(d)` are cleaned up on deactivation:

```csharp
public MyScene()
{
    this.WhenActivated(d =>
    {
        // subscriptions are disposed when the view is deactivated
        this.WhenAnyValue(x => x.ViewModel!.Name)
            .Subscribe(name => GD.Print($"Name: {name}"))
            .DisposeWith(d);

        Disposable.Create(() => GD.Print("deactivated"))
            .DisposeWith(d);
    });
}
```
