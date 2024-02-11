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

        private bool isFleeing;

        private float angerMeter;
        private const float stalkRange = 20f;

        enum State
        {
            Searching,
            Stalking,
            Chanting,
            Fleeing,
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

            StartSearch(transform.position);
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

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Searching:
                    agent.speed = 5f;
                    agent.acceleration = 8f;

                    if (FoundClosestPlayerInRange(stalkRange))
                    {
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.Stalking);
                    }

                    break;

                case (int)State.Stalking:
                    if (!TargetClosestPlayerInAnyCase())  
                    {
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.Searching);
                        return;
                    }
                    Stalking();
                    break;

                case (int)State.Chanting:
                    Chanting();
                    break;

                case (int)State.Fleeing:
                    // placeholder state
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

        // courtesy of ExampleEnemy on Github
        bool FoundClosestPlayerInRange(float range)
        {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
            if(targetPlayer == null) { return false;  }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) <= range;
        }

        bool TargetClosestPlayerInAnyCase()
        {
            mostOptimalDistance = 2000f;
            targetPlayer = null;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                if (tempDist < mostOptimalDistance)
                {
                    mostOptimalDistance = tempDist;
                    targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            if (targetPlayer == null) return false;
            return true;
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
            Debug.Log(distanceToPlayer);

            if (distanceToPlayer <= stalkRange)
            {
                AvoidClosestPlayer();
                return;
            } else if(distanceToPlayer >= 50) 
            {
                SwitchToBehaviourClientRpc((int)State.Chanting);
            }

            agent.speed = 0f;

        }

        void Chanting()
        {
            HandlePlayerVision();

            agent.speed = 0f;
        }

        private void HandlePlayerVision()
        {
            if (targetPlayer.HasLineOfSightToPosition(leftPos.position) && !isFleeing)
            {
                 StartCoroutine(Flee());
            }
        }

        private IEnumerator Flee()
        {
            isFleeing = true;
            SwitchToBehaviourClientRpc((int)State.Fleeing);

            while (true)
            {
                Debug.Log("attempting flee");
                float distanceToPlayer = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
                
                // player has successfully scared off nice guy
                if(distanceToPlayer >= 50)
                {
                    isFleeing = false;
                    SwitchToBehaviourClientRpc((int)State.Searching);
                    StartSearch(transform.position);
                    yield break;
                }

                AvoidClosestPlayer();
                yield return null;
            }


        }

        private void AvoidClosestPlayer()
        {

            Transform farthestNodeTransform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: false);
            if (farthestNodeTransform != null)
            {
                agent.speed = 60f;
                agent.acceleration = 50f;
                targetNode = farthestNodeTransform;
                SetDestinationToPosition(targetNode.position);
                return;
            }
            else
            {
                SwitchToBehaviourClientRpc((int)State.Attacking); 
            }

        }

    }

}
