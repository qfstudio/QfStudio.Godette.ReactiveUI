using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Godot;
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;

namespace QfStudio.Godette.IntegrationTests.Views.Collection;

[SceneTree(root: "_root")]
[GodotViewFor<ViewModels.Collection.ItemViewModel>]
public partial class ItemLabel : HBoxContainer
{
    public ItemLabel()
    {
        this.WhenActivated(d =>
        {
            this.Bind(ViewModel, vm => vm.Name, v => v.NameEdit.Text)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.Score)
                .Subscribe(score => ScoreLabel.Text = score.ToString())
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.MoveUp, v => v.MoveUpButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.MoveDown, v => v.MoveDownButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Duplicate, v => v.DuplicateButton)
                .DisposeWith(d);
        });
    }
}
