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
        CustomControlTestButton.Pressed += () => GetTree().ChangeSceneToFile(CustomControlTest.CustomControlTestScene.TscnFilePath);
        InteractionButton.Pressed += () => GetTree().ChangeSceneToFile(Interaction.InteractionTestScene.TscnFilePath);
        ObservableBridgeButton.Pressed += () => GetTree().ChangeSceneToFile(ObservableBridge.ObservableBridgeTestScene.TscnFilePath);
        RoutingButton.Pressed += () => GetTree().ChangeSceneToFile(Routing.RoutingDemoScene.TscnFilePath);
        FrameOperatorsButton.Pressed += () => GetTree().ChangeSceneToFile(FrameOperators.FrameOperatorsTestScene.TscnFilePath);
        PollingBindingButton.Pressed += () => GetTree().ChangeSceneToFile(PollingBinding.PollingBindingTestScene.TscnFilePath);
        ItemsBinderButton.Pressed += () => GetTree().ChangeSceneToFile(Collection.ItemsBinderTestScene.TscnFilePath);
        ItemListButton.Pressed += () => GetTree().ChangeSceneToFile(Collection.ItemListBinderTestScene.TscnFilePath);
        IndexedControlBinderButton.Pressed += () => GetTree().ChangeSceneToFile(Collection.IndexedControlBinderTestScene.TscnFilePath);
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
