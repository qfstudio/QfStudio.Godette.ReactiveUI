using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using ReactiveUI;
using Expression = System.Linq.Expressions.Expression;

namespace QfStudio.Godette.ReactiveUI;

/// <summary>
/// Creates observable change notifications for Godot object properties by polling them every frame.
/// </summary>
/// <remarks>
/// <para>
/// Polling is only the fallback for what push observation cannot serve: the engine never raises
/// change notifications for engine-declared properties (e.g. <c>Position</c>, <c>Size</c>,
/// <c>Visible</c>), and objects without a notification interface
/// (<see cref="IReactiveObject"/> or <see cref="System.ComponentModel.INotifyPropertyChanged"/>)
/// have no push at all; everything push-capable stays with its push binder. The affinity
/// (<see cref="PollingAffinity"/>) is therefore placed between signal-based observation (15) and
/// <see cref="IROObservableForProperty"/> (10): high enough for engine-declared properties on
/// notification objects to escape dead observation, low enough for signal-backed properties to
/// keep priority.
/// </para>
/// <para>
/// See <see cref="GodotPropertyBinder"/> for the full property observation affinity chain.
/// </para>
/// </remarks>
public sealed class GodotPollBasedPropertyBinder : ICreatesObservableForProperty
{
    public const int PollingAffinity = 12;

    private static readonly ConcurrentDictionary<(Type Type, string Property), Func<object, object?>?> GetterCache = new();

    public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged = false)
    {
        if (beforeChanged) 
            return 0;
        
        if (!typeof(Godot.GodotObject).IsAssignableFrom(type)) 
            return 0;

        // Polling is only the fallback for what push observation cannot serve: the engine never
        // raises change notifications for engine-declared properties, and objects without a
        // notification interface (IReactiveObject/INotifyPropertyChanged) have no push at all.
        // Everything push-capable stays with its push binder.
        if (typeof(IReactiveObject).IsAssignableFrom(type) || typeof(INotifyPropertyChanged).IsAssignableFrom(type))
        {
            if (IsEngineDeclaredProperty(type, propertyName))
            {
                return PollingAffinity;
            }

            return 0;
        }

        return PollingAffinity;
    }

    public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(
        object sender, Expression expression, string propertyName,
        bool beforeChanged = false, bool suppressWarnings = false)
    {
        var getter = GetterCache.GetOrAdd((sender.GetType(), propertyName), static key =>
        {
            var property = key.Type.GetProperty(
                key.Property,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is null || !property.CanRead)
                return null;

            // (object o) => (object)((Type)o).Property
            var param = Expression.Parameter(typeof(object), "o");
            var cast = Expression.Convert(param, key.Type);
            var access = Expression.Property(cast, property);
            var boxed = Expression.Convert(access, typeof(object));
            return Expression.Lambda<Func<object, object?>>(boxed, param).Compile();
        });

        if (getter is null)
            return Observable.Never<IObservedChange<object?, object?>>();

        return Observable.PollEveryUpdate<object, object?>(
                sender,
                getter)
            .Select(value => new ObservedChange<object?, object?>(sender, expression, value));
    }

    private static bool IsEngineDeclaredProperty(Type type, string propertyName)
    {
        var declaringType = type.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)?.DeclaringType;
        return declaringType is not null && declaringType.Assembly == typeof(Godot.GodotObject).Assembly;
    }
}
