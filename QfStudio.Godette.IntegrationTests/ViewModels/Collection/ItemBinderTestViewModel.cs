using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace QfStudio.Godette.IntegrationTests.ViewModels.Collection;

public class ItemBinderTestItemViewModel : ReactiveObject
{
    private string _name = "";

    public ItemBinderTestItemViewModel(string name) => Name = name;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public ObservableCollection<ItemBinderTestItemViewModel> Children { get; } = new();
}

public class ItemBinderTestViewModel : ReactiveObject
{
    private int _counter;

    public ItemBinderTestViewModel()
    {
        Items.Add(new ItemBinderTestItemViewModel("Alpha"));
        Items.Add(new ItemBinderTestItemViewModel("Beta"));
        Items.Add(new ItemBinderTestItemViewModel("Gamma"));
        _counter = 3;

        AddItemCommand = ReactiveCommand.Create(() =>
        {
            _counter++;
            Items.Add(new ItemBinderTestItemViewModel($"Item {_counter}"));
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
                Items[0] = new ItemBinderTestItemViewModel("Replaced " + DateTime.Now.Ticks);
        });

        MoveCommand = ReactiveCommand.Create(() =>
        {
            if (Items.Count > 1)
                Items.Move(0, Items.Count - 1);
        });
    }

    public ObservableCollection<ItemBinderTestItemViewModel> Items { get; } = new();

    public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceFirstCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveCommand { get; }
}
