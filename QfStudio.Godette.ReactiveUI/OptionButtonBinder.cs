using System.Reactive.Linq;
using Godot;

namespace QfStudio.Godette.ReactiveUI;

public class OptionButtonBinder<TViewModel> : CollectionBinderBase<OptionButton, TViewModel>
    where TViewModel : class
{
    private readonly Func<TViewModel, string>? _textSelector;
    private readonly Func<TViewModel, Texture2D?>? _iconSelector;

    public OptionButtonBinder(
        Func<TViewModel, string>? textSelector = null,
        Func<TViewModel, Texture2D?>? iconSelector = null)
    {
        _textSelector = textSelector;
        _iconSelector = iconSelector;
    }

    protected override void AddItem(int index, TViewModel viewModel)
    {
        var text = _textSelector?.Invoke(viewModel) ?? viewModel.ToString() ?? "";
        var icon = _iconSelector?.Invoke(viewModel);

        if (index >= 0 && index < Container.ItemCount)
        {
            PopulateItems();
            return;
        }

        var idx = Container.ItemCount;
        Container.AddItem(text);
        if (icon != null)
            Container.SetItemIcon(idx, icon);
    }

    protected override void RemoveItem(int index, TViewModel viewModel)
    {
        Container.RemoveItem(index);
    }

    protected override void ReplaceItem(int index, TViewModel viewModel)
    {
        var text = _textSelector?.Invoke(viewModel) ?? viewModel.ToString() ?? "";
        var icon = _iconSelector?.Invoke(viewModel);

        Container.SetItemText(index, text);
        Container.SetItemIcon(index, icon);
    }

    // Append-only control; no native move API.
    protected override void MoveItem(int oldIndex, int newIndex) => PopulateItems();

    protected override void RemoveAllItems() => Container.Clear();

    public TViewModel? GetViewModelByIndex(int index) =>
        index >= 0 && index < Collection.Count ? Collection[index] : null;

    public int GetIndexByViewModel(TViewModel viewModel) =>
        Collection.IndexOf(viewModel);

    public IObservable<TViewModel?> ObserveSelection()
    {
        return Container.ObserveItemSelected()
            .Select(index => GetViewModelByIndex((int)index));
    }
}
