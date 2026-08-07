using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Godot;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace QfStudio.Godette.IntegrationTests.Views.CustomNodeTest;

/// <summary>
/// A reusable custom node with no ViewModel: it opts into the reactive contract via the
/// <c>[IReactiveObject]</c> attribute and <see cref="IActivatableView"/>. <c>[Reactive][Export]</c>
/// gives it an observable, inspector-editable property on a single storage. Its engine-declared
/// <c>Position</c> is observed by frame polling and jitters each frame, clamped to the parent's
/// bounds. Contrast with <see cref="CustomNodeB"/>, which hand-implements IReactiveObject.
/// </summary>
[IReactiveObject]
[SceneTree(root: "_root", tscnRelativeToClassPath: "CustomNode.tscn")]
public partial class CustomNodeA : Control, IActivatableView
{
    private readonly RandomNumberGenerator _rng = new();

    [Reactive]
    [Export]
    public partial int ClickCount { get; set; }

    public CustomNodeA()
    {
        this.WhenActivated(d =>
        {
            GD.Print("[CustomNodeA] Activated");
            Disposable.Create(() => GD.Print("[CustomNodeA] Deactivated")).DisposeWith(d);

            // User-declared [Reactive] property: observed by push (IROObservableForProperty).
            this.WhenAnyValue(x => x.ClickCount)
                .Subscribe(count => CountLabel.Text = $"ClickCount: {count}")
                .DisposeWith(d);

            // Engine-declared property: observed by frame polling (GodotPollBasedPropertyBinder).
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
