using System.Reactive.Linq;
using Godot;

namespace QfStudio.Godette.ReactiveUI;

public class TabBarBinder<TViewModel> : CollectionBinderBase<TabBar, TViewModel>
    where TViewModel : class
{
    private readonly Func<TViewModel, string>? _textSelector;
    private readonly Func<TViewModel, Texture2D?>? _iconSelector;

    public TabBarBinder(
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

        if (index >= 0 && index < Container.TabCount)
        {
            PopulateItems();
            return;
        }

        Container.AddTab(text, icon);
    }

    protected override void RemoveItem(int index, TViewModel viewModel)
    {
        Container.RemoveTab(index);
    }

    protected override void ReplaceItem(int index, TViewModel viewModel)
    {
        var text = _textSelector?.Invoke(viewModel) ?? viewModel.ToString() ?? "";
        var icon = _iconSelector?.Invoke(viewModel);

        Container.SetTabTitle(index, text);
        Container.SetTabIcon(index, icon);
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
        var newClamped = Math.Clamp(newIndex, 0, Container.TabCount - 1);
        if (oldIndex != newClamped)
            Container.MoveTab(oldIndex, newClamped);
    }

    protected override void RemoveAllItems() => Container.ClearTabs();

    public TViewModel? GetViewModelByIndex(int index) =>
        index >= 0 && index < Collection.Count ? Collection[index] : null;

    public int GetIndexByViewModel(TViewModel viewModel) =>
        Collection.IndexOf(viewModel);

    public IObservable<TViewModel?> ObserveSelection()
    {
        return Container.ObserveTabChanged()
            .Select(index => GetViewModelByIndex((int)index));
    }
}
