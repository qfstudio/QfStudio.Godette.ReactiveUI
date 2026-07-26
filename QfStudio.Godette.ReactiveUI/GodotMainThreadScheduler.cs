using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace QfStudio.Godette.ReactiveUI;

public class GodotMainThreadScheduler : IScheduler
{
    private readonly SynchronizationContext _context;

    private GodotMainThreadScheduler(SynchronizationContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public static GodotMainThreadScheduler Create(SynchronizationContext context) => new(context);

    public DateTimeOffset Now => DateTimeOffset.Now;

    public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        if (ReferenceEquals(SynchronizationContext.Current, _context))
            return action(this, state);

        var disposable = new SingleAssignmentDisposable();

        _context.Post(_ =>
        {
            if (!disposable.IsDisposed)
            {
                disposable.Disposable = action(this, state);
            }
        }, null);

        return disposable;
    }

    public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        var disposable = new SingleAssignmentDisposable();

        if (dueTime <= TimeSpan.Zero)
        {
            return Schedule(state, action);
        }

        var timer = new Timer(_ =>
        {
            _context.Post(__ =>
            {
                if (!disposable.IsDisposed)
                {
                    disposable.Disposable = action(this, state);
                }
            }, null);
        }, null, dueTime, Timeout.InfiniteTimeSpan);

        return StableCompositeDisposable.Create(disposable, timer);
    }

    public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        return Schedule(state, dueTime - Now, action);
    }
}
