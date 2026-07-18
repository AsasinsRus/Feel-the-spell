using System.Collections.Generic;
using UnityEngine;

public class BowVisuals : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem[] _bowParticles;
    [SerializeField]
    private Vector3 _maxPulledRotationSpeed = new Vector3(40, 25, 0);
    [SerializeField]
    private LineRenderer[] _lines;
    private Dictionary<LineRenderer, float> standartWidth = new();

    [SerializeField]
    private Transform _startPoint;
    [SerializeField]
    private Transform _endPoint;

    [SerializeField]
    private float pullThreashold = .4f;

    private XRPullInteractable _interactable;

    private void Start()
    {
        _interactable = GetComponentInChildren<XRPullInteractable>();

        _interactable.PullUpdated += OnStringUpdate;
        _interactable.PullActionReleased += OnStringReleased;
        _interactable.PullStarted += OnPullStart;

        SetTrailsActive(false);
        SetBowParticlesActive(false);

        foreach (var line in _lines)
        {
            standartWidth.Add(line, line.GetPosition(1).x);
            line.SetPosition(1, Vector3.zero);
        }
    }

    private void OnPullStart()
    {
        SetBowParticlesActive(true);
    }

    private void OnStringReleased(float pullAmount)
    {
        if(pullAmount < pullThreashold)
            SetBowParticlesActive(false);
        else
            SetTrailsActive(true);
    }

    private void SetBowParticlesActive(bool active)
    {
        foreach (var particle in _bowParticles)
            if(active)
                particle.Play();
            else particle.Stop();
    }

    private void SetTrailsActive(bool active)
    {
        foreach (var particle in _bowParticles)
        {
            var tr = particle.trails;
            tr.ratio = 0; //active ? 1 : 0;
        }
    }

    private void OnStringUpdate(float pullAmount)
    {
        UpdateLines(pullAmount);
        UpdateBowParticles(pullAmount);
    }

    private void UpdateLines(float pullAmount)
    {
        foreach (var line in _lines)
        {
            Vector3 linePosition = new Vector3(pullAmount * standartWidth[line], line.transform.localPosition.y, line.transform.localPosition.z);
            line.SetPosition(1, linePosition);
        }
    }

    private void UpdateBowParticles(float pullAmount)
    {
        foreach(var particle in _bowParticles)
        {
            var vc = particle.velocityOverLifetime;

            vc.orbitalX = _maxPulledRotationSpeed.x * pullAmount;
            vc.orbitalY = _maxPulledRotationSpeed.y * pullAmount;
            vc.orbitalZ = _maxPulledRotationSpeed.z * pullAmount;
        }
    }

    private void OnDestroy()
    {
        _interactable.PullUpdated -= OnStringUpdate;
        _interactable.PullActionReleased -= OnStringReleased;
        _interactable.PullStarted -= OnPullStart;
    }
}
