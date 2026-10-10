using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerShipSensor))]
[RequireComponent(typeof(CharacterController))]
public class CharacterAnimationManager : MonoBehaviour
{
    const string StateIdle = "Idle";
    const string StateRunning = "Running";
    const string StateSpaceMovement = "SpaceMovement";
    const string StateSpaceIdle = "SpaceIdle";
    const string StateDead = "Dead";

    const string PlaceholderIdle = "Placeholder_Idle";
    const string PlaceholderRunning = "Placeholder_Running";
    const string PlaceholderSpaceMovement = "Placeholder_SpaceMovement";
    const string PlaceholderSpaceIdle = "Placeholder_SpaceIdle";
    const string PlaceholderDead = "Placeholder_Dead";

    [SerializeField] Animator animator;
    [SerializeField] RuntimeAnimatorController baseController;
    [SerializeField] AnimationClip idle;
    [SerializeField] AnimationClip running;
    [SerializeField] AnimationClip spaceMovement;
    [SerializeField] AnimationClip spaceIdle;
    [SerializeField] AnimationClip dead;
    [SerializeField] float crossFadeDuration = 0.15f;
    [SerializeField] float moveSpeedThreshold = 0.1f;

    PlayerShipSensor _shipSensor;
    CharacterController _controller;
    bool _isDead;
    string _currentState;

    public void SetDead(bool deadState)
    {
        _isDead = deadState;
        if (_isDead)
            PlayState(StateDead);
    }

    void Awake()
    {
        _shipSensor = GetComponent<PlayerShipSensor>();
        _controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        ApplyClipOverrides();

        if (_isDead)
            PlayState(StateDead);
        else
            PlayState(StateIdle);
    }

    void Update()
    {
        if (animator == null || _isDead)
            return;

        bool inSpace = _shipSensor == null || !_shipSensor.IsInside;
        Vector3 velocity = _controller != null ? _controller.velocity : Vector3.zero;
        velocity.y = 0f;
        bool isMoving = velocity.sqrMagnitude >= moveSpeedThreshold * moveSpeedThreshold;

        if (inSpace)
            PlayState(isMoving ? StateSpaceMovement : StateSpaceIdle);
        else
            PlayState(isMoving ? StateRunning : StateIdle);
    }

    void PlayState(string stateName)
    {
        if (animator == null || stateName == _currentState)
            return;

        _currentState = stateName;
        animator.CrossFadeInFixedTime(stateName, crossFadeDuration);
    }

    void ApplyClipOverrides()
    {
        if (animator == null || baseController == null)
            return;

        var overrideController = new AnimatorOverrideController(baseController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip original = overrides[i].Key;
            if (original == null)
                continue;

            AnimationClip replacement = ResolveClip(original.name);
            if (replacement != null)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
        }

        overrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = overrideController;
        animator.applyRootMotion = false;
    }

    AnimationClip ResolveClip(string clipName)
    {
        switch (clipName)
        {
            case PlaceholderIdle:
            case "Idle":
                return idle;
            case PlaceholderRunning:
            case "Running":
            case "Run":
                return running;
            case PlaceholderSpaceMovement:
            case "SpaceMovement":
            case "Swim_Run":
                return spaceMovement;
            case PlaceholderSpaceIdle:
            case "SpaceIdle":
            case "Swim_Idle":
                return spaceIdle;
            case PlaceholderDead:
            case "Dead":
            case "LyingBack":
                return dead;
            default:
                return null;
        }
    }
}
