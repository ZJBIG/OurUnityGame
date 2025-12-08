using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public enum AnimationState
    {
        Flash,
        Dash,
        Through,
        DownWall,
        Jump,
        Run,
        Idle
    }

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Animator ani;
    private PlayerCtrl playerCtrl;
    private PlayerAbilities playerAbilities;

    public AnimationState currentState;
    private readonly Dictionary<AnimationState, string> stateToTrigger = new Dictionary<AnimationState, string>();

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        ani = GetComponent<Animator>();
        playerCtrl = GetComponent<PlayerCtrl>();
        playerAbilities = GetComponent<PlayerAbilities>();

        InitializeStateMapping();
        currentState = AnimationState.Idle;
        SetAnimationState(AnimationState.Idle);
    }

    void Update()
    {
        UpdateAnimation();
    }
    private void InitializeStateMapping()
    {
        stateToTrigger[AnimationState.Flash] = "Flash";
        stateToTrigger[AnimationState.Dash] = "Dash";
        stateToTrigger[AnimationState.Through] = "Through";
        stateToTrigger[AnimationState.DownWall] = "DownWall";
        stateToTrigger[AnimationState.Jump] = "Jump";
        stateToTrigger[AnimationState.Run] = "Run";
        stateToTrigger[AnimationState.Idle] = "Idle";
    }
    private void UpdateAnimation()
    {
        if (currentState != AnimationState.DownWall) sr.flipX = playerCtrl.Direction.x == -1;
        else sr.flipX = playerCtrl.Direction.x == 1;
        AnimationState targetState = DetermineTargetState();
        if (targetState != currentState)
            SetAnimationState(targetState);
    }
    private AnimationState DetermineTargetState()
    {
        //if (!playerAbilities.teleportCooldown)
        //    return AnimationState.Flash;
        if (!playerCtrl.canDash)
            return AnimationState.Dash;
        //if (playerAbilities.shootCooldown != 0)
        //    return AnimationState.Through;
        if (playerCtrl.WallDetect() && playerCtrl.inSky)
            return AnimationState.DownWall;
        if (playerCtrl.inSky)
            return AnimationState.Jump;
        if (rb.velocity.magnitude >= 0.01f)
            return AnimationState.Run;
        return AnimationState.Idle;
    }

    private void SetAnimationState(AnimationState newState)
    {
        ani.ResetTrigger(stateToTrigger[currentState]);
        ani.SetTrigger(stateToTrigger[newState]);
        currentState = newState;
    }
}