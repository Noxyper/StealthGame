using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    void OnAnimatorMove()
    {
        Animator tempAnimator = GetComponent<Animator>();

        //transform.root.position += tempAnimator.deltaPosition;
    }
}
