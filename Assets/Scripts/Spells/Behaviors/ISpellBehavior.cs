using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// Interface for behaviour of spells.
/// </summary>
public interface ISpellBehaviour
{
    /// <summary>
    /// Saving the spell as an attribute. 
    /// </summary>
    /// <param name="spell">The spell</param>
    void OnPickUp(SpellObject spell);

 /// <summary>
 /// Describes the behaviour of spell after it is released (for example throwing behavious).
 /// </summary>
 /// <param name="spell">The spell</param>
 /// <param name="args"></param>
    void OnRelease(SpellObject spell, SelectExitEventArgs args);
}
