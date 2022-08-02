using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GD.MinMaxSlider;

namespace DC.Control
{
    public class AnimationAndMovementController : MonoBehaviour
    {
        //Shared Variables
        [Header("Movement Controls")]
        [SerializeField]
        [Range(0.5f, 25f)]
        float walkSpeed = 1.0f;
        [SerializeField]
        [Range(0.5f, 25f)]
        float runSpeed = 3.0f;
        [SerializeField]
        [Tooltip("Toggle Running by default for click to move. This will be an option the player can choose for themselves as well.")]
        private bool runOnClick;
        [SerializeField]
        [Tooltip("Sets the threshold in which the player will stop trying to move once they are this close to the target.")]
        private float targetThreshold = 0.1f;
        [SerializeField]
        [Tooltip("How fast the character rotates when changing directions.")]
        [Range(0.5f, 100f)]
        float rotationFactorPerFrame = 1.0f;


        private Vector3 currentMovement;
        private bool isRunPressed;

        public bool isMoving;
        public bool isRunning;
        public bool isDancing;
        public float speed = 0f;

        //Controller Dependencies
        private Camera mainCamera;
        private PlayerInput playerInput;
        private CharacterController characterController;
        private Animator animator;

        //Gamepad Or Keyboard
        private Vector2 currentMovementInput;
        private bool isMovementPressed;

        //Click to Move
        private Vector3 targetPosition;
        private bool isClickToMove;
        private bool mousePressed;

        //Animations
        private int isWalkingHash;
        private int isRunningHash;
        private int isDancingHash;

        [Header("Camera Controls")]
        [SerializeField]
        private float cameraScrollSpeed = 10;
        [SerializeField]
        [MinMaxSlider(2, 45)]
        Vector2 cameraSizeRange = new Vector2(5f, 15f);

        private Vector3 currentCameraInput;

        private void Awake()
        {
            mainCamera = Camera.main;
            playerInput = new PlayerInput();
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();

            isWalkingHash = Animator.StringToHash("isWalking");
            isRunningHash = Animator.StringToHash("isRunning");
            isDancingHash = Animator.StringToHash("isDancing");

            playerInput.CharacterControls.Move.started += onMovementInput;
            playerInput.CharacterControls.Move.canceled += onMovementInput;
            playerInput.CharacterControls.Move.performed += onMovementInput;
            playerInput.CharacterControls.Run.started += onRun;
            playerInput.CharacterControls.Run.canceled += onRun;
            playerInput.CharacterControls.ClickToMove.performed += onClickToMove;
            playerInput.CharacterControls.ClickToMove.canceled += onClickToMoveStop;
            playerInput.CharacterControls.Camera.started += onCameraZoom;
            playerInput.CharacterControls.Dance.performed += onDanceInput;
        }

        private void OnEnable()
        {
            playerInput.CharacterControls.Enable();
        }
        private void OnDisable()
        {
            playerInput.CharacterControls.Disable();
        }

        void Update()
        {
            HandleRotation();
            HandleAnimation();
            HandleMovement();
        }

        private void onClickToMove(InputAction.CallbackContext context)
        {
            mousePressed = true;
            ClickToMove();
        }

        private void onClickToMoveStop(InputAction.CallbackContext context)
        {
            mousePressed = false;
        }

        private void ClickToMove()
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray: ray, hitInfo: out var hit) && hit.collider)
            {
                isClickToMove = true;
                isDancing = false;
                targetPosition = hit.point;
                targetPosition.y += (transform.position.y - targetPosition.y);
            }
        }

        private void onMovementInput(InputAction.CallbackContext context)
        {
            isClickToMove = false;
            isDancing = false;
            currentMovementInput = context.ReadValue<Vector2>();
            currentMovement = new Vector3(currentMovementInput.x, 0, currentMovementInput.y);
            //currentMovement.x = currentMovementInput.x;
            //currentMovement.z = currentMovementInput.y;
            currentMovement = IsometricConversion(currentMovement);
            isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
        }
        private void onRun(InputAction.CallbackContext context)
        {
            isRunPressed = context.ReadValueAsButton();
        }
        private void onCameraZoom(InputAction.CallbackContext context)
        {
            currentCameraInput = context.ReadValue<Vector2>();
            float amountToScroll = currentCameraInput.y >= 0 ? 1 : -1;
            amountToScroll *= cameraScrollSpeed;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - amountToScroll, cameraSizeRange.x, cameraSizeRange.y);
        }
        private void onDanceInput(InputAction.CallbackContext context)
        {
            isDancing = true;
        }

        private void HandleMovement()
        {
            isMoving = isClickToMove || isMovementPressed;
            isRunning = isRunPressed || (isClickToMove && runOnClick);
            speed = isRunning || (isClickToMove && runOnClick) ? runSpeed : walkSpeed;
            if (mousePressed) ClickToMove();

            if (isMoving)
            {
                isClickToMove = Vector3.Distance(transform.position, targetPosition) > targetThreshold ? isClickToMove : false;
                if (isClickToMove)
                {
                    Vector3 direction = targetPosition - transform.position;
                    currentMovement = direction.normalized;
                }
                if (!characterController.isGrounded) currentMovement.y -= 1;
                else currentMovement.y = 0;
                characterController.Move(currentMovement * Time.deltaTime * speed);
            }
        }
        void HandleRotation()
        {
            Vector3 positionToLookAt;

            positionToLookAt.x = currentMovement.x;
            positionToLookAt.y = 0.0f;
            positionToLookAt.z = currentMovement.z;

            Quaternion currentRotation = transform.rotation;
            if (isMoving && currentMovement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
                transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
            }
        }
        void HandleAnimation()
        {
            bool isWalkingAnimated = animator.GetBool(isWalkingHash);
            bool isRunningAnimated = animator.GetBool(isRunningHash);
            bool isDancingAnimated = animator.GetBool(isDancingHash);

            if (isMoving && !isWalkingAnimated)
            {
                animator.SetBool(isWalkingHash, true);
            }
            else if (!isMoving && isWalkingAnimated)
            {
                animator.SetBool(isWalkingHash, false);
            }

            if ((isMoving && isRunning) && !isRunningAnimated)
            {
                animator.SetBool(isRunningHash, true);
            }
            else if ((!isMoving || !isRunning) && isRunningAnimated)
            {
                animator.SetBool(isRunningHash, false);
            }

            if (isDancing && !isMoving && !isDancingAnimated)
            {
                animator.SetBool(isDancingHash, true);
            }
            else if (isMoving)
            {
                animator.SetBool(isDancingHash, false);
            }
        }

        private Vector3 IsometricConversion(Vector3 vector)
        {
            Quaternion rotation = Quaternion.Euler(0, 45.0f, 0);
            Matrix4x4 matrix = Matrix4x4.Rotate(rotation);
            Vector3 result = matrix.MultiplyPoint3x4(vector);
            return result;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, .5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(targetPosition, targetThreshold);
        }
    }
}
