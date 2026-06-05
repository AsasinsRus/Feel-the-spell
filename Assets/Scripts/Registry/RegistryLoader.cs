using UnityEngine;

public static class RegistryLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Resources.LoadAll<ScriptableObject>("Registries");
    }
}
