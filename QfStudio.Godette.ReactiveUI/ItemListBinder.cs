using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Godot;
using ReactiveUI;

namespace QfStudio.Godette.ReactiveUI;

// TODO consider exposing the SelectedItem property as a bindable property
public class ItemListBinder<TViewModel> : CollectionBinderBase<ItemList, TViewModel>
    where TViewModel : class
{
    private readonly Expression<Func<TViewModel, string?>> _textSelector;
    private readonly Expression<Func<TViewModel, Texture2D?>>? _iconSelector;

    private readonly Dictionary<TViewModel, CompositeDisposable> _subscriptions = new();

    public ItemListBinder(
        Expression<Func<TViewModel, string?>> textSelector,
        Expression<Func<TViewModel, Texture2D?>>? iconSelector = null)
    {
        _textSelector = textSelector;
        _iconSelector = iconSelector;
    }

    protected override void AddItem(int index, TViewModel viewModel)
    {
        Container.AddItem("", null, true);
        if (index >= 0 && index < Container.ItemCount - 1)
            Container.MoveItem(Container.ItemCount - 1, index);

        WatchAnyValues(viewModel);
    }

    protected override void RemoveItem(int index, TViewModel viewModel)
    {
        UnwatchAnyValues(viewModel);
        Container.RemoveItem(index);
    }

    protected override void ReplaceItem(int index, TViewModel oldViewModel, TViewModel newViewModel)
    {
        UnwatchAnyValues(oldViewModel);

        if (index < 0 || index >= Collection.Count)
            return;

        WatchAnyValues(newViewModel);
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
        var clamped = Math.Clamp(newIndex, 0, Container.ItemCount - 1);
        if (oldIndex != clamped)
            Container.MoveItem(oldIndex, clamped);
    }

    protected override void RemoveAllItems()
    {
        DisposeAllWatchers();
        Container.Clear();
    }

    private void WatchAnyValues(TViewModel vm)
    {
        var subscription = new CompositeDisposable();
        _subscriptions[vm] = subscription;

        vm.WhenAnyValue(_textSelector)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(text =>
            {
                var idx = Collection.IndexOf(vm);
                if (idx >= 0 && idx < Container.ItemCount)
                    Container.SetItemText(idx, text ?? "");
            })
            .DisposeWith(subscription);

        if (_iconSelector != null)
        {
            vm.WhenAnyValue(_iconSelector)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(icon =>
                {
                    var idx = Collection.IndexOf(vm);
                    if (idx >= 0 && idx < Container.ItemCount)
                        Container.SetItemIcon(idx, icon);
                })
                .DisposeWith(subscription);
        }
    }

    private void UnwatchAnyValues(TViewModel vm)
    {
        if (_subscriptions.TryGetValue(vm, out var subscription))
        {
            subscription.Dispose();
            _subscriptions.Remove(vm);
        }
    }

    private void DisposeAllWatchers()
    {
        foreach (var subscription in _subscriptions.Values)
            subscription.Dispose();
        _subscriptions.Clear();
    }

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
