using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Godot;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace QfStudio.Godette.IntegrationTests.ViewModels.Collection;

public partial class ItemViewModel : ViewModelBase
{
    private readonly ObservableCollection<ItemViewModel> _items;

    public ItemViewModel(ObservableCollection<ItemViewModel> items)
    {
        _items = items;

        MoveUp = ReactiveCommand.Create(() =>
        {
            var idx = _items.IndexOf(this);
            if (idx > 0)
                _items.Move(idx, idx - 1);
        });

        MoveDown = ReactiveCommand.Create(() =>
        {
            var idx = _items.IndexOf(this);
            if (idx >= 0 && idx < _items.Count - 1)
                _items.Move(idx, idx + 1);
        });

        Duplicate = ReactiveCommand.Create(() =>
        {
            var idx = _items.IndexOf(this);
            if (idx >= 0)
                _items.Insert(idx + 1, new ItemViewModel(_items)
                {
                    Name = $"{Name} (copy)",
                    Score = Score
                });
        });
    }

    [Reactive]
    public partial string Name { get; set; } = "";

    [Reactive]
    public partial int Score { get; set; }

    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public ReactiveCommand<Unit, Unit> Duplicate { get; }
}

public partial class ItemsBinderTestViewModel : ReactiveObject
{
    public ObservableCollection<ItemViewModel> Items { get; } = new();

    public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertAtFrontCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertAtMiddleCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceFirstCommand { get; }

    public ItemsBinderTestViewModel()
    {
        Items.Add(new ItemViewModel(Items) { Name = "Alice", Score = 10 });
        Items.Add(new ItemViewModel(Items) { Name = "Bob", Score = 20 });
        Items.Add(new ItemViewModel(Items) { Name = "Charlie", Score = 30 });

        var hasItems = this.WhenAnyValue(x => x.Items.Count).Select(count => count > 0);

        AddItemCommand = ReactiveCommand.Create(() =>
        {
            var item = new ItemViewModel(Items) { Name = $"Item {Items.Count + 1}", Score = 0 };
            Items.Add(item);
            GD.Print($"[ItemsTest] Added: {item.Name}");
        });

        RemoveItemCommand = ReactiveCommand.Create(() =>
        {
            if (Items.Count > 0)
            {
                var item = Items[^1];
                Items.RemoveAt(Items.Count - 1);
                GD.Print($"[ItemsTest] Removed: {item.Name}");
            }
        }, hasItems);

        DuplicateCommand = ReactiveCommand.Create(() =>
        {
            if (Items.Count > 0)
            {
                var item = Items[0];
                Items.Add(item);
                GD.Print($"[ItemsTest] Duplicated: {item.Name}");
            }
        }, hasItems);

        ClearCommand = ReactiveCommand.Create(() =>
        {
            Items.Clear();
            GD.Print("[ItemsTest] Cleared");
        }, hasItems);

        InsertAtFrontCommand = ReactiveCommand.Create(() =>
        {
            var item = new ItemViewModel(Items) { Name = $"Front {Items.Count + 1}", Score = 0 };
            Items.Insert(0, item);
            GD.Print($"[ItemsTest] Inserted at front: {item.Name}");
        });

        InsertAtMiddleCommand = ReactiveCommand.Create(() =>
        {
            var mid = Items.Count / 2;
            var item = new ItemViewModel(Items) { Name = $"Mid {Items.Count + 1}", Score = 0 };
            Items.Insert(mid, item);
            GD.Print($"[ItemsTest] Inserted at middle ({mid}): {item.Name}");
        }, hasItems);

        ReplaceFirstCommand = ReactiveCommand.Create(() =>
        {
            if (Items.Count > 0)
            {
                var old = Items[0];
                var item = new ItemViewModel(Items) { Name = $"Replaced {old.Name}", Score = old.Score + 1 };
                Items[0] = item;
                GD.Print($"[ItemsTest] Replaced first: {old.Name} → {item.Name}");
            }
        }, hasItems);
    }
}
