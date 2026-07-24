using System.Threading;
using QfStudio.Godette.ReactiveUI;
using ReactiveUI;
using ReactiveUI.Builder;
using Splat;

namespace QfStudio.Godette.IntegrationTests.Autoloads;

public partial class RxAppBootstrapper : Godot.Node
{
    private readonly GodotFrameScheduler _processFrameScheduler = new();
    private readonly GodotFrameScheduler _physicsFrameScheduler = new();

    public RxAppBootstrapper()
    {
        var scheduler = GodotMainThreadScheduler.Create(SynchronizationContext.Current!);
        GodotSchedulers.MainThreadScheduler = scheduler;
        GodotSchedulers.ProcessFrameScheduler = _processFrameScheduler;
        GodotSchedulers.PhysicsFrameScheduler = _physicsFrameScheduler;

        var viewLocator = new GodotViewLocator();
        viewLocator.RegisterViewsFromAssemblyViaReflection(typeof(RxAppBootstrapper).Assembly, verbose: true);

        RxAppBuilder.CreateReactiveUIBuilder()
            .WithMainThreadScheduler(scheduler)
            .WithConverters()
            .WithRegistration(locator =>
            {
                locator.RegisterConstant(new GodotActivationFetcher(), typeof(IActivationForViewFetcher));
                locator.RegisterConstant(new GodotPropertyBinder(), typeof(ICreatesObservableForProperty));
                locator.RegisterConstant(new GodotPollBasedPropertyBinder(), typeof(ICreatesObservableForProperty));
                locator.RegisterConstant(new GodotCommandBinder(), typeof(ICreatesCommandBinding));
                locator.RegisterConstant(viewLocator, typeof(IViewLocator));
            })
            .WithCoreServices()
            .BuildApp();
    }

    public override void _Process(double delta)
    {
        _processFrameScheduler.NotifyProcess(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _physicsFrameScheduler.NotifyProcess(delta);
    }
}

internal static class RxAppBuilderExtensions
{
    extension(IReactiveUIBuilder builder)
    {
        public IReactiveUIBuilder WithConverters()
        {
            return builder
                .WithConverter(new FloatToDoubleConverter())
                .WithConverter(new DoubleToFloatConverter());
        }
    }
}
