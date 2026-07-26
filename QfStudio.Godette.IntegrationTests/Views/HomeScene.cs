using Godot;

namespace QfStudio.Godette.IntegrationTests.Views;

[SceneTree(root: "_root")]
public partial class HomeScene : Control
{
    public override void _Ready()
    {
        ActivationButton.Pressed += () => GetTree().ChangeSceneToFile(Activation.ActivatableHostScene.TscnFilePath);
        DataBindingButton.Pressed += () => GetTree().ChangeSceneToFile(DataBinding.DataBindingTestScene.TscnFilePath);
        CommandButton.Pressed += () => GetTree().ChangeSceneToFile(Command.CommandTestScene.TscnFilePath);
        InteractionButton.Pressed += () => GetTree().ChangeSceneToFile(Interaction.InteractionTestScene.TscnFilePath);
        ObservableBridgeButton.Pressed += () => GetTree().ChangeSceneToFile(ObservableBridge.ObservableBridgeTestScene.TscnFilePath);
        RoutingButton.Pressed += () => GetTree().ChangeSceneToFile(Routing.RoutingDemoScene.TscnFilePath);
        FrameOperatorsButton.Pressed += () => GetTree().ChangeSceneToFile(FrameOperators.FrameOperatorsTestScene.TscnFilePath);
        PollingBindingButton.Pressed += () => GetTree().ChangeSceneToFile(PollingBinding.PollingBindingTestScene.TscnFilePath);
        ItemsBinderButton.Pressed += () => GetTree().ChangeSceneToFile(Collection.ItemsBinderTestScene.TscnFilePath);
        ItemListButton.Pressed += () => GetTree().ChangeSceneToFile("res://Views/Collection/ItemListTestScene.tscn");
        IndexedItemBinderButton.Pressed += () => GetTree().ChangeSceneToFile("res://Views/Collection/IndexedItemBinderTestScene.tscn");
        ValidationButton.Pressed += () => GetTree().ChangeSceneToFile(Validation.ValidationTestScene.TscnFilePath);
        MiscButton.Pressed += () => GetTree().ChangeSceneToFile(Misc.MiscTestScene.TscnFilePath);
        ExitButton.Pressed += Exit;
    }

    private void Exit()
    {
        GC.Collect();
        GetTree().Quit();
    }
}
