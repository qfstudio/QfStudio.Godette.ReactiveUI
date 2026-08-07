# 路由

ReactiveUI 的 `RoutingState` 配合 `GodotViewLocator` 实现页面导航。本库提供了视图定位器和注册 API；你仍需一个小的适配器，在导航时切换子节点——下文的 `RoutedViewController` **并非** NuGet 包的一部分，而是可从 [IntegrationTests/Views/Routing/RoutedViewController.cs](https://github.com/qfstudio/QfStudio.Godette.ReactiveUI/blob/master/QfStudio.Godette.IntegrationTests/Views/Routing/RoutedViewController.cs) 复制的示例代码：

```csharp
// 初始化（在 _Ready 或构造函数中）
var locator = new GodotViewLocator();
locator.RegisterView<PageAView, PageAViewModel>(PageAView.TscnFilePath);
locator.RegisterView<PageBViewModel>(PageBView.TscnFilePath);

var shell = new ShellViewModel(); // 实现 IScreen，包含 RoutingState
var router = new RoutedViewController(shell.Router, locator); // 示例适配器，见上方说明
router.Connect(ContentContainer);

// 导航
shell.Router.Navigate.Execute(new PageAViewModel(shell));
shell.Router.NavigateBack.Execute().Subscribe();
```
