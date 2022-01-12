using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(LayerMask.GetMask("Player") == (LayerMask.GetMask("Player") | (1 << other.gameObject.layer)))
        {
            FindObjectOfType<GameManager>().EndGame(true);
        }
    }
}
