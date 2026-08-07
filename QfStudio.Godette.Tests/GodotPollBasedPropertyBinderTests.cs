using System.ComponentModel;
using Godot;
using ReactiveUI;

namespace QfStudio.Godette.Tests;

public class GodotPollBasedPropertyBinderTests
{
    private readonly GodotPollBasedPropertyBinder _binder = new();

    [Fact]
    public void Affinity_EngineDeclaredPropertyOnReactiveView_ReturnsPollingAffinity()
    {
        // Position is declared by the Godot engine (Godot.Control) and the view implements IReactiveObject:
        // the affinity must outrank IROObservableForProperty (10) so frame polling escapes dead observation.
        var affinity = _binder.GetAffinityForObject(typeof(ReactiveControl), nameof(ReactiveControl.Position));
        Assert.True(affinity > 10);
    }

    [Fact]
    public void Affinity_UserDeclaredPropertyOnReactiveView_ReturnsZero()
    {
        // User-declared properties return 0 so IROObservableForProperty (10) observes them by push
        // (they raise change notifications when decorated with [Reactive]).
        Assert.Equal(0, _binder.GetAffinityForObject(typeof(ReactiveControl), nameof(ReactiveControl.UserProperty)));
    }

    [Fact]
    public void Affinity_EngineDeclaredPropertyOnPlainControl_ReturnsPollingAffinity()
    {
        // Plain Godot objects have no push notifications: the affinity must outrank
        // POCOObservableForProperty (1) so frame polling claims the property.
        var affinity = _binder.GetAffinityForObject(typeof(PlainControl), nameof(PlainControl.Position));
        Assert.True(affinity > 1);
    }

    [Fact]
    public void Affinity_UserDeclaredPropertyOnInpcControl_ReturnsZero()
    {
        // INotifyPropertyChanged-only Godot objects also own push observation of their user-declared
        // properties (INPCObservableForProperty, 5), so the poller must not claim them.
        Assert.Equal(0, _binder.GetAffinityForObject(typeof(InpcControl), nameof(InpcControl.UserProperty)));
    }

    [Fact]
    public void Affinity_EngineDeclaredPropertyOnInpcControl_ReturnsPollingAffinity()
    {
        // The engine never raises INotifyPropertyChanged notifications for engine-declared properties:
        // the affinity must outrank INPCObservableForProperty (5) so frame polling claims them.
        var affinity = _binder.GetAffinityForObject(typeof(InpcControl), nameof(InpcControl.Position));
        Assert.True(affinity > 5);
    }

    [Fact]
    public void Affinity_BeforeChanged_ReturnsZero()
    {
        Assert.Equal(0, _binder.GetAffinityForObject(typeof(ReactiveControl), nameof(ReactiveControl.Position), beforeChanged: true));
    }

    [Fact]
    public void Affinity_NonGodotReactiveObject_ReturnsZero()
    {
        // Pure C# ViewModels are not GodotObjects and must stay with IROObservableForProperty (10).
        Assert.Equal(0, _binder.GetAffinityForObject(typeof(ReactiveViewModel), nameof(ReactiveViewModel.Name)));
    }

    private sealed class ReactiveControl : Control, IReactiveObject
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangingEventHandler? PropertyChanging;

        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);

        public string UserProperty { get; set; } = "";
    }

    private sealed class PlainControl : Control
    {
    }

#pragma warning disable CS0067 // Event never used: metadata-only test type implementing INotifyPropertyChanged.
    private sealed class InpcControl : Control, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string UserProperty { get; set; } = "";
    }
#pragma warning restore CS0067

    private sealed class ReactiveViewModel : IReactiveObject
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangingEventHandler? PropertyChanging;

        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);

        public string Name { get; set; } = "";
    }
}
