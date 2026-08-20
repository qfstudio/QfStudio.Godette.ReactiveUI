using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Godot;
using ReactiveUI;

namespace QfStudio.Godette.ReactiveUI;

public class TabBarBinder<TViewModel> : CollectionBinderBase<TabBar, TViewModel>
    where TViewModel : class
{
    private readonly Expression<Func<TViewModel, string?>> _textSelector;
    private readonly Expression<Func<TViewModel, Texture2D?>>? _iconSelector;
    private readonly Expression<Func<TViewModel, bool?>>? _disabledSelector;

    private readonly Dictionary<TViewModel, CompositeDisposable> _subscriptions = new();

    public TabBarBinder(
        Expression<Func<TViewModel, string?>> textSelector,
        Expression<Func<TViewModel, Texture2D?>>? iconSelector = null,
        Expression<Func<TViewModel, bool?>>? disabledSelector = null)
    {
        _textSelector = textSelector;
        _iconSelector = iconSelector;
        _disabledSelector = disabledSelector;
    }

    protected override void AddItem(int index, TViewModel viewModel)
    {
        Container.AddTab("", null);
        if (index >= 0 && index < Container.TabCount - 1)
            Container.MoveTab(Container.TabCount - 1, index);

        WatchAnyValues(viewModel);
    }

    protected override void RemoveItem(int index, TViewModel viewModel)
    {
        UnwatchAnyValues(viewModel);
        Container.RemoveTab(index);
    }

    protected override void ReplaceItem(int index, TViewModel oldViewModel, TViewModel newViewModel)
    {
        UnwatchAnyValues(oldViewModel);
        WatchAnyValues(newViewModel);
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
        var newClamped = Math.Clamp(newIndex, 0, Container.TabCount - 1);
        if (oldIndex != newClamped)
            Container.MoveTab(oldIndex, newClamped);
    }

    protected override void RemoveAllItems()
    {
        DisposeAllWatchers();
        Container.ClearTabs();
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
                if (idx >= 0 && idx < Container.TabCount)
                    Container.SetTabTitle(idx, text ?? "");
            })
            .DisposeWith(subscription);

        if (_iconSelector != null)
        {
            vm.WhenAnyValue(_iconSelector)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(icon =>
                {
                    var idx = Collection.IndexOf(vm);
                    if (idx >= 0 && idx < Container.TabCount)
                        Container.SetTabIcon(idx, icon);
                })
                .DisposeWith(subscription);
        }

        if (_disabledSelector != null)
        {
            vm.WhenAnyValue(_disabledSelector)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(disabled =>
                {
                    var idx = Collection.IndexOf(vm);
                    if (idx >= 0 && idx < Container.TabCount)
                        Container.SetTabDisabled(idx, disabled ?? false);
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
        return Container.ObserveTabChanged()
            .Select(index => GetViewModelByIndex((int)index));
    }
}
