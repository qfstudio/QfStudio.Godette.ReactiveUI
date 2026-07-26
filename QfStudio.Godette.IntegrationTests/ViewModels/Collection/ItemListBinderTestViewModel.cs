using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace QfStudio.Godette.IntegrationTests.ViewModels.Collection;

public partial class ItemListBinderTestItemViewModel : ReactiveObject
{
    [Reactive]
    public partial string Name { get; set; }

    public ItemListBinderTestItemViewModel(string name) => Name = name;
}

public class ItemListBinderTestViewModel : ViewModelBase
{
    public ObservableCollection<ItemListBinderTestItemViewModel> Items { get; } = new();

    public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertAtFrontCommand { get; }
    public ReactiveCommand<ItemListBinderTestItemViewModel, Unit> RemoveCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<ItemListBinderTestItemViewModel, Unit> ReplaceCommand { get; }
    public ReactiveCommand<ItemListBinderTestItemViewModel, Unit> MoveToEndCommand { get; }

    public ItemListBinderTestViewModel()
    {
        // Initial items
        Items.Add(new ItemListBinderTestItemViewModel("Item A"));
        Items.Add(new ItemListBinderTestItemViewModel("Item B"));
        Items.Add(new ItemListBinderTestItemViewModel("Item C"));
        var counter = 3;

        AddItemCommand = ReactiveCommand.Create(() =>
        {
            counter++;
            Items.Add(new ItemListBinderTestItemViewModel($"Item {counter}"));
        });

        InsertAtFrontCommand = ReactiveCommand.Create(() =>
        {
            counter++;
            Items.Insert(0, new ItemListBinderTestItemViewModel($"Item {counter}"));
        });

        RemoveCommand = ReactiveCommand.Create<ItemListBinderTestItemViewModel>(vm =>
        {
            var index = Items.IndexOf(vm);
            if (index >= 0) Items.RemoveAt(index);
        });

        ClearCommand = ReactiveCommand.Create(() => Items.Clear());

        ReplaceCommand = ReactiveCommand.Create<ItemListBinderTestItemViewModel>(vm =>
        {
            var index = Items.IndexOf(vm);
            if (index >= 0)
                Items[index] = new ItemListBinderTestItemViewModel("Replaced " + DateTime.Now.Ticks);
        });

        MoveToEndCommand = ReactiveCommand.Create<ItemListBinderTestItemViewModel>(vm =>
        {
            var index = Items.IndexOf(vm);
            if (index >= 0 && Items.Count > 1)
                Items.Move(index, Items.Count - 1);
        });
    }
}
