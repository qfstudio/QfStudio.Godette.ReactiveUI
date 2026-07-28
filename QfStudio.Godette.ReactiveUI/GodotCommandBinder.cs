using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows.Input;
using Godot;
using ReactiveUI;

namespace QfStudio.Godette.ReactiveUI;

/// <remarks>
/// PopupMenu is not supported by BindCommand. BindCommand is 1:1 (one control to one command)
/// and expects a parameter-less signal; PopupMenu emits IdPressed(id) whose value is UI state,
/// not a ViewModel concept, so it cannot serve as a command parameter without leaking UI states into the ViewModel.
/// For per-item commands, use <see cref="PopupMenuBinder{TViewModel}"/> with a per-item CommandSelector instead.
/// Similar situations: OptionButton, TabBar, ItemList, Tree.
/// </remarks>
public class GodotCommandBinder : ICreatesCommandBinding
{
    int ICreatesCommandBinding.GetAffinityForObject<T>(bool hasEventTarget)
    {
        var type = typeof(T);

        return type switch
        {
            not null when typeof(BaseButton).IsAssignableFrom(type) => 10,
            not null when typeof(LineEdit).IsAssignableFrom(type) => 10,
            _ => 0
        };
    }

    public IDisposable? BindCommandToObject<T>(ICommand? command, T? target, IObservable<object?> commandParameter) where T : class
    {
        if (command is null)
            return null;

        return target switch
        {
            BaseButton button => GodotCommandBinderImpl.BindButton(command, button, commandParameter),
            LineEdit lineEdit => GodotCommandBinderImpl.BindLineEdit(command, lineEdit, commandParameter),
            _ => null
        };
    }

    public IDisposable? BindCommandToObject<T, TEventArgs>(ICommand? command, T? target, IObservable<object?> commandParameter,
        string eventName) where T : class
    {
        throw new NotSupportedException($"GodotCommandBinder does not support binding by event name.");
    }

    public IDisposable? BindCommandToObject<T, TEventArgs>(ICommand? command, T? target, IObservable<object?> commandParameter,
        Action<EventHandler<TEventArgs>> addHandler, Action<EventHandler<TEventArgs>> removeHandler) where T : class where TEventArgs : EventArgs
    {
        throw new NotSupportedException("GodotCommandBinder does not support custom event handlers, as most Godot controls do not follow the standard .NET event pattern.");
    }
}

internal static class GodotCommandBinderImpl
{
    private static IDisposable BindViewCore(ICommand command, Action<Action> addHandler, Action<Action> removeHandler, IObservable<object?> commandParameter, Action<bool>? setViewEnabled)
    {
        return BindViewCore(command, Observable.FromEvent(addHandler, removeHandler), commandParameter, setViewEnabled);
    }

    private static IDisposable BindViewCore<TDelegate, TEventArgs>(ICommand command, Action<TDelegate> addHandler, Action<TDelegate> removeHandler, IObservable<object?> commandParameter, Action<bool>? setViewEnabled)
    {
        return BindViewCore(command,
            Observable.FromEvent<TDelegate, TEventArgs>(addHandler, removeHandler).Select(_ => Unit.Default),
            commandParameter,
            setViewEnabled);
    }

    private static IDisposable BindViewCore(ICommand command, IObservable<Unit> commandTrigger, IObservable<object?> commandParameter, Action<bool>? setViewEnabled)
    {
        var disposable = new CompositeDisposable();

        var sharedParameter = commandParameter.StartWith((object?)null).Replay(1).RefCount();

        commandTrigger
            .WithLatestFrom(sharedParameter, (_, param) => param)
            .Subscribe(param =>
            {
                if (command.CanExecute(param))
                    command.Execute(param);
            })
            .DisposeWith(disposable);

        if (setViewEnabled != null)
        {
            var canExecuteChanged = Observable.FromEventPattern(
                    h => command.CanExecuteChanged += h,
                    h => command.CanExecuteChanged -= h)
                .Select(_ => Unit.Default)
                .StartWith(Unit.Default);

            canExecuteChanged
                .WithLatestFrom(sharedParameter, (_, param) => param)
                .Select(command.CanExecute)
                .DistinctUntilChanged()
                .Subscribe(setViewEnabled)
                .DisposeWith(disposable);
        }

        return disposable;
    }

    public static IDisposable BindButton(ICommand command, BaseButton button, IObservable<object?> param)
    {
        return BindViewCore(command,
            h => button.Pressed += h,
            h => button.Pressed -= h,
            param,
            enabled => button.Disabled = !enabled);
    }

    public static IDisposable BindLineEdit(ICommand command, LineEdit lineEdit, IObservable<object?> param)
    {
        var shouldFire = true;
        var commandTrigger = lineEdit.ObserveTextSubmitted().Select(_ => Unit.Default).Where(_ => shouldFire);
        return BindViewCore(command,
            commandTrigger,
            param,
            enabled => shouldFire = enabled);
    }
}
