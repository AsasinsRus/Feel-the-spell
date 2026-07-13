/*using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

DEPRECATED

public sealed class GraspScoreState
{
    private const int PALM_SCORE = 2;
    private const int THUMB_OPPOSED_SCORE = 2;

    private readonly Dictionary<XRGrabInteractable, InteractableGraspState> states = new();

    public XRGrabInteractable CurrentCandidate { get; private set; }

    public bool TryGetScore(XRGrabInteractable interactable, out int score)
    {
        if(states.TryGetValue(interactable, out var state))
        {
            score = state.Score;
            return true;
        }

        score = 0;
        return false;
    }

    public void AddScore(XRGrabInteractable interactable, int amount)
    {
        var state = GetOrCreate(interactable);
        state.Score += amount;
        ReevaluateCandidate();
    }

    public void RemoveScore(XRGrabInteractable interactable, int amount)
    {
        if (!states.TryGetValue(interactable, out var state))
            return;

        state.Score -= amount;

        CleanUpIfEmpty(interactable, state);
        ReevaluateCandidate();
    }

    public bool SetPalmState(XRGrabInteractable interactable, bool isNearPalm)
    {
        var state = GetOrCreate(interactable);

        if (state.IsNearPalm == isNearPalm)
            return false;

        state.IsNearPalm = isNearPalm;

        if (isNearPalm)
            AddScore(interactable, PALM_SCORE);
        else RemoveScore(interactable, PALM_SCORE);
        
        return true;
    }

    public bool SetThumbOppositionState(XRGrabInteractable interactable, bool opposed)
    {
        var state = GetOrCreate(interactable);

        if (state.HasThumbOpposedToAnyFinger == opposed)
            return false;

        state.HasThumbOpposedToAnyFinger = opposed;

        if (opposed)
            AddScore(interactable, THUMB_OPPOSED_SCORE);
        else RemoveScore(interactable, THUMB_OPPOSED_SCORE);

        return true;
    }

    public void RemoveInteractable(XRGrabInteractable interactable)
    {
        states.Remove(interactable);

        if (CurrentCandidate == interactable)
            CurrentCandidate = null;

        ReevaluateCandidate();
    }

    private InteractableGraspState GetOrCreate(XRGrabInteractable interactable)
    {
        if(!states.TryGetValue(interactable, out var state))
        {
            state = new InteractableGraspState(interactable);
            states.Add(interactable, state);
        }

        return state;
    }

    private void CleanUpIfEmpty(XRGrabInteractable interactable, InteractableGraspState state)
    {
        if (state.Score <= 0 && !state.IsNearPalm && !state.HasThumbOpposedToAnyFinger)
            states.Remove(interactable);
    }

    private void ReevaluateCandidate()
    {
        CurrentCandidate = null;

        var bestScore = int.MinValue;

        foreach(var pair in states)
        {
            if(pair.Value.Score > bestScore)
            {
                bestScore = pair.Value.Score;
                CurrentCandidate = pair.Key;
            }
        }
    }
}

public sealed class InteractableGraspState
{
    public XRGrabInteractable Interactable;
    
    public int Score;
    
    public bool IsNearPalm;
    public bool HasThumbOpposedToAnyFinger;

    public InteractableGraspState(XRGrabInteractable interactable)
    {
        this.Interactable = interactable;
    }
}*/