using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Godot;
using QfStudio.Godette.IntegrationTests.ViewModels.Collection;
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;

namespace QfStudio.Godette.IntegrationTests.Views.Collection;

[SceneTree(root: "_root")]
[GodotViewFor<IndexedControlBinderTestViewModel>]
public partial class IndexedControlBinderTestScene : Control
{
    private readonly OptionButtonBinder<IndexedItemViewModel> _optionBinder = new(
        textSelector: vm => vm.Name);

    private readonly TabBarBinder<IndexedItemViewModel> _tabBinder = new(
        textSelector: vm => vm.Name);

    private readonly PopupMenuBinder<IndexedItemViewModel> _popupBinder = new(
        textSelector: vm => vm.Name);

    public IndexedControlBinderTestScene()
    {
        this.WhenActivated(d =>
        {
            _optionBinder.Connect(OptionSelect, ViewModel!.Items).DisposeWith(d);
            _tabBinder.Connect(TabBarSelect, ViewModel.Items).DisposeWith(d);
            _popupBinder.Connect(MenuButtonSelect.GetPopup(), ViewModel.Items).DisposeWith(d);

            _optionBinder.ObserveSelection()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(vm => OptionLabel.Text = $"Option: {vm?.Name ?? "(none)"}")
                .DisposeWith(d);

            _tabBinder.ObserveSelection()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(vm => TabLabel.Text = $"Tab: {vm?.Name ?? "(none)"}")
                .DisposeWith(d);

            _popupBinder.ObserveSelection()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(vm => PopupLabel.Text = $"Popup: {vm?.Name ?? "(none)"}")
                .DisposeWith(d);
        });
    }

    public override void _Ready()
    {
        ViewModel = new IndexedControlBinderTestViewModel();

        AddButton.Pressed += () => ViewModel.AddItemCommand.Execute().Subscribe();
        RemoveButton.Pressed += () => ViewModel.RemoveItemCommand.Execute().Subscribe();
        ClearButton.Pressed += () => ViewModel.ClearCommand.Execute().Subscribe();
        ReplaceButton.Pressed += () => ViewModel.ReplaceFirstCommand.Execute().Subscribe();
        MoveButton.Pressed += () => ViewModel.MoveCommand.Execute().Subscribe();
        VerifyMappingButton.Pressed += VerifyMapping;

        BackButton.Pressed += () => GetTree().ChangeSceneToFile(HomeScene.TscnFilePath);
    }

    private void VerifyMapping()
    {
        var items = ViewModel!.Items;

        // OptionButton check
        var optCount = Math.Min(items.Count, OptionSelect.ItemCount);
        var optMatches = 0;
        for (var i = 0; i < optCount; i++)
        {
            if (ReferenceEquals(_optionBinder.GetViewModelByIndex(i), items[i]))
                optMatches++;
        }
        GD.Print($"[IndexedControlTest] OptionButton: {optMatches}/{optCount}");

        // TabBar check
        var tabCount = Math.Min(items.Count, TabBarSelect.TabCount);
        var tabMatches = 0;
        for (var i = 0; i < tabCount; i++)
        {
            if (ReferenceEquals(_tabBinder.GetViewModelByIndex(i), items[i]))
                tabMatches++;
        }
        GD.Print($"[IndexedControlTest] TabBar: {tabMatches}/{tabCount}");

        var allPass = optMatches == optCount && tabMatches == tabCount;
        GD.Print($"[IndexedControlTest] Summary: {(allPass ? "PASS" : "FAIL")}");
    }
}
