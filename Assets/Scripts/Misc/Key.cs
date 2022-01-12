using DG.Tweening;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Key : MonoBehaviour
{
    public DoorMechanics doorConnection;
    [Space]
    public Color doorColor = Color.grey;
    public float keyRotationSpeed = 1f;

    void Start()
    {
        if (GetComponent<MeshRenderer>())
        {
            GetComponent<MeshRenderer>().material.color = doorColor;
        }
        transform.DOBlendableRotateBy(Vector3.up * keyRotationSpeed, 1f).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
    }

    void OnTriggerEnter(Collider other)
    {
        if (LayerMask.GetMask("Player") == (LayerMask.GetMask("Player") | (1 << other.gameObject.layer)))
        {
            doorConnection._keyPickedUp = true;
            Destroy(gameObject);
        }
    }
}
