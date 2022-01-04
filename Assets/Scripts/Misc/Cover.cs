using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CoverDirection
{
    NORTH, SOUTH, EAST, WEST
}

[RequireComponent(typeof(Collider))]
public class Cover : MonoBehaviour
{
    Vector3 _centre, _size, _min, _max;

    public Vector3 Center { get => _centre; private set => _centre = value; }
    public Vector3 Size { get => _size; private set => _size = value; }
    public Vector3 Minimum { get => _min; private set => _min = value; }
    public Vector3 Maximum { get => _max; private set => _max = value; }

    internal CoverDirection _wallDirection;
    [SerializeField] internal bool _insideWall;

    void Start()
    {
        switch (tag)
        {
            case "CoverNorth":
                _wallDirection = CoverDirection.NORTH;
                break;
            case "CoverSouth":
                _wallDirection = CoverDirection.SOUTH;
                break;
            case "CoverEast":
                _wallDirection = CoverDirection.EAST;
                break;
            case "CoverWest":
                _wallDirection = CoverDirection.WEST;
                break;
        }

        _centre = GetComponent<Collider>().bounds.center;
        _size = GetComponent<Collider>().bounds.size;
        _min = GetComponent<Collider>().bounds.min;
        _max = GetComponent<Collider>().bounds.max;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>())
        {
            other.GetComponent<PlayerMovement>()._cover = this;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() && other.GetComponent<PlayerMovement>()._cover != null)
        {
            if (_insideWall)
            {
                if ((_wallDirection == CoverDirection.NORTH && Input.GetAxisRaw("Vertical") < 0) ||
                   (_wallDirection == CoverDirection.SOUTH && Input.GetAxisRaw("Vertical") > 0) ||
                   (_wallDirection == CoverDirection.EAST && Input.GetAxisRaw("Horizontal") < 0) ||
                   (_wallDirection == CoverDirection.WEST && Input.GetAxisRaw("Horizontal") > 0))
                    other.GetComponent<PlayerMovement>()._cover = this;
            }
            else
            {
                if ((_wallDirection == CoverDirection.NORTH && Input.GetAxisRaw("Vertical") > 0) ||
                   (_wallDirection == CoverDirection.SOUTH && Input.GetAxisRaw("Vertical") < 0) ||
                   (_wallDirection == CoverDirection.EAST && Input.GetAxisRaw("Horizontal") > 0) ||
                   (_wallDirection == CoverDirection.WEST && Input.GetAxisRaw("Horizontal") < 0))
                    other.GetComponent<PlayerMovement>()._cover = this;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() && other.GetComponent<PlayerMovement>()._cover == this)
        {
            other.GetComponent<PlayerMovement>()._cover = null;
        }
    }
}
