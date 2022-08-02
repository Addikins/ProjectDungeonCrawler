using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DC.Control
{
    public class Pet : MonoBehaviour
    {
        [SerializeField]
        AnimationAndMovementController player;
        [SerializeField]
        [Tooltip("Distance from player that the pet needs to get to before stopping.")]
        float comfortZone = 2f;
        [SerializeField]
        [Tooltip("Distance player can move away until pet follows.")]
        float tether = 5f;
        [SerializeField]
        [Tooltip("Distance until pet is considered out of bounds.")]
        float tetherTolerance = 25f;
        [SerializeField]
        [Tooltip("Time while out of bounds until pet will teleport back to player.")]
        float toleranceTimerLimit = 5f;
        [SerializeField]
        Animator petAnimator;

        private bool isMoving;
        private Transform target;
        private NavMeshAgent agent;
        private float toleranceTimer = 0f;

        //Animations
        private int isWalkingHash;
        private int isRunningHash;
        private int isDancingHash;

        private void Awake()
        {
            target = player.GetComponent<Transform>();
            agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = comfortZone;

            isWalkingHash = Animator.StringToHash("isWalking");
            isRunningHash = Animator.StringToHash("isRunning");
            isDancingHash = Animator.StringToHash("isDancing");
        }

        void Update()
        {
            CheckMovement();
            HandleAnimation();
        }

        private void CheckMovement()
        {
            float distance = Vector3.Distance(transform.position, target.position);
            if (isMoving)
            {
                if (distance <= comfortZone)
                {
                    isMoving = false;
                    //toleranceTimer = 0f;
                    Debug.Log("Landed in comfort zone!");
                }
                else
                {
                    CheckTetherTolerance(distance);
                    HandleMovement();
                }
            }
            else if (distance > tether)
            {
                isMoving = true;
                HandleMovement();
            }
        }

        private void CheckTetherTolerance(float distance)
        {
            if (distance >= tetherTolerance || !player.isMoving)
            {
                toleranceTimer += Time.deltaTime;
                if (toleranceTimer >= toleranceTimerLimit)
                {
                    //May want to provide an offset
                    agent.Warp(target.position);
                    toleranceTimer = 0f;
                }
            }
        }

        private void HandleMovement()
        {
            agent.speed = player.speed;
            agent.destination = target.position;
        }

        void HandleAnimation()
        {
            bool isWalkingAnimated = petAnimator.GetBool(isWalkingHash);
            bool isRunningAnimated = petAnimator.GetBool(isRunningHash);
            bool isDancingAnimated = petAnimator.GetBool(isDancingHash);

            if (isMoving && !isWalkingAnimated)
            {
                petAnimator.SetBool(isWalkingHash, true);
            }
            else if (!isMoving && isWalkingAnimated)
            {
                petAnimator.SetBool(isWalkingHash, false);
            }

            if ((isMoving && player.isRunning) && !isRunningAnimated)
            {
                petAnimator.SetBool(isRunningHash, true);
            }
            else if ((!isMoving || !player.isRunning) && isRunningAnimated)
            {
                petAnimator.SetBool(isRunningHash, false);
            }

            if (player.isDancing && !isMoving && !isDancingAnimated)
            {
                petAnimator.SetBool(isDancingHash, true);
            }
            else if (isMoving)
            {
                petAnimator.SetBool(isDancingHash, false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.transform.position, comfortZone);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.transform.position, tether);
        }
    }
}
