using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Godot;
using QfStudio.Godette.IntegrationTests.ViewModels.Collection;
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;

namespace QfStudio.Godette.IntegrationTests.Views.Collection;

[SceneTree(root: "_root")]
[GodotViewFor<ItemListBinderTestViewModel>]
public partial class ItemListBinderTestScene : Control
{
    private readonly ItemListBinder<ItemListBinderTestItemViewModel> _binder = new(
        textSelector: vm => vm.Name);

    private ItemListBinderTestItemViewModel? _selectedVm;

    public ItemListBinderTestScene()
    {
        this.WhenActivated(d =>
        {
            _binder.Connect(MyItemList, ViewModel!.Items)
                .DisposeWith(d);

            AddButton.Pressed += () => ViewModel.AddItemCommand.Execute().Subscribe();
            InsertFrontButton.Pressed += () => ViewModel.InsertAtFrontCommand.Execute().Subscribe();
            RemoveButton.Pressed += () =>
            {
                if (_selectedVm != null)
                {
                    ViewModel.RemoveCommand.Execute(_selectedVm).Subscribe();
                    _selectedVm = null;
                }
            };
            ClearButton.Pressed += () => ViewModel.ClearCommand.Execute().Subscribe();
            ReplaceButton.Pressed += () =>
            {
                if (_selectedVm != null) ViewModel.ReplaceCommand.Execute(_selectedVm).Subscribe();
            };
            MoveButton.Pressed += () =>
            {
                if (_selectedVm != null) ViewModel.MoveToEndCommand.Execute(_selectedVm).Subscribe();
            };
            VerifyMappingButton.Pressed += VerifyMapping;

            _binder.ObserveSelection()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(vm =>
                {
                    _selectedVm = vm;
                    SelectedLabel.Text = $"Selected: {vm?.Name ?? "(none)"}";
                });
        });
    }

    public override void _Ready()
    {
        ViewModel = new ItemListBinderTestViewModel();

        BackButton.Pressed += () => GetTree().ChangeSceneToFile(HomeScene.TscnFilePath);
    }

    private void VerifyMapping()
    {
        var items = ViewModel!.Items;
        var itemList = MyItemList;
        var countMatch = items.Count == itemList.ItemCount;
        GD.Print($"[ItemListTest] Position check: Items.Count={items.Count}, ItemList.ItemCount={itemList.ItemCount} (match: {countMatch})");

        var positionMatches = 0;
        var positionChecks = 0;
        var count = Math.Min(items.Count, itemList.ItemCount);
        for (var i = 0; i < count; i++)
        {
            var expectedVm = items[i];
            var actualVm = _binder.GetViewModelByIndex(i);
            var match = ReferenceEquals(actualVm, expectedVm);
            positionChecks++;
            if (match) positionMatches++;
            GD.Print($"  [{i}] Item='{itemList.GetItemText(i)}' -> VM='{actualVm?.Name}' (expected='{expectedVm.Name}', match={match})");
        }

        var allPass = positionMatches == positionChecks && countMatch;
        GD.Print($"[ItemListTest] Summary: Position {positionMatches}/{positionChecks} -> {(allPass ? "PASS" : "FAIL")}");
    }
}
