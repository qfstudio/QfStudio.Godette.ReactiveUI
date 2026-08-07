using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Godot;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace QfStudio.Godette.IntegrationTests.Views.CustomControlTest;

/// <summary>
/// A reusable custom control that opts into the reactive contract by hand-implementing
/// <see cref="IReactiveObject"/> (the four members) and <see cref="IActivatableView"/>, contrasted
/// with <see cref="CustomControlA"/> which uses the <c>[IReactiveObject]</c> attribute. Both paths
/// satisfy <c>[Reactive][Export]</c> and engine-property polling identically.
/// </summary>
[SceneTree(root: "_root", tscnRelativeToClassPath: "CustomControl.tscn")]
public partial class CustomControlB : Control, IReactiveObject, IActivatableView
{
    private readonly RandomNumberGenerator _rng = new();

    [Reactive]
    [Export]
    public partial int ClickCount { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
    void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) => PropertyChanging?.Invoke(this, args);

    public CustomControlB()
    {
        this.WhenActivated(d =>
        {
            GD.Print("[CustomControlB] Activated");
            Disposable.Create(() => GD.Print("[CustomControlB] Deactivated")).DisposeWith(d);

            this.WhenAnyValue(x => x.ClickCount)
                .Subscribe(count => CountLabel.Text = $"ClickCount: {count}")
                .DisposeWith(d);

            this.WhenAnyValue(x => x.Position)
                .Subscribe(pos => PositionLabel.Text = $"position: ({pos.X:0.00}, {pos.Y:0.00})")
                .DisposeWith(d);
        });
    }

    public override void _Ready()
    {
        _rng.Randomize();
        IncrementButton.Pressed += () => ClickCount++;
    }

    public override void _Process(double delta)
    {
        if (GetParent() is not Control parent)
            return;

        var pos = Position;
        pos.X += _rng.RandfRange(-0.2f, 0.2f);
        pos.Y += _rng.RandfRange(-0.2f, 0.2f);
        pos.X = Mathf.Clamp(pos.X, 0.0f, Mathf.Max(0.0f, parent.Size.X - Size.X));
        pos.Y = Mathf.Clamp(pos.Y, 0.0f, Mathf.Max(0.0f, parent.Size.Y - Size.Y));
        Position = pos;
    }
}
