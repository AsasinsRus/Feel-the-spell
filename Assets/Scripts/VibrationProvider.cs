using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class VibrationProvider
{
    private const float HAPTIC_SEND_RESOLUTION = .1f;

    public AnimationCurve vibrationPattern { get; private set; }
    public float duration { get; private set; }
    public bool loop { get; private set; }

    private HapticImpulsePlayer[] _hapticPlayers;

    private float elastedTime;
    private bool hasEnded;

    public VibrationProvider(AnimationCurve vibrationPattern, float duration, bool loop, HapticImpulsePlayer[] hapticImpulsePlayers)
    {
        this.vibrationPattern = vibrationPattern;
        this.duration = duration;
        this.loop = loop;

        _hapticPlayers = hapticImpulsePlayers;
    }

    private float GetNormilizedValue(float t)
        => vibrationPattern.Evaluate(Mathf.Clamp01(t / duration));

    public void Update()
    {
        if (duration <= 0) hasEnded = true;

        if (hasEnded) return;

        elastedTime += Time.deltaTime;

        var tNomilized = GetNormilizedValue(elastedTime);
        Vibrate(tNomilized);

        CheckLoop();
    }

    public void Stop()
    {
        hasEnded = true;
    }

    public void Play()
    {
        hasEnded = false;
        elastedTime = 0;
    }

    private void Vibrate(float tNomilized)
    {
        foreach (var player in _hapticPlayers)
        {
            player.SendHapticImpulse(tNomilized, HAPTIC_SEND_RESOLUTION);
        }
    }

    private void CheckLoop()
    {
        if (loop && elastedTime > duration)
            elastedTime = 0;
        else if(!loop)
            hasEnded = true;
    }
}
