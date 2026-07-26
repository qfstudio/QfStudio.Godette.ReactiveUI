using System.Collections.ObjectModel;
using System.Reactive;
using Godot;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace QfStudio.Godette.IntegrationTests.ViewModels.Collection;

public partial class IndexedItemViewModel : ViewModelBase
{
    public IndexedItemViewModel(string name) => Name = name;

    [Reactive]
    public partial string Name { get; set; }

    [Reactive]
    public partial Texture2D? Icon { get; set; }
}

public partial class IndexedControlBinderTestViewModel : ViewModelBase
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

        InsertInMiddleCommand = ReactiveCommand.Create(() =>
        {
            counter++;
            var mid = Items.Count / 2;
            Items.Insert(mid, new IndexedItemViewModel($"Item {counter}"));
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

        ChangeTextCommand = ReactiveCommand.Create(() =>
        {
            foreach (var item in Items)
            {
                var suffix = Random.Shared.Next(1000).ToString();
                item.Name = $"Text {suffix}";
            }
        });

        ChangeIconCommand = ReactiveCommand.Create(() =>
        {
            foreach (var item in Items)
            {
                if (Random.Shared.Next(2) == 0)
                {
                    item.Icon = null;
                }
                else
                {
                    var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
                    image.Fill(Color.Color8((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256)));
                    item.Icon = ImageTexture.CreateFromImage(image);
                }
            }
        });

        ChangeSelectedTextCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem != null)
                SelectedItem.Name = "Selected " + Random.Shared.Next(1000);
        });

        ChangeSelectedIconCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedItem == null) 
                return;
                
            if (Random.Shared.Next(2) == 0)
            {
                SelectedItem.Icon = null;
            }
            else
            {
                var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
                image.Fill(Color.Color8((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256)));
                SelectedItem.Icon = ImageTexture.CreateFromImage(image);
            }
        });
    }

    public ObservableCollection<IndexedItemViewModel> Items { get; } = [];

    [Reactive]
    public partial IndexedItemViewModel? SelectedItem { get; set; }

    public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertInMiddleCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplaceFirstCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveCommand { get; }
    public ReactiveCommand<Unit, Unit> ChangeTextCommand { get; }
    public ReactiveCommand<Unit, Unit> ChangeIconCommand { get; }
    public ReactiveCommand<Unit, Unit> ChangeSelectedTextCommand { get; }
    public ReactiveCommand<Unit, Unit> ChangeSelectedIconCommand { get; }
}
