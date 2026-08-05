# Validation

[ReactiveUI.Validation](https://github.com/reactiveui/ReactiveUI.Validation) is a separate package -- install it first:

```
dotnet add package ReactiveUI.Validation
```

Then define rules on the ViewModel and bind error messages on the View:

```csharp
// usings: ReactiveUI, ReactiveUI.SourceGenerators,
//         ReactiveUI.Validation.Abstractions, ReactiveUI.Validation.Contexts,
//         ReactiveUI.Validation.Extensions

// ViewModel -- implement IActivatableViewModel and IValidatableViewModel
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
