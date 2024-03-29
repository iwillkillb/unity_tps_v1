﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Components
    public Animator _Animator;
    CharacterController _CharacterController;

    // Input field
    public float inputAxisHor { get; set; }
    public float inputAxisVer { get; set; }
    public bool inputJump { get; set; }

    [Header("Move")]
    public float moveSpeed = 7f;
    public float slopeForce = 5f;
    float inputAngle;   // Get angle by input.
    float verticalVelocity = 0f;
    Vector3 velocity;

    [Header("Rotation")]
    public float rotationSlerp = 5f;        // This is used by smooth rotation If you don't use NavMeshAgent's rotation.
    public bool isStaringFront = true;
    // True  : Character stares Camera's direction.
    // False : The character looks in the direction in which it moves.

    [Header("Jump")]
    public float gravity = 9.81f;
    public float jumpForce = 10f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashTime = 0.5f;
    float currentDashTime = 0f;



    // Start is called before the first frame update
    void Awake()
    {
        // Components connecting
        _CharacterController = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        // Input
        inputAxisHor = Input.GetAxis("Horizontal");
        inputAxisVer = Input.GetAxis("Vertical");
        inputJump = Input.GetButtonDown("Jump");

        SetVerticalVelocity();
        Movement();
        Rotation(isStaringFront);
        Dash();

        SetAnimationParameter();
    }

    void SetVerticalVelocity()
    {
        // Ground check and gravity
        if (_CharacterController.isGrounded)
        {
            if (verticalVelocity > -gravity)
                verticalVelocity = -gravity * Time.deltaTime;

            if (inputJump)
            {
                // Jump
                verticalVelocity = jumpForce;
            }
            else
            {
                // Not jump on ground -> Slope force
                float currentSlopeForce = Mathf.Lerp(0f, slopeForce, Vector3.Angle(Vector3.up, GetGroundNormal()) / _CharacterController.slopeLimit);
                verticalVelocity -= _CharacterController.height * currentSlopeForce;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;

            // Ceiling check
            if(_CharacterController.collisionFlags == CollisionFlags.Above)
            {
                verticalVelocity = (verticalVelocity > 0f) ? 0f : verticalVelocity;
            }
            /*
            Collider[] cols = Physics.OverlapSphere(transform.position + Vector3.up * _CharacterController.height, 0.1f);
            foreach (Collider col in cols)
            {
                // Except myself
                if (col.gameObject != gameObject)
                {
                    verticalVelocity = (verticalVelocity > 0f) ? 0f : verticalVelocity;
                    break;
                }
            }*/
        }
    }
    Vector3 GetGroundNormal()
    {
        // Slope Check
        RaycastHit slopeHit;
        Vector3 groundNormal = Vector3.up;
        if (Physics.Raycast(transform.position, -transform.up, out slopeHit, 0.1f))
        {
            groundNormal = slopeHit.normal;
        }

        return groundNormal;
    }

    void Movement()
    {
        // Initialization actual moving values.
        Vector3 moveAxis = Vector3.zero;
        Quaternion moveDir = Quaternion.identity;
        Quaternion groundQuaternion = Quaternion.FromToRotation(transform.up, GetGroundNormal());

        if (inputAxisHor != 0f || inputAxisVer != 0f)
        {
            // Character Rotation : Character and camera move in the apposite direction (in Y axis).
            // Example : 
            // 1. Input Right key -> inputAngle is 90
            // 2. Character moves Right -> Character rotates (inputAngle + Camera's current angle)

            // inputAngle : Front 0, Back 180, Left -90, Right 90
            inputAngle = Mathf.Atan2(inputAxisHor, inputAxisVer) * Mathf.Rad2Deg;

            // Get trnRotationReference's y rotation.
            moveDir = Quaternion.Euler(Vector3.up * Camera.main.transform.eulerAngles.y);

            // Use Input data
            moveAxis = (Vector3.right * inputAxisHor + Vector3.forward * inputAxisVer).normalized;
        }

        velocity = Vector3.up * verticalVelocity + groundQuaternion * (moveDir * moveAxis) * moveSpeed;

        // Actual Moving
        _CharacterController.Move(velocity * Time.deltaTime);
    }

    void Rotation(bool isStaringFront)
    {
        // Rotation backup
        Quaternion preRot = transform.rotation;  // Get rotation value in Previous frame.
        Quaternion newRot = transform.rotation;  // Set new direction rotation value.

        // Stare
        if (isStaringFront)
        {
            newRot = Quaternion.Euler(Vector3.up * Camera.main.transform.eulerAngles.y);
        }
        // No stare -> Rotate only moving
        else if (inputAxisHor != 0f || inputAxisVer != 0f)
        {
            newRot = Quaternion.Euler(Vector3.up * (inputAngle + Camera.main.transform.eulerAngles.y));
        }

        // Actual Rotation
        transform.rotation = Quaternion.Slerp(preRot, newRot, rotationSlerp * Time.deltaTime);
    }

    void Dash()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            currentDashTime = dashTime;
        }

        if (currentDashTime > 0f)
        {
            currentDashTime -= Time.deltaTime;

            Rotation(true);

            // Dash to camera's forward
            _CharacterController.Move(Quaternion.Euler(Vector3.up * Camera.main.transform.eulerAngles.y) * Vector3.forward * (dashSpeed * currentDashTime / dashTime) * Time.deltaTime);
        }
    }

    void SetAnimationParameter()
    {
        if (_Animator == null)
        {
            return;
        }

        if (isStaringFront)
        {
            _Animator.SetFloat("move", inputAxisVer);
            _Animator.SetFloat("direction", inputAxisHor);
        }
        else
        {
            _Animator.SetFloat("move", Mathf.Max(Mathf.Abs(inputAxisHor), Mathf.Abs(inputAxisVer)));
        }

        _Animator.SetBool("isGrounded", _CharacterController.isGrounded);
    }



    /*
    void LookAtPoint(Vector3 point)
    {
        Vector3 direction = (point - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }*/
}
