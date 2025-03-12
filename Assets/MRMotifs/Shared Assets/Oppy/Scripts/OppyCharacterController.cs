// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

/// <summary>
/// Listens to the user's input, moves and animates Oppy accordingly.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class OppyCharacterController : MonoBehaviour
{
    /// <summary>
    /// The transform who's the forward vector for Oppy's motion
    /// </summary>
    [SerializeField] private Transform movementFrameOfReference;

    /// <summary>
    ///  The vertical speed that Oppy will have if the jump button is pressed
    /// </summary>
    [SerializeField] private float jumpSpeed = 4;

    /// <summary>
    ///  The vertical acceleration applied to Oppy if the jump button is kept pressed
    /// </summary>
    [SerializeField] private float keepPressedJumpAcceleration = 1;

    [SerializeField] private OVRInput.Button jumpButton;

    /// <summary>
    ///  The transform in front of which Oppy will be respawned
    /// </summary>
    [SerializeField] private Transform respawnTransform;

    [SerializeField] private float maximumLinearSpeed = 0.9f;
    [SerializeField] private float gravity = -9.8f;

    private Animator _animator;
    private CharacterController _characterController;

    private bool _jumpRequested;
    private Vector3 _moveVelocity;
    private Quaternion _rotation;
    private Vector2 _motionInput;
    private JumpingState _jumpingState = JumpingState.Grounded;

    private static readonly int Jumping = Animator.StringToHash("Jumping");
    private static readonly int Landed = Animator.StringToHash("Landed");
    private static readonly int Running = Animator.StringToHash("Running");

    private const float JumpDelay = 0.16f;

    private enum JumpingState
    {
        Grounded,
        JumpStarted,
        JumpedAndAirborne
    }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        GetLocomotionInput();
        HandleLocomotion();
        HandleJumping();
        ApplyMotion();
    }

    public void Respawn()
    {
        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }

        _characterController.enabled = false;
        transform.position = respawnTransform.position + respawnTransform.forward * 0.3f;
        _characterController.enabled = true;
    }

    private void GetLocomotionInput()
    {
        var hInput = Input.GetAxis("Horizontal");
        var vInput = Input.GetAxis("Vertical");
        var thumbstickAxis = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        _motionInput = new Vector2(hInput + thumbstickAxis.x, vInput + thumbstickAxis.y);
    }

    private void ApplyMotion()
    {
        _moveVelocity.y += gravity * Time.deltaTime;
        _characterController.Move(_moveVelocity * Time.deltaTime);
        if (Mathf.Abs(_motionInput.y) > 0 || Mathf.Abs(_motionInput.x) > 0)
        {
            transform.rotation = _rotation;
        }
    }

    private void HandleLocomotion()
    {
        var noMovementInput = Mathf.Abs(_motionInput.y) == 0 && Mathf.Abs(_motionInput.x) == 0;
        _animator.SetBool(Running, !noMovementInput && _characterController.isGrounded);

        var motionForwardDirection = Vector3.ProjectOnPlane(movementFrameOfReference.forward, Vector3.up).normalized;
        var motionRightDirection = Vector3.ProjectOnPlane(movementFrameOfReference.right, Vector3.up).normalized;
        var motionDirection = (motionForwardDirection * _motionInput.y + motionRightDirection * _motionInput.x).normalized;
        _rotation = transform.rotation;

        if (!_characterController.isGrounded) return;
        _moveVelocity = motionDirection * maximumLinearSpeed;
        var lerpedMoveDirection = Vector3.Lerp(transform.forward, motionDirection, 0.6f);
        _rotation = Quaternion.LookRotation(lerpedMoveDirection);
    }

    private void HandleJumping()
    {
        var jumpButtonDown = OVRInput.GetDown(jumpButton) || Input.GetButtonDown("Jump");
        var jumpButtonPressed = OVRInput.Get(jumpButton) || Input.GetButton("Jump");

        if (_jumpRequested)
        {
            _moveVelocity.y = jumpSpeed;
            _jumpRequested = false;
        }

        if (_jumpingState == JumpingState.JumpStarted && !_characterController.isGrounded)
        {
            _jumpingState = JumpingState.JumpedAndAirborne;
        }

        if (_jumpingState != JumpingState.Grounded && jumpButtonPressed)
        {
            _moveVelocity.y += keepPressedJumpAcceleration * Time.deltaTime;
        }

        if (_jumpingState == JumpingState.Grounded && _characterController.isGrounded && jumpButtonDown)
        {
            _jumpingState = JumpingState.JumpStarted;
            StartCoroutine(RequestJumpAfterSeconds(JumpDelay));
            _animator.SetTrigger(Jumping);
        }
        else if (_characterController.isGrounded && _jumpingState == JumpingState.JumpedAndAirborne)
        {
            _animator.SetTrigger(Landed);
            _jumpingState = JumpingState.Grounded;
        }
    }

    private IEnumerator RequestJumpAfterSeconds(float delay)
    {
        yield return new WaitForSeconds(delay);
        _jumpRequested = true;
    }
}
