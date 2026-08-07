using System.Linq.Expressions;
using System.Reactive.Linq;
using ReactiveUI;

namespace QfStudio.Godette.ReactiveUI;

/// <summary>
/// Creates observable change notifications for built-in Godot control properties
/// by subscribing to the corresponding Godot signals.
/// </summary>
/// <remarks>
/// <para>
/// This binder claims affinity 15 (<see cref="SignalBasedAffinity"/>). Since Godot views commonly
/// implement <see cref="IReactiveObject"/>, a higher affinity than
/// <see cref="IROObservableForProperty"/> (10) ensures signal-based observation wins whenever a
/// built-in control property has a matching signal.
/// </para>
/// <para>
/// The full property observation affinity chain (on a tie, the first registered binder wins):
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Affinity</term>
/// <description>Binder</description>
/// <description>Claims</description>
/// </listheader>
/// <item>
/// <term>15</term>
/// <description><see cref="GodotPropertyBinder"/></description>
/// <description>built-in control properties observed via their signals</description>
/// </item>
/// <item>
/// <term>12</term>
/// <description><see cref="GodotPollBasedPropertyBinder"/></description>
/// <description>engine-declared properties (e.g. <c>Position</c>, <c>Size</c>, <c>Visible</c>) on <see cref="IReactiveObject"/>/<see cref="System.ComponentModel.INotifyPropertyChanged"/> objects, and all properties on other Godot objects</description>
/// </item>
/// <item>
/// <term>10</term>
/// <description><see cref="IROObservableForProperty"/></description>
/// <description><see cref="IReactiveObject"/> user-declared properties (<c>[Reactive]</c> / <c>ViewModel</c>) - push</description>
/// </item>
/// <item>
/// <term>5</term>
/// <description><see cref="INPCObservableForProperty"/></description>
/// <description><see cref="System.ComponentModel.INotifyPropertyChanged"/> types</description>
/// </item>
/// <item>
/// <term>1</term>
/// <description><see cref="POCOObservableForProperty"/></description>
/// <description>fallback: emits the current value once</description>
/// </item>
/// </list>
/// </remarks>
public class GodotPropertyBinder : ICreatesObservableForProperty
{
    public const int SignalBasedAffinity = 15;

    public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged = false)
    {
        if (beforeChanged) return 0;

        return propertyName switch
        {
            nameof(Godot.Range.Value) when typeof(Godot.Range).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.LineEdit.Text) when typeof(Godot.LineEdit).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.TextEdit.Text) when typeof(Godot.TextEdit).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.BaseButton.ButtonPressed) when typeof(Godot.BaseButton).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.TabContainer.CurrentTab) when typeof(Godot.TabContainer).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.OptionButton.Selected) when typeof(Godot.OptionButton).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.TabBar.CurrentTab) when typeof(Godot.TabBar).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.ColorPicker.Color) when typeof(Godot.ColorPicker).IsAssignableFrom(type) => SignalBasedAffinity,
            nameof(Godot.ColorPickerButton.Color) when typeof(Godot.ColorPickerButton).IsAssignableFrom(type) => SignalBasedAffinity,
            _ => 0
        };
    }

    public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(
        object sender, Expression expression, string propertyName,
        bool beforeChanged = false, bool suppressWarnings = false)
    {
        return propertyName switch
        {
            nameof(Godot.Range.Value) when sender is Godot.Range range =>
                Observable.FromEvent<Godot.Range.ValueChangedEventHandler, double>(
                        h => range.ValueChanged += h,
                        h => range.ValueChanged -= h)
                    .Select(v => new ObservedChange<object?, object?>(sender, expression, v)),

            nameof(Godot.LineEdit.Text) when sender is Godot.LineEdit lineEdit =>
                Observable.FromEvent<Godot.LineEdit.TextChangedEventHandler, string>(
                        h => lineEdit.TextChanged += h,
                        h => lineEdit.TextChanged -= h)
                    .Select(v => new ObservedChange<object?, object?>(sender, expression, v)),

            nameof(Godot.TextEdit.Text) when sender is Godot.TextEdit textEdit =>
                Observable.FromEvent(
                        h => textEdit.TextChanged += h,
                        h => textEdit.TextChanged -= h)
                    .Select(_ => new ObservedChange<object?, object?>(sender, expression, textEdit.Text)),

            nameof(Godot.BaseButton.ButtonPressed) when sender is Godot.BaseButton button =>
                Observable.FromEvent<Godot.BaseButton.ToggledEventHandler, bool>(
                        h => button.Toggled += h,
                        h => button.Toggled -= h)
                    .Select(v => new ObservedChange<object?, object?>(sender, expression, v)),

            nameof(Godot.TabContainer.CurrentTab) when sender is Godot.TabContainer tabContainer =>
                Observable.FromEvent<Godot.TabContainer.TabChangedEventHandler, long>(
                        h => tabContainer.TabChanged += h,
                        h => tabContainer.TabChanged -= h)
                    .Select(idx => new ObservedChange<object?, object?>(sender, expression, (int)idx)),

            nameof(Godot.OptionButton.Selected) when sender is Godot.OptionButton optionButton =>
                Observable.FromEvent<Godot.OptionButton.ItemSelectedEventHandler, long>(
                        h => optionButton.ItemSelected += h,
                        h => optionButton.ItemSelected -= h)
                    .Select(idx => new ObservedChange<object?, object?>(sender, expression, (int)idx)),

            nameof(Godot.TabBar.CurrentTab) when sender is Godot.TabBar tabBar =>
                Observable.FromEvent<Godot.TabBar.TabChangedEventHandler, long>(
                        h => tabBar.TabChanged += h,
                        h => tabBar.TabChanged -= h)
                    .Select(tab => new ObservedChange<object?, object?>(sender, expression, (int)tab)),

            nameof(Godot.ColorPicker.Color) when sender is Godot.ColorPicker colorPicker =>
                Observable.FromEvent<Godot.ColorPicker.ColorChangedEventHandler, Godot.Color>(
                        h => colorPicker.ColorChanged += h,
                        h => colorPicker.ColorChanged -= h)
                    .Select(c => new ObservedChange<object?, object?>(sender, expression, c)),

            nameof(Godot.ColorPickerButton.Color) when sender is Godot.ColorPickerButton colorPickerButton =>
                Observable.FromEvent<Godot.ColorPickerButton.ColorChangedEventHandler, Godot.Color>(
                        h => colorPickerButton.ColorChanged += h,
                        h => colorPickerButton.ColorChanged -= h)
                    .Select(c => new ObservedChange<object?, object?>(sender, expression, c)),

            _ => Observable.Never<IObservedChange<object?, object?>>()
        };
    }
}
