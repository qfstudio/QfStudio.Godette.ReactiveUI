using Godot;
using QfStudio.Godette.IntegrationTests.ViewModels.CustomControlTest;
using QfStudio.Godette.ReactiveUI;

namespace QfStudio.Godette.IntegrationTests.Views.CustomControlTest;

[SceneTree(root: "_root")]
[GodotViewFor<CustomControlTestViewModel>]
public partial class CustomControlTestScene : Control
{
    private static readonly PackedScene _packedScene = GD.Load<PackedScene>("res://Views/CustomControlTest/CustomControl.tscn");
    private static readonly Script _scriptStyleA = GD.Load<Script>("res://Views/CustomControlTest/CustomControlA.cs");
    private static readonly Script _scriptStyleB = GD.Load<Script>("res://Views/CustomControlTest/CustomControlB.cs");

    public override void _Ready()
    {
        ViewModel = new CustomControlTestViewModel();

        BackButton.Pressed += () => GetTree().ChangeSceneToFile(HomeScene.TscnFilePath);
        AddButton.Pressed += AddCustomControl;
        RemoveButton.Pressed += RemoveLastCustomControl;
    }

    private void AddCustomControl()
    {
        var control = _packedScene.Instantiate<Control>();
        var id = control.GetInstanceId();
        control.SetScript(Random.Shared.NextSingle() < 0.5f ? _scriptStyleA : _scriptStyleB);
        control = (Control)GodotObject.InstanceFromId(id)!;
        Container.AddChild(control);

        // Randomize position within the container bounds.
        var max = new Vector2(
            Mathf.Max(0.0f, Container.Size.X - control.Size.X),
            Mathf.Max(0.0f, Container.Size.Y - control.Size.Y));
        control.Position = new Vector2(
            (float)GD.RandRange(0.0, max.X),
            (float)GD.RandRange(0.0, max.Y));

        GD.Print($"[CustomControlTest] Added {control.GetType().Name} at {control.Position}");
    }

    private void RemoveLastCustomControl()
    {
        var control = Container.GetChildCount() > 0 ? Container.GetChildren()[^1] : null;
        if (control is null)
        {
            return;
        }

        control.QueueFree();
        GD.Print("[CustomControlTest] Removed last control");
    }
}
