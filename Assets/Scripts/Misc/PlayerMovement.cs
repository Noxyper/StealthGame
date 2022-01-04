using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    CharacterController _characterController;
    [SerializeField] Animator _characterModel;
    [SerializeField] Camera _characterCamera;

    [Space] [SerializeField] float _speed;
    [SerializeField] float _lookSpeed;
    bool _isCrouching;
    Vector3 _facingDirection;
    Vector3 _cameraDefaultPosition;

    internal Cover _cover;

    public float Speed { get => _speed; private set => _speed = value; }

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _cameraDefaultPosition = _characterCamera.transform.localPosition;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X)) _characterModel.SetTrigger("yes");

        _isCrouching = _isCrouching ^ Input.GetKeyDown(KeyCode.LeftControl);
        _characterModel.SetBool("Crouching", _isCrouching);

        Vector3 tempMovement = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if (!_cover)
        {
            if (_characterModel.GetBool("Covered"))
            {
                _characterModel.SetBool("Covered", false);
            }

            _characterModel.SetInteger("IdleState", Random.Range(0, 3));
            _facingDirection = tempMovement != Vector3.zero ? tempMovement : _facingDirection;

            _characterController.SimpleMove(tempMovement * (Speed + (Input.GetKey(KeyCode.LeftShift) && !_isCrouching ? 2 : _isCrouching ? -2 : 0)));
            _characterModel.SetFloat("Speed", tempMovement != Vector3.zero ? (Speed + (Input.GetKey(KeyCode.LeftShift) && !_isCrouching ? 2 : _isCrouching ? -1 : 0)) : 0);
            _characterCamera.transform.localPosition = Vector3.Lerp(_characterCamera.transform.localPosition, _cameraDefaultPosition + (_facingDirection), Time.deltaTime * (_lookSpeed / 2));
        }
        else
        {
            if(!_characterModel.GetBool("Covered"))
            {
                _characterModel.SetTrigger("JustCovered");
                _characterModel.SetBool("Covered", true);
            }

            switch(_cover._wallDirection)
            {
                case CoverDirection.NORTH:
                    transform.position = new Vector3(transform.position.x, transform.position.y, tempMovement.z > 0 ? transform.position.z : _cover.Maximum.z);
                    _characterController.SimpleMove(new Vector3(tempMovement.z <= 0 ? tempMovement.x : 0, 0, tempMovement.z > 0 ? tempMovement.z : 0) * (Speed + (_isCrouching ? -1 : 0)));
                    _facingDirection = Vector3.back;

                    _characterModel.SetFloat("MoveSideways", Mathf.Lerp(_characterModel.GetFloat("MoveSideways"), -Input.GetAxisRaw("Horizontal"), Time.deltaTime * _lookSpeed));
                    _characterCamera.transform.localPosition = Vector3.Lerp(_characterCamera.transform.localPosition, _cameraDefaultPosition + (_facingDirection * -0.75f) + (tempMovement.z > 0 ? Vector3.zero : tempMovement * 2f), Time.deltaTime * (_lookSpeed / 2));
                    break;
                case CoverDirection.SOUTH:
                    transform.position = new Vector3(transform.position.x, transform.position.y, tempMovement.z < 0 ? transform.position.z : _cover.Minimum.z);
                    _characterController.SimpleMove(new Vector3(tempMovement.z >= 0 ? tempMovement.x : 0, 0, tempMovement.z < 0 ? tempMovement.z : 0) * (Speed + (_isCrouching ? -1 : 0)));
                    _facingDirection = Vector3.forward;

                    _characterModel.SetFloat("MoveSideways", Mathf.Lerp(_characterModel.GetFloat("MoveSideways"), Input.GetAxisRaw("Horizontal"), Time.deltaTime * _lookSpeed));
                    _characterCamera.transform.localPosition = Vector3.Lerp(_characterCamera.transform.localPosition, _cameraDefaultPosition + (_facingDirection * -0.75f) + (tempMovement.z < 0 ? Vector3.zero : tempMovement * 2f), Time.deltaTime * (_lookSpeed / 2));
                    break;
                case CoverDirection.EAST:
                    transform.position = new Vector3(tempMovement.x > 0 ? transform.position.x : _cover.Maximum.x, transform.position.y, transform.position.z);
                    _characterController.SimpleMove(new Vector3(tempMovement.x > 0 ? tempMovement.x : 0, 0, tempMovement.x <= 0 ? tempMovement.z : 0) * (Speed + (_isCrouching ? -1 : 0)));
                    _facingDirection = Vector3.left;

                    _characterModel.SetFloat("MoveSideways", Mathf.Lerp(_characterModel.GetFloat("MoveSideways"), Input.GetAxisRaw("Vertical"), Time.deltaTime * _lookSpeed));
                    _characterCamera.transform.localPosition = Vector3.Lerp(_characterCamera.transform.localPosition, _cameraDefaultPosition + (_facingDirection * -0.75f) + (tempMovement.x > 0 ? Vector3.zero : tempMovement * 2f), Time.deltaTime * (_lookSpeed / 2));
                    break;
                case CoverDirection.WEST:
                    transform.position = new Vector3(tempMovement.x < 0 ? transform.position.x : _cover.Minimum.x, transform.position.y, transform.position.z);
                    _characterController.SimpleMove(new Vector3(tempMovement.x < 0 ? tempMovement.x : 0, 0, tempMovement.x >= 0 ? tempMovement.z : 0) * (Speed + (_isCrouching ? -1 : 0)));
                    _facingDirection = Vector3.right;

                    _characterModel.SetFloat("MoveSideways", Mathf.Lerp(_characterModel.GetFloat("MoveSideways"), -Input.GetAxisRaw("Vertical"), Time.deltaTime * _lookSpeed));
                    _characterCamera.transform.localPosition = Vector3.Lerp(_characterCamera.transform.localPosition, _cameraDefaultPosition + (_facingDirection * -0.75f) + (tempMovement.x < 0 ? Vector3.zero : tempMovement * 2f), Time.deltaTime * (_lookSpeed / 2));
                    break;
            }

            _characterModel.SetFloat("Speed", tempMovement != Vector3.zero ? (Speed + (_isCrouching ? -2 : 0)) : 0);
        }

        _characterModel.transform.rotation = Quaternion.Slerp(_characterModel.transform.rotation, Quaternion.LookRotation(_facingDirection, Vector3.up), Time.deltaTime * _lookSpeed);
    }
}
