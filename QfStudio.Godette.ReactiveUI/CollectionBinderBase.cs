using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace QfStudio.Godette.ReactiveUI;

public abstract class CollectionBinderBase<TContainer, TViewModel>
    where TViewModel : class
{
    public bool IsConnected { get; private set; }

    protected TContainer Container { get; private set; } = default!;

    protected IList<TViewModel> Collection { get; private set; } = null!;

    /// <remarks>
    /// Calling <c>Connect</c> grants the binder full control over the target container node.
    /// Any previously attached child nodes or items of the target container node will be removed.
    /// </remarks>
    public IDisposable Connect(TContainer container, IList<TViewModel> collection)
    {
        if (IsConnected)
            throw new InvalidOperationException("Already connected. Disconnect before reconnecting.");
        IsConnected = true;

        Container = container;
        Collection = collection;

        var disposable = new CompositeDisposable();

        Disposable.Create(() => IsConnected = false).DisposeWith(disposable);

        PopulateItems();

        if (collection is INotifyCollectionChanged notifyCollection)
        {
            NotifyCollectionChangedEventHandler handler = (_, e) => OnCollectionChanged(e);
            notifyCollection.CollectionChanged += handler;
            Disposable.Create(() => notifyCollection.CollectionChanged -= handler)
                .DisposeWith(disposable);
        }

        Disposable.Create(RemoveAllItems).DisposeWith(disposable);

        return disposable;
    }

    private void PopulateItems()
    {
        RemoveAllItems();
        foreach (var item in Collection)
            AddItem(item, -1);
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                HandleAdd(e);
                break;
            case NotifyCollectionChangedAction.Remove:
                HandleRemove(e);
                break;
            case NotifyCollectionChangedAction.Replace:
                HandleReplace(e);
                break;
            case NotifyCollectionChangedAction.Move:
                HandleMove(e);
                break;
            case NotifyCollectionChangedAction.Reset:
                HandleReset();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }

    protected virtual void HandleAdd(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
            return;

        var idx = e.NewStartingIndex;
        foreach (var item in e.NewItems)
            AddItem((TViewModel)item, idx < 0 ? -1 : idx++);
    }

    protected virtual void HandleRemove(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null)
            return;

        var start = e.OldStartingIndex;
        if (start < 0)
            throw new InvalidOperationException(
                "Collection remove must provide OldStartingIndex; use a collection that supplies indices (e.g. ObservableCollection<T>).");

        // Remove from highest to lowest to keep lower indices stable.
        for (var i = e.OldItems.Count - 1; i >= 0; i--)
            RemoveItem(start + i);
    }

    protected virtual void HandleReplace(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewStartingIndex < 0 || e.NewItems == null)
            throw new InvalidOperationException(
                "Collection replace must provide NewStartingIndex and NewItems; use a collection that supplies indices (e.g. ObservableCollection<T>).");

        for (var i = 0; i < e.NewItems.Count; i++)
            ReplaceItem(e.NewStartingIndex + i, (TViewModel)e.NewItems[i]!);
    }

    protected virtual void HandleMove(NotifyCollectionChangedEventArgs e)
        => MoveItem(e.OldStartingIndex, e.NewStartingIndex);

    protected virtual void HandleReset()
        => PopulateItems();

    protected abstract void AddItem(TViewModel viewModel, int index);

    protected abstract void RemoveItem(int index);

    protected abstract void ReplaceItem(int index, TViewModel viewModel);

    protected abstract void MoveItem(int oldIndex, int newIndex);

    protected abstract void RemoveAllItems();
}
