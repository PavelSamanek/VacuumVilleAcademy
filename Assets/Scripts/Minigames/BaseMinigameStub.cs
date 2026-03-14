// Stub used by ProjectSetup when a minigame type cannot be resolved at edit-time.
// Replace with the actual minigame component in the scene.
namespace VacuumVille.Minigames
{
    public class BaseMinigameStub : BaseMinigame
    {
        protected override void OnMinigameBegin() { }
    }
}
