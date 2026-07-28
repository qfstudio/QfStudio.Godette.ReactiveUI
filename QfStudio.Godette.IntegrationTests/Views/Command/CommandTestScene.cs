using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Godot;
using QfStudio.Godette.IntegrationTests.ViewModels.Command;
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;

namespace QfStudio.Godette.IntegrationTests.Views.Command;

[SceneTree(root: "_root")]
[GodotViewFor<CommandTestViewModel>]
public partial class CommandTestScene : Control
{
    public CommandTestScene()
    {
        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.SimpleCommand, v => v.SimpleCommandButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.AsyncCommand, v => v.AsyncCommandButton)
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm!.QueryString, v => v.SearchEdit.Text)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.SearchCommand, v => v.SearchEdit,
                    vm => vm.QueryString)
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.ConditionalCommandEnabled, v => v.ConditionalCommandCheckButton.ButtonPressed)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.ConditionalCommand, v => v.ConditionalCommandButton)
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.InputText, v => v.EvenNumberCommandLineEdit.Text)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.AcceptEvenNumbersOnlyCommand, v => v.EvenNumberCommandButton,
                    this.WhenAnyValue(x => x.ViewModel!.InputDigits).Switch())
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.LineEditSubmitEnabled, v => v.LineEditSubmitToggleCheckButton.ButtonPressed)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.LineEditSubmitCommand, v => v.LineEditSubmitLineEdit)
                .DisposeWith(d);
        });
    }

    public override void _Ready()
    {
        ViewModel = new CommandTestViewModel();

        BackButton.Pressed += () => GetTree().ChangeSceneToFile(HomeScene.TscnFilePath);
    }
}
