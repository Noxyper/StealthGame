using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class GuardAnimation : MonoBehaviour
{
    public AIAgent aiAgent;

    Animator _animator;

    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        _animator.SetFloat("Speed", aiAgent._agentVelocity.magnitude);
        if (aiAgent._aiState == AIState.ALERTED) _animator.speed = 1.666666f;
        else _animator.speed = 1f;
    }

    void OnAnimatorIK()
    {
        _animator.SetLookAtWeight(aiAgent._lookAtWeight);
        _animator.SetLookAtPosition(aiAgent._lookAtPosition);
    }
}
