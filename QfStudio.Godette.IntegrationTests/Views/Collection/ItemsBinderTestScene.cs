using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;
using Godot;
using QfStudio.Godette.IntegrationTests.ViewModels.Collection;
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;

namespace QfStudio.Godette.IntegrationTests.Views.Collection;

[SceneTree(root: "_root")]
[GodotViewFor<ItemsBinderTestViewModel>]
public partial class ItemsBinderTestScene : Control
{
    private readonly ItemsBinder<VBoxContainer, ItemLabel, ItemViewModel> _itemsBinder =
        new(Splat.Locator.Current.GetService<GodotViewLocator>()!);

    public ItemsBinderTestScene()
    {
        this.WhenActivated(d =>
        {
            _itemsBinder.Connect(ItemsContainer, ViewModel!.Items)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.AddItemCommand, v => v.AddButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.RemoveItemCommand, v => v.RemoveButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.DuplicateCommand, v => v.DuplicateButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ClearCommand, v => v.ClearButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.InsertAtFrontCommand, v => v.InsertFrontButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.InsertAtMiddleCommand, v => v.InsertMiddleButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ReplaceFirstCommand, v => v.ReplaceFirstButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.MoveCommand, v => v.MoveButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.RemoveAtMiddleCommand, v => v.RemoveMidButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.MoveLastToFrontCommand, v => v.MoveBackButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.AddRangeCommand, v => v.AddRangeButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.RemoveRangeCommand, v => v.RemoveRangeButton)
                .DisposeWith(d);
        });
    }

    public override void _Ready()
    {
        ViewModel = new ItemsBinderTestViewModel();

        BackButton.Pressed += () => GetTree().ChangeSceneToFile(HomeScene.TscnFilePath);

        VerifyMappingButton.Pressed += VerifyMapping;

        VerifyCustomBinder();
    }

    private void VerifyCustomBinder()
    {
        var container = new VBoxContainer();
        var items = new ObservableCollection<ItemViewModel>();
        items.Add(new ItemViewModel(items) { Name = "CustomA" });
        items.Add(new ItemViewModel(items) { Name = "CustomB" });
        var binder = new ItemsBinder<VBoxContainer, Label, ItemViewModel>(
            () => new Label(),
            (label, vm) => label.Text = $"VM:{vm.Name}");

        using var connection = binder.Connect(container, items);
        var passed = container.GetChildCount() == 2
            && container.GetChild<Label>(0).Text == "VM:CustomA"
            && container.GetChild<Label>(1).Text == "VM:CustomB";

        // ReplaceItem path must use the custom binder as well.
        items[0] = new ItemViewModel(items) { Name = "CustomC" };
        passed &= container.GetChild<Label>(0).Text == "VM:CustomC";

        GD.Print($"[ItemsTest] Custom binder (Label.Text) check -> {(passed ? "PASS" : "FAIL")}");

        container.QueueFree();
    }

    private void VerifyMapping()
    {
        var vmNodeMatches = 0;
        var vmNodeChecks = 0;

        // Mapping check
        foreach (var vm in ViewModel!.Items)
        {
            var nodes = _itemsBinder.GetNodesForViewModel(vm);
            GD.Print($"[ItemsTest] VM '{vm.Name}' -> {nodes.Count} node(s)");
            foreach (var node in nodes)
            {
                var retrievedVm = _itemsBinder.GetViewModelOfNode(node);
                var match = ReferenceEquals(retrievedVm, vm);
                vmNodeChecks++;
                if (match) vmNodeMatches++;
                GD.Print($"  Node '{node.Name}' -> VM '{retrievedVm?.Name}' (match: {match})");
            }
        }

        // Position check: Container.GetChild(i) <-> Items[i]
        var items = ViewModel!.Items;
        var container = ItemsContainer;
        var countMatch = items.Count == container.GetChildCount();
        GD.Print($"[ItemsTest] Position check: Items.Count={items.Count}, Container.ChildCount={container.GetChildCount()} (match: {countMatch})");

        var positionMatches = 0;
        var positionChecks = 0;
        var count = Math.Min(items.Count, container.GetChildCount());
        for (var i = 0; i < count; i++)
        {
            var expectedVm = items[i];
            var node = container.GetChild<ItemLabel>(i);
            var actualVm = _itemsBinder.GetViewModelOfNode(node);
            var match = ReferenceEquals(actualVm, expectedVm);
            positionChecks++;
            if (match) positionMatches++;
            GD.Print($"  [{i}] Node='{node.Name}' -> VM='{actualVm?.Name}' (expected='{expectedVm.Name}', match={match})");
        }

        var allPass = vmNodeMatches == vmNodeChecks
                      && positionMatches == positionChecks
                      && countMatch;
        GD.Print($"[ItemsTest] Summary: VM <-> Node {vmNodeMatches}/{vmNodeChecks}, Position {positionMatches}/{positionChecks}, Count {countMatch} -> {(allPass ? "PASS" : "FAIL")}");
    }
}
