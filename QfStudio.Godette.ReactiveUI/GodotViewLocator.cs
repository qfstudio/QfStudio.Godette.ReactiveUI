using System.Reflection;
using Godot;
using ReactiveUI;

namespace QfStudio.Godette.ReactiveUI;

public class GodotViewLocator : IViewLocator
{
    private readonly Dictionary<Type, Registration> _registrations = [];

    public IViewFor<TViewModel>? ResolveView<TViewModel>(string? contract = null) where TViewModel : class
    {
        if (_registrations.TryGetValue(typeof(TViewModel), out var registration))
        {
            var scene = GD.Load<PackedScene>(registration.SceneFilePath);
            return scene.Instantiate<IViewFor<TViewModel>>();
        }

        return null;
    }

    public IViewFor? ResolveView(object? instance, string? contract = null)
    {
        if (instance == null) return null;

        if (_registrations.TryGetValue(instance.GetType(), out var registration))
        {
            var scene = GD.Load<PackedScene>(registration.SceneFilePath);
            return scene.Instantiate<IViewFor>();
        }

        return null;
    }

    public void RegisterView<TView, TViewModel>(string sceneFilePath) where TView : IViewFor
    {
         _registrations[typeof(TViewModel)] = new Registration(typeof(TViewModel), sceneFilePath, typeof(TView));
    }

    public void RegisterView(Type viewModelType, string sceneFilePath, Type? viewType = null)
    {
        _registrations[viewModelType] = new Registration(viewModelType, sceneFilePath, viewType);
    }

    public void RegisterView<TViewModel>(string sceneFilePath)
    {
        _registrations[typeof(TViewModel)] = new Registration(typeof(TViewModel), sceneFilePath);
    }

    public void RegisterViewsFromAssemblyViaReflection(Assembly assembly, bool verbose = false)
    {
        // ----------------------------------------------------------------
        // GodotSharp.SourceGenerators emits ISceneTree.g.cs containing:
        //   public partial interface ISceneTree
        //   {
        //       static abstract string TscnFilePath { get; }
        //   }
        // ----------------------------------------------------------------

        var iSceneTree = Type.GetType($"Godot.ISceneTree, {assembly.GetName().Name}");
        if (iSceneTree is null)
            return;

        var types = assembly.GetTypes();
        var concreteTypes = types.Where(t => t is { IsInterface: false, IsAbstract: false });

        var viewsAndViewModel = concreteTypes.SelectMany(t => t.GetInterfaces(), (t, iface) => (ViewType: t, Interface: iface))
            .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == typeof(IViewFor<>))
            .Select(x => (TView: x.ViewType, TViewModel: x.Interface.GetGenericArguments()[0]));

        foreach (var (view, viewModel) in viewsAndViewModel)
        {
            try
            {
                if (!iSceneTree.IsAssignableFrom(view))
                    continue;

                var sceneFilePathPropertyInfo = view.GetProperty("TscnFilePath", BindingFlags.Public | BindingFlags.Static);
                var sceneFilePath = (string?)sceneFilePathPropertyInfo?.GetValue(null);

                if (string.IsNullOrEmpty(sceneFilePath))
                    continue;

                RegisterView(viewModel, sceneFilePath, view);
                if (verbose)
                {
                    GD.Print($"Registered: {_registrations[viewModel]}");
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private record Registration(Type ViewModel, string SceneFilePath, Type? View = null);
}
