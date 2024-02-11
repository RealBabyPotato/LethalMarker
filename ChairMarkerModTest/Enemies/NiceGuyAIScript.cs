using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;
using static UnityEngine.LightAnchor;

/* TODO:
 * Stalking state
 * Longer he spends near you more something happens?
 * Bell sounds? the bell tolls!
 * Destroys doors?
 * Walkie talkie -- play sounds if within radius??!? (use WalkieTalkie.TransmitOneShotAudio static method)
 * Sync rotation
 */


namespace ChairMarkerModTest.Enemies
{
    internal class NiceGuyAIScript : EnemyAI
    {
        public AudioClip hitSFX;

        public Transform leftPos;
        //public Transform rightPos;

        public Vector3 leftOffset = new Vector3(0, 1.4f, 0);
        //public Vector3 rightOffset;

        private const float moveSpeed = 4f;
        private const float rotationSpeed = 4f;

        private bool flag;

        public AudioClip bell;
        public Transform turnCompass;

        public AISearchRoutine searchRoutine;

        private float rotationalUpdateTimer;
        private const float rotationUpdateThreshold = 3f;

        private float angerMeter;

        enum State
        {
            Searching,
            Stalking,
            Chasing,
            Attacking
        }

        public override void Start()
        {
            base.Start();

            // animations
            Debug.Log("Nice Guy spawned");
            currentBehaviourStateIndex = (int)State.Searching;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            if (!flag)
            {
                WalkieTalkie.TransmitOneShotAudio(creatureVoice, bell, 1);
                flag = true;
            }

        }

        public override void Update()
        {
            base.Update();

            rotationalUpdateTimer += Time.deltaTime;
            if(rotationalUpdateTimer >= rotationUpdateThreshold)
            {
                Debug.Log((State)currentBehaviourStateIndex);
                rotationalUpdateTimer = 0;
            }

            /*leftPos.position = base.transform.position + leftOffset;

            if (!targetPlayer && Vector3.Distance(base.transform.position, GetClosestPlayer().transform.position) <= 10f && !searchRoutine.inProgress)
            {
                targetPlayer = GetClosestPlayer();

                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);

                var direction = (targetPlayer.playerGlobalHead.position - transform.position).normalized;

                if (!targetPlayer.HasLineOfSightToPosition(leftPos.position))
                {
                    transform.position += direction * (Time.deltaTime * moveSpeed);
                }
            }
            else
            {
                targetPlayer = null;
                rotationalUpdateTimer += Time.deltaTime;

                if(rotationalUpdateTimer >= rotationUpdateThreshold)
                {
                    transform.rotation = Quaternion.identity;
                    rotationalUpdateTimer = 0;
                }
            }*/

            if (targetPlayer != null && PlayerIsTargetable(targetPlayer) && !searchRoutine.inProgress) 
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);

                /*if (targetPlayer.HasLineOfSightToPosition(leftPos.position))
                {
                    SwitchToBehaviourState((int)State.Chasing);
                }*/
            }

            if (stunNormalizedTimer > 0)
            {
                agent.speed = 0;
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if(isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }
            SearchForPlayerUnlessInRange(60, ref searchRoutine); // change range later!

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Searching:
                    agent.speed = 5f;
                    agent.acceleration = 8f;
                    break;

                case (int)State.Stalking:
                    Stalking();
                    break;

                case (int)State.Chasing:
                    Debug.Log("chasing");
                    break;

                case (int)State.Attacking: 
                    break;

                default:
                    Debug.Log("Current behaviour state doesn't exist!");
                    break;
            }
        }

        void SearchForPlayerUnlessInRange(float range, ref AISearchRoutine searchRoutine)
        {
            TargetClosestPlayer();
            if (targetPlayer != null && Vector3.Distance(base.transform.position, targetPlayer.transform.position) <= range)
            {
                if (searchRoutine.inProgress)
                {
                    StopSearch(searchRoutine);
                    SwitchToBehaviourClientRpc((int)State.Stalking);
                }
            }
            else
            {
                if (!searchRoutine.inProgress)
                {
                    StartSearch(transform.position, searchRoutine);
                    SwitchToBehaviourClientRpc((int)State.Searching);
                }
            }
        }

        void Stalking()
        {


            Debug.Log("speed: " + agent.speed + " velocity: " + agent.velocity +  " acceleration: " + agent.acceleration);

            if(targetPlayer == null || !IsOwner)
            {
                Debug.Log("target player null while stalking");
                return;
            }

            AvoidClosestPlayer();
        }

        private void CheckLinesOfSight()
        {

        }

        private void AvoidClosestPlayer()
        {
            float distanceToPlayer = Vector3.Distance(base.transform.position, targetPlayer.transform.position);

            Transform farthestNodeTransform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true);
            if (distanceToPlayer <= 20f && distanceToPlayer >= 5f && farthestNodeTransform != null) //&& this.HasLineOfSightToPosition(targetPlayer.transform.position)) // player is near ish, run away
            {
                agent.speed = 60f;
                agent.acceleration = 50f;
                Debug.Log("avoiding closest player, distance: " + distanceToPlayer);
                targetNode = farthestNodeTransform;
                SetDestinationToPosition(targetNode.position);
                return;
            } else if (distanceToPlayer >= 20f)
            {
                agent.speed = 0f;
                return;
            }
            else
            {
                SwitchToBehaviourClientRpc((int)State.Chasing);
            }

        }

    }

}
