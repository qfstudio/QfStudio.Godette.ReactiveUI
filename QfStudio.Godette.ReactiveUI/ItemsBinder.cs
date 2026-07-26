using ReactiveUI;

namespace QfStudio.Godette.ReactiveUI;

/// <summary>
/// Binds an <see cref="IList{TViewModel}"/> to a Godot container by creating and
/// managing child nodes via <see cref="NodeBuilder"/>.
/// </summary>
public class ItemsBinder<TContainerNode, TNode, TViewModel> : CollectionBinderBase<TContainerNode, TViewModel>
    where TContainerNode : Godot.Node
    where TNode : Godot.Node
    where TViewModel : class
{
    private readonly List<TNode> _nodes = [];
    private readonly Dictionary<TNode, TViewModel> _nodeToViewModel = new();

    public ItemsBinder(IViewLocator viewLocator) : this(() =>
        viewLocator.ResolveView<TViewModel>() as TNode ??
        throw new InvalidOperationException("Cannot resolve view for view model."))
    {
    }

    public ItemsBinder(Func<TNode> nodeBuilder)
    {
        NodeBuilder = nodeBuilder;
    }

    protected Func<TNode> NodeBuilder { get; }

    protected override void AddItem(int index, TViewModel viewModel)
    {
        var node = NodeBuilder();
        ApplyViewModel(node, viewModel);

        Container.AddChild(node);
        if (index >= 0 && index < _nodes.Count)
        {
            Container.MoveChild(node, index);
            _nodes.Insert(index, node);
        }
        else
        {
            _nodes.Add(node);
        }

        _nodeToViewModel[node] = viewModel;
    }

    protected override void RemoveItem(int index, TViewModel viewModel)
    {
        if (index < 0 || index >= _nodes.Count)
            return;

        var node = _nodes[index];
        _nodes.RemoveAt(index);
        _nodeToViewModel.Remove(node);
        Container.RemoveChild(node);
        node.QueueFree();
    }

    protected override void ReplaceItem(int index, TViewModel oldViewModel, TViewModel newViewModel)
    {
        if (index < 0 || index >= _nodes.Count)
            return;

        var node = _nodes[index];
        _nodeToViewModel[node] = newViewModel;
        ApplyViewModel(node, newViewModel);
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _nodes.Count)
            return;
        var newClamped = Math.Clamp(newIndex, 0, _nodes.Count - 1);

        var node = _nodes[oldIndex];
        _nodes.RemoveAt(oldIndex);
        Container.MoveChild(node, newClamped);
        _nodes.Insert(newClamped, node);
    }

    protected override void RemoveAllItems()
    {
        foreach (var node in Container.GetChildren())
        {
            Container.RemoveChild(node);
            node.QueueFree();
        }
        _nodes.Clear();
        _nodeToViewModel.Clear();
    }

    public List<TNode> GetNodesForViewModel(TViewModel viewModel)
    {
        var ret = new List<TNode>();
        foreach (var node in _nodes)
            if (_nodeToViewModel.TryGetValue(node, out var vm) && ReferenceEquals(vm, viewModel))
                ret.Add(node);
        return ret;
    }

    public TViewModel? GetViewModelOfNode(TNode node) =>
        _nodeToViewModel.GetValueOrDefault(node);

    protected virtual void ApplyViewModel(TNode node, TViewModel viewModel)
    {
        if (node is IViewFor<TViewModel> view)
        {
            view.ViewModel = viewModel;
        }
        else
        {
            throw new InvalidOperationException(
                $"Node of type '{node.GetType().Name}' does not implement IViewFor<{typeof(TViewModel).Name}>. " +
                $"Override ApplyViewModel to provide custom logic.");
        }
    }
}
