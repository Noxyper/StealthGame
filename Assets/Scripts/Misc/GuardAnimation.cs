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
    }

    void OnAnimatorIK()
    {
        _animator.SetLookAtWeight(aiAgent._lookAtWeight);
        _animator.SetLookAtPosition(aiAgent._lookAtPosition);
    }
}
