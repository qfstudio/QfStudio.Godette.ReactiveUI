# Routing

ReactiveUI's `RoutingState` works with `GodotViewLocator` for page navigation. The library provides the view locator and registration API; you still need a small adapter to swap child nodes on navigation -- `RoutedViewController` below is **not** part of the NuGet package, it is sample code you can copy from [IntegrationTests/Views/Routing/RoutedViewController.cs](https://github.com/qfstudio/QfStudio.Godette.ReactiveUI/blob/master/QfStudio.Godette.IntegrationTests/Views/Routing/RoutedViewController.cs):

```csharp
// Setup (in _Ready or constructor)
var locator = new GodotViewLocator();
locator.RegisterView<PageAView, PageAViewModel>(PageAView.TscnFilePath);
locator.RegisterView<PageBViewModel>(PageBView.TscnFilePath);

var shell = new ShellViewModel(); // implements IScreen with RoutingState
var router = new RoutedViewController(shell.Router, locator); // sample adapter
router.Connect(ContentContainer);

// Navigate
shell.Router.Navigate.Execute(new PageAViewModel(shell));
shell.Router.NavigateBack.Execute().Subscribe();
```
