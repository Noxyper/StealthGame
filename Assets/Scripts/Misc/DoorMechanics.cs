using DG.Tweening;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class DoorMechanics : MonoBehaviour
{
    [System.Serializable]
    public struct Door
    {
        public GameObject right;
        public GameObject left;
    }

    public Color doorColor = Color.grey;
    public Door door;
    public LayerMask doorOpensTo;
    [Space]
    public float openingPosition = 1f;
    public float openingSpeed = 1f;

    internal bool _keyPickedUp;

    void Start()
    {
        if (door.right.GetComponent<MeshRenderer>())
        {
            door.right.GetComponent<MeshRenderer>().material.color = doorColor;
        }

        if (door.left.GetComponent<MeshRenderer>())
        {
            door.left.GetComponent<MeshRenderer>().material.color = doorColor;
        }
    }

    void Update()
    {
        if (_keyPickedUp)
        {
            door.left.transform.DOLocalMoveX(-openingPosition, openingSpeed);
            door.right.transform.DOLocalMoveX(openingPosition, openingSpeed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (doorOpensTo == (doorOpensTo | (1 << other.gameObject.layer)))
        {
            door.left.transform.DOLocalMoveX(-openingPosition, openingSpeed);
            door.right.transform.DOLocalMoveX(openingPosition, openingSpeed);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (doorOpensTo == (doorOpensTo | (1 << other.gameObject.layer)))
        {
            door.left.transform.DOLocalMoveX(0f, openingSpeed);
            door.right.transform.DOLocalMoveX(0f, openingSpeed);
        }
    }
}
