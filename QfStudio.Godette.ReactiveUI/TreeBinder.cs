using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Godot;

namespace QfStudio.Godette.ReactiveUI;

public class TreeBinder<TViewModel> : CollectionBinderBase<Tree, TViewModel>
    where TViewModel : class
{
    private readonly Func<TViewModel, string>? _textSelector;
    private readonly Func<TViewModel, Texture2D?>? _iconSelector;
    private readonly Func<TViewModel, IList<TViewModel>>? _childrenSelector;

    private readonly Dictionary<TViewModel, TreeItem> _vmToItem = new();
    private readonly Dictionary<TreeItem, TViewModel> _itemToVm = new();
    private readonly Dictionary<TViewModel, CompositeDisposable> _subscriptions = new();
    private readonly Dictionary<TViewModel, TViewModel> _childToParent = new();

    public TreeBinder(
        Func<TViewModel, string>? textSelector = null,
        Func<TViewModel, Texture2D?>? iconSelector = null)
    {
        _textSelector = textSelector;
        _iconSelector = iconSelector;
    }

    public TreeBinder(
        Func<TViewModel, IList<TViewModel>> childrenSelector,
        Func<TViewModel, string>? textSelector = null,
        Func<TViewModel, Texture2D?>? iconSelector = null)
        : this(textSelector, iconSelector)
    {
        _childrenSelector = childrenSelector;
    }

    // index is ignored: Tree is organized by hierarchy.
    protected override void AddItem(int index, TViewModel viewModel)
        => AddVm(viewModel, null);

    protected override void RemoveItem(int index, TViewModel viewModel)
    {
        // For nested collections the base index is relative to the child collection,
        // not the root. Route to the actual owning collection.
        if (index < 0 || index >= Collection.Count || !ReferenceEquals(Collection[index], viewModel))
        {
            if (_childToParent.TryGetValue(viewModel, out var parentVm)
                && _childrenSelector != null
                && _vmToItem.ContainsKey(parentVm))
            {
                var children = _childrenSelector(parentVm);
                var childIndex = children.IndexOf(viewModel);
                if (childIndex >= 0)
                {
                    children.RemoveAt(childIndex);
                    return;
                }
            }
        }

        RemoveVm(viewModel);
    }

    // No fine-grained Replace/Move API on Tree.
    protected override void ReplaceItem(int index, TViewModel viewModel) => RebuildAll();
    protected override void MoveItem(int oldIndex, int newIndex) => RebuildAll();

    protected override void RemoveAllItems()
    {
        Container.Clear();
        _vmToItem.Clear();
        _itemToVm.Clear();
        DisposeAllSubscriptions();
        _childToParent.Clear();
    }

    private void AddVm(TViewModel vm, TreeItem? parent)
    {
        if (_vmToItem.ContainsKey(vm))
            return;

        var item = Container.CreateItem(parent);
        var text = _textSelector?.Invoke(vm) ?? vm.ToString() ?? "";
        item.SetText(0, text);
        var icon = _iconSelector?.Invoke(vm);
        if (icon != null) item.SetIcon(0, icon);

        _vmToItem[vm] = item;
        _itemToVm[item] = vm;

        if (_childrenSelector != null)
        {
            var children = _childrenSelector(vm);
            foreach (var child in children)
                AddVm(child, item);

            if (children is INotifyCollectionChanged notifyCollection)
                SubscribeToChildren(vm, notifyCollection);
        }
    }

    private void RemoveVm(TViewModel vm)
    {
        if (!_vmToItem.TryGetValue(vm, out var item)) return;

        DisposeDescendantSubscriptions(vm);

        var child = item.GetFirstChild();
        while (child != null)
        {
            var next = child.GetNext();
            if (_itemToVm.TryGetValue(child, out var childVm))
            {
                _vmToItem.Remove(childVm);
                _itemToVm.Remove(child);
                _childToParent.Remove(childVm);
            }
            child = next;
        }

        _vmToItem.Remove(vm);
        _itemToVm.Remove(item);
        _childToParent.Remove(vm);
        item.Free();
    }

    private void RebuildAll()
    {
        RemoveAllItems();
        foreach (var vm in Collection)
            AddVm(vm, null);
    }

    #region Child collection change tracking

    private void SubscribeToChildren(TViewModel parentVm, INotifyCollectionChanged notifyCollection)
    {
        var disposable = new CompositeDisposable();

        NotifyCollectionChangedEventHandler handler = (_, e) => OnChildrenCollectionChanged(parentVm, e);
        notifyCollection.CollectionChanged += handler;
        Disposable.Create(() => notifyCollection.CollectionChanged -= handler)
            .DisposeWith(disposable);

        _subscriptions[parentVm] = disposable;
    }

    private void OnChildrenCollectionChanged(TViewModel parentVm, NotifyCollectionChangedEventArgs e)
    {
        if (!_vmToItem.ContainsKey(parentVm))
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                HandleChildAdd(parentVm, e);
                break;
            case NotifyCollectionChangedAction.Remove:
                HandleChildRemove(parentVm, e);
                break;
            case NotifyCollectionChangedAction.Replace:
                HandleChildReplace(parentVm, e);
                break;
            case NotifyCollectionChangedAction.Move:
                HandleChildMove(parentVm, e);
                break;
            case NotifyCollectionChangedAction.Reset:
                RebuildAll();
                break;
        }
    }

    private void HandleChildAdd(TViewModel parentVm, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null) return;
        var parentItem = _vmToItem[parentVm];
        var idx = e.NewStartingIndex;
        foreach (TViewModel childVm in e.NewItems)
        {
            var siblingItem = parentItem.GetChild(idx);
            var newTreeItem = Container.CreateItem(siblingItem);
            var text = _textSelector?.Invoke(childVm) ?? childVm.ToString() ?? "";
            newTreeItem.SetText(0, text);
            var icon = _iconSelector?.Invoke(childVm);
            if (icon != null) newTreeItem.SetIcon(0, icon);

            _vmToItem[childVm] = newTreeItem;
            _itemToVm[newTreeItem] = childVm;
            _childToParent[childVm] = parentVm;

            if (_childrenSelector != null)
            {
                var children = _childrenSelector(childVm);
                foreach (var grandchild in children)
                    AddVm(grandchild, newTreeItem);

                if (children is INotifyCollectionChanged notifyCollection)
                    SubscribeToChildren(childVm, notifyCollection);
            }

            idx++;
        }
    }

    private void HandleChildRemove(TViewModel parentVm, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null) return;
        var curIdx = e.OldStartingIndex;
        foreach (TViewModel childVm in e.OldItems)
        {
            if (_vmToItem.ContainsKey(childVm))
                RemoveVm(childVm);
            curIdx++;
        }
    }

    private void HandleChildReplace(TViewModel parentVm, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null || e.NewItems == null) return;
        for (var i = 0; i < e.OldItems.Count; i++)
        {
            if (e.OldItems[i] is TViewModel oldVm)
                RemoveVm(oldVm);
        }
        var parentItem = _vmToItem[parentVm];
        var idx = e.NewStartingIndex;
        foreach (TViewModel newVm in e.NewItems)
        {
            var siblingItem = parentItem.GetChild(idx);
            var newTreeItem = Container.CreateItem(siblingItem);
            var text = _textSelector?.Invoke(newVm) ?? newVm.ToString() ?? "";
            newTreeItem.SetText(0, text);
            var icon = _iconSelector?.Invoke(newVm);
            if (icon != null) newTreeItem.SetIcon(0, icon);

            _vmToItem[newVm] = newTreeItem;
            _itemToVm[newTreeItem] = newVm;
            _childToParent[newVm] = parentVm;

            if (_childrenSelector != null)
            {
                var children = _childrenSelector(newVm);
                foreach (var grandchild in children)
                    AddVm(grandchild, newTreeItem);

                if (children is INotifyCollectionChanged notifyCollection)
                    SubscribeToChildren(newVm, notifyCollection);
            }

            idx++;
        }
    }

    private void HandleChildMove(TViewModel parentVm, NotifyCollectionChangedEventArgs e)
    {
        // Tree has no native reorder API; rebuild all.
        RebuildAll();
    }

    #endregion

    #region Subscription cleanup

    private void DisposeDescendantSubscriptions(TViewModel vm)
    {
        if (_subscriptions.TryGetValue(vm, out var disposable))
        {
            disposable.Dispose();
            _subscriptions.Remove(vm);
        }

        if (_childrenSelector != null)
        {
            foreach (var child in _childrenSelector(vm))
                DisposeDescendantSubscriptions(child);
        }
    }

    private void DisposeAllSubscriptions()
    {
        foreach (var kv in _subscriptions)
            kv.Value.Dispose();
        _subscriptions.Clear();
    }

    #endregion

    public TViewModel? GetViewModelByItem(TreeItem item) =>
        _itemToVm.GetValueOrDefault(item);

    public TreeItem? GetItemByViewModel(TViewModel viewModel) =>
        _vmToItem.GetValueOrDefault(viewModel);

    public IObservable<TViewModel?> ObserveSelection()
    {
        return Container.ObserveItemSelected()
            .Select(_ =>
            {
                var selected = Container.GetSelected();
                return selected != null ? GetViewModelByItem(selected) : null;
            });
    }
}
