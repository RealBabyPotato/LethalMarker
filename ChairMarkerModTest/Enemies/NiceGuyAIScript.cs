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
 */

/*
 * FINITE STATE MACHINE:
 * Stalks player, follows around until reaches a threshold distance
 * Once reached, stands still and emit a sound
 * --> if player can't find enemy in time limit
 *  -> get angry, reposition
 *      -> if really angry, enter chase state
 * 
 * otherwise run away (become less angry?)
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

        private bool flag;

        public AudioClip bell;
        public Transform turnCompass;

        public AISearchRoutine searchRoutine;

        private float rotationalUpdateTimer;
        private const float rotationUpdateThreshold = 3f;

        private float angerMeter;
        private const float stalkRange = 20f;

        enum State
        {
            Searching,
            Stalking,
            Chanting,
            Chasing,
            Attacking
        }

        public override void Start()
        {
            base.Start();

            // animations
            currentBehaviourStateIndex = (int)State.Searching;
            searchRoutine.searchWidth = 20f;
            searchRoutine.searchPrecision = 3;

            // targetNode = ChooseClosestNodeToPosition(base.transform.position);
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
            /*
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
            */

            if (targetPlayer != null && PlayerIsTargetable(targetPlayer) && !searchRoutine.inProgress) 
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
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

            if(currentBehaviourStateIndex != (int)State.Stalking || currentBehaviourStateIndex != (int)State.Chanting) SearchForPlayerUnlessInRange(60, ref searchRoutine); // change range later!

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Searching:
                    agent.speed = 5f;
                    agent.acceleration = 8f;
                    break;

                case (int)State.Stalking:
                    Stalking();
                    break;

                case (int)State.Chanting:
                    Chanting();
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
            if (targetPlayer != null)
            {
                Debug.Log(Vector3.Distance(this.transform.position, targetPlayer.transform.position));
            }

            if (targetPlayer != null && Vector3.Distance(base.transform.position, targetPlayer.transform.position) <= range)
            {
                if (searchRoutine.inProgress)
                {
                    StopSearch(searchRoutine);
                    SwitchToBehaviourClientRpc((int)State.Stalking);
                    return;
                }
            }
            else
            {
                if (!searchRoutine.inProgress)
                {
                    StartSearch(transform.position, searchRoutine);
                    SwitchToBehaviourClientRpc((int)State.Searching);
                    return;
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

            float distanceToPlayer = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
            // Debug.Log(distanceToPlayer);

            if (distanceToPlayer <= stalkRange)
            {
                AvoidClosestPlayer();
                return;
            } else if(distanceToPlayer >= 50) 
            {
                SwitchToBehaviourState((int)State.Chanting);
            }

            agent.speed = 0f;

        }

        void Chanting()
        {
            HandlePlayerVision();

            agent.speed = 0f;
            Debug.Log("chanting ahhh");
        }

        private void HandlePlayerVision()
        {
            if (targetPlayer.HasLineOfSightToPosition(leftPos.position))
            {
                Debug.Log("PLAYER IS LOOKING at me");
                AvoidClosestPlayer();
                SwitchToBehaviourState((int)State.Stalking);
            }
        }

        private void AvoidClosestPlayer()
        {

            Transform farthestNodeTransform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true);
            if (farthestNodeTransform != null) //&& this.HasLineOfSightToPosition(targetPlayer.transform.position)) // player is near ish, run away
            {
                agent.speed = 60f;
                agent.acceleration = 50f;
                targetNode = farthestNodeTransform;
                SetDestinationToPosition(targetNode.position);
                return;
            }
            else
            {
                //Debug.Log(farthestNodeTransform);
                SwitchToBehaviourState((int)State.Attacking); 
            }

        }

    }

}
