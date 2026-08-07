# 激活生命周期

当 View 被激活（进入场景树并就绪）时，`WhenActivated` 触发。所有通过 `DisposeWith(d)` 注册的订阅都会在停用时被清理：

```csharp
public MyScene()
{
    this.WhenActivated(d =>
    {
        // 视图停用时自动释放订阅
        this.WhenAnyValue(x => x.ViewModel!.Name)
            .Subscribe(name => GD.Print($"Name: {name}"))
            .DisposeWith(d);

        Disposable.Create(() => GD.Print("deactivated"))
            .DisposeWith(d);
    });
}
```
