using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows.Input;
using Godot;
using ReactiveUI;

namespace QfStudio.Godette.ReactiveUI;

/// <remarks>
/// If no commandSelector is provided, PopupMenu entries are clickable by default.
/// </remarks>
public class PopupMenuBinder<TViewModel> : CollectionBinderBase<PopupMenu, TViewModel>
    where TViewModel : class
{
    private readonly Expression<Func<TViewModel, string?>> _textSelector;
    private readonly Expression<Func<TViewModel, Texture2D?>>? _iconSelector;
    private readonly Expression<Func<TViewModel, ICommand?>>? _commandSelector;
    private readonly Func<TViewModel, ICommand?>? _commandSelectorFunc;
    private readonly Expression<Func<TViewModel, object?>>? _commandParameterSelector;
    private readonly Func<TViewModel, object?>? _commandParameterSelectorFunc;

    private readonly Dictionary<TViewModel, CompositeDisposable> _subscriptions = new();

    private IDisposable? _commandTriggerSubscription;

    public PopupMenuBinder(
        Expression<Func<TViewModel, string?>> textSelector,
        Expression<Func<TViewModel, Texture2D?>>? iconSelector = null,
        Expression<Func<TViewModel, ICommand?>>? commandSelector = null,
        Expression<Func<TViewModel, object?>>? commandParameterSelector = null)
    {
        _textSelector = textSelector;
        _iconSelector = iconSelector;
        _commandSelector = commandSelector;
        _commandSelectorFunc = commandSelector?.Compile();
        _commandParameterSelector = commandParameterSelector;
        _commandParameterSelectorFunc = commandParameterSelector?.Compile();
    }

    protected override void AddItem(int index, TViewModel viewModel)
    {
        if (index >= 0 && index < Container.ItemCount)
        {
            PopulateItems();
            return;
        }

        Container.AddItem("");
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
        WatchAnyValues(newViewModel);
    }

    // append-only control; No native move API
    protected override void MoveItem(int oldIndex, int newIndex) => PopulateItems();

    protected override void RemoveAllItems()
    {
        DisposeAllWatchers();
        _commandTriggerSubscription?.Dispose();
        _commandTriggerSubscription = null;
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

        if (_commandSelector != null)
        {
            IObservable<(ICommand? Cmd, object? Param)> cmdStream = _commandParameterSelector != null
                ? vm.WhenAnyValue(_commandSelector, _commandParameterSelector)
                    .Select(t => (Cmd: t.Item1, Param: t.Item2))
                : vm.WhenAnyValue(_commandSelector).Select(c => (Cmd: c, Param: (object?)null));

            cmdStream
                .Select(t => t.Cmd == null
                    ? Observable.Return(Unit.Default)
                        .ObserveOn(RxSchedulers.MainThreadScheduler)
                        .Do(_ => UpdateCommandEnabled(vm, null, null))
                    : Observable.Return(Unit.Default)
                        .Concat(Observable.FromEventPattern(
                                h => t.Cmd.CanExecuteChanged += h,
                                h => t.Cmd.CanExecuteChanged -= h)
                            .Select(_ => Unit.Default))
                        .ObserveOn(RxSchedulers.MainThreadScheduler)
                        .Do(_ => UpdateCommandEnabled(vm, t.Cmd, t.Param)))
                .Switch()
                .Subscribe()
                .DisposeWith(subscription);

            EnsureCommandTrigger();
        }
    }

    private void UpdateCommandEnabled(TViewModel vm, ICommand? cmd, object? param)
    {
        var idx = Collection.IndexOf(vm);
        if (idx < 0 || idx >= Container.ItemCount)
            return;

        if (cmd == null)
        {
            Container.SetItemDisabled(idx, false);
            return;
        }
        Container.SetItemDisabled(idx, !cmd.CanExecute(param));
    }

    private void EnsureCommandTrigger()
    {
        if (_commandSelector == null || _commandTriggerSubscription != null)
            return;

        _commandTriggerSubscription = Container.ObserveIdPressed()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(id =>
            {
                var vm = GetViewModelByIndex((int)id);
                if (vm == null)
                    return;
                var cmd = _commandSelectorFunc!(vm);
                if (cmd == null)
                    return;
                var param = _commandParameterSelectorFunc?.Invoke(vm);
                if (cmd.CanExecute(param))
                    cmd.Execute(param);
            });
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
        return Container.ObserveIdPressed()
            .Select(id => GetViewModelByIndex((int)id));
    }
}
