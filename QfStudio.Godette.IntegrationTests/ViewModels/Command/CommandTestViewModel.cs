using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Godot;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace QfStudio.Godette.IntegrationTests.ViewModels.Command;

public partial class CommandTestViewModel : ViewModelBase
{
    public CommandTestViewModel()
    {
        SimpleCommand = ReactiveCommand.Create(() =>
        {
            GD.Print($"[Command] Simple command");
        });

        AsyncCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            GD.Print("[Command] Async command: started");
            await Task.Delay(2000);
            GD.Print("[Command] Async command: completed");
        });

        SearchCommand = ReactiveCommand.CreateFromTask<string>(async query =>
        {
            GD.Print($"[Command] Search: query='{query}'");
            await Task.Delay(300);
        });

        var commandEnabled = this.WhenAnyValue(x => x.ConditionalCommandEnabled);
        ConditionalCommand = ReactiveCommand.Create(() =>
        {
            GD.Print($"[Command] Conditional command");
        }, commandEnabled);

        InputDigits = this.WhenAnyValue(x => x.InputText)
            .Select(text => int.TryParse(text, out var num) ? (int?)num : null);

        var lineEditSubmitEnabled = this.WhenAnyValue(x => x.LineEditSubmitEnabled);
        LineEditSubmitCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            GD.Print("[Command] LineEdit submit command triggered.");
            await Task.Delay(Random.Shared.Next(5000));
            GD.Print("[Command] LineEdit submit command done.");
        }, lineEditSubmitEnabled);

        AcceptEvenNumbersOnlyCommand = ReactiveCommand.CreateFromTask<int>(async (num, ct) =>
        {
            await Task.Delay(200, ct);
            if (ct.IsCancellationRequested)
                return;

            GD.Print(num % 2 == 0
                ? $"[Command] AcceptEvenNumbersOnlyCommand Accepted! num={num}"
                : $"[Command] AcceptEvenNumbersOnlyCommand Rejected! num={num}");
            await Task.Delay(300, ct);
        }, InputDigits.Select(num => num is not null && num % 2 == 0));
    }

    [Reactive]
    public partial string QueryString { get; set; } = "World";

    [Reactive]
    public partial bool ConditionalCommandEnabled { get; set; } = false;

    [Reactive]
    public partial string InputText { get; set; } = string.Empty;

    [Reactive]
    public partial bool LineEditSubmitEnabled { get; set; } = true;

    public IObservable<int?> InputDigits { get; }

    public ReactiveCommand<Unit, Unit> SimpleCommand { get; }
    public ReactiveCommand<Unit, Unit> LineEditSubmitCommand { get; }
    public ReactiveCommand<Unit, Unit> AsyncCommand { get; }
    public ReactiveCommand<string, Unit> SearchCommand { get; }

    public ReactiveCommand<Unit, Unit> ConditionalCommand { get; }

    public ReactiveCommand<int, Unit> AcceptEvenNumbersOnlyCommand { get; }
}
