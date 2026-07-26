using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace QfStudio.Godette.IntegrationTests.ViewModels.Collection;

public partial class IndexedItemViewModel : ViewModelBase
{
    public IndexedItemViewModel(string name) => Name = name;

    [Reactive]
    public partial string Name { get; set; }
}

public class IndexedControlBinderTestViewModel : ViewModelBase
{
    public IndexedControlBinderTestViewModel()
    {
        Items.Add(new IndexedItemViewModel("Alpha"));
        Items.Add(new IndexedItemViewModel("Beta"));
        Items.Add(new IndexedItemViewModel("Gamma"));
        var counter = 3;

        AddItemCommand = ReactiveCommand.Create(() =>
        {
            counter++;
            Items.Add(new IndexedItemViewModel($"Item {counter}"));
        });

        RemoveItemCommand = ReactiveCommand.Create(() =>
        {
            if (Items.Count > 0)
                Items.RemoveAt(Items.Count - 1);
        });

        ClearCommand = ReactiveCommand.Create(() => Items.Clear());

        ReplaceFirstCommand = ReactiveCommand.Create(() =>
        {
            if (Items.Count > 0)
                Items[0] = new IndexedItemViewModel("Replaced " + DateTime.Now.Ticks);
        });

        MoveCommand = ReactiveCommand.Create(() =>
        {
            if (Items.Count > 1)
                Items.Move(0, Items.Count - 1);
        });
    }

    public ObservableCollection<IndexedItemViewModel> Items { get; } = [];

    public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceFirstCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveCommand { get; }
}
