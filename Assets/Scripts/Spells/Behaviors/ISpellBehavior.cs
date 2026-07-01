using UnityEngine.XR.Interaction.Toolkit;

public interface ISpellBehaviour
{
    void OnPickUp(SpellObject spell);
    void OnRelease(SpellObject spell, SelectExitEventArgs args);
}
