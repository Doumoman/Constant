namespace StarNight.Rewrite.Player
{
    public interface IPlayerInteractable
    {
        string Prompt { get; }

        bool CanInteract(PlayerInteractor interactor);

        void Interact(PlayerInteractor interactor);
    }
}
