# 验证

[ReactiveUI.Validation](https://github.com/reactiveui/ReactiveUI.Validation) 是一个独立的包 —— 请先安装它：

```
dotnet add package ReactiveUI.Validation
```

然后在 ViewModel 上定义规则，并在 View 上绑定错误信息：

```csharp
// 引入：ReactiveUI, ReactiveUI.SourceGenerators,
//       ReactiveUI.Validation.Abstractions, ReactiveUI.Validation.Contexts,
//       ReactiveUI.Validation.Extensions

// ViewModel — 实现 IActivatableViewModel 和 IValidatableViewModel
public partial class MyViewModel : ReactiveObject, IActivatableViewModel, IValidatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    [Reactive] public partial string Email { get; set; } = "";

    public MyViewModel()
    {
        this.ValidationRule(vm => vm.Email,
            email => !string.IsNullOrWhiteSpace(email) && email.Contains('@'),
            "Email must contain '@'.");
    }
}

// View
this.WhenActivated(d =>
{
    this.Bind(ViewModel, vm => vm.Email, v => v.EmailEdit.Text)
        .DisposeWith(d);
    this.BindValidation(ViewModel, vm => vm.Email, v => v.ErrorLabel.Text)
        .DisposeWith(d);
});
```
