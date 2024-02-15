using System.Collections;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.Networking;

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
        public AudioClip chanting;
        public AudioClip warning;
        public AudioClip[] hissing;

        public Transform leftPos;
        //public Transform rightPos;

        public Vector3 leftOffset = new Vector3(0, 1.4f, 0);
        //public Vector3 rightOffset;

        private bool flag;

        public AudioClip bell;
        public Transform turnCompass;

        //private NetworkVariable<float> stalkingTimerSynced = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public AISearchRoutine searchRoutine;

        #region timers

        private float rotationalUpdateTimer;
        private const float rotationUpdateThreshold = 3f;

        private float stalkingTime;
        private float stalkingTimeThreshold;

        private float repositionTime;
        private const float repositionTimerThreshold = 1.3f;

        #endregion

        System.Random enemyRandom;

        private bool isFleeing;

        private float angerMeter;
        private const float stalkRange = 15f;
        private const float beginChantRange = 30f;

        // Transform lastTraversedNode;

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

            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            creatureVoice.volume = 2f;

            StartSearch(transform.position);
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            if (!flag)
            {
                //WalkieTalkie.TransmitOneShotAudio(creatureVoice, bell, 1);
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
                PlayWarningClientRpc();
            }

            if (targetPlayer != null && PlayerIsTargetable(targetPlayer) && !searchRoutine.inProgress) 
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
            }

            if (stunNormalizedTimer > 0)
            {
                agent.speed = 0;
            }

            //Debug.Log(stalkingTimerSynced.Value);

            /*if(stalkingTime > stalkingTimeThreshold)
            {
                PlayWarningClientRpc();
            }*/
        }

        [ClientRpc]
        private void PlayWarningClientRpc()
        {
            creatureVoice.PlayOneShot(warning);
            Debug.Log("PlayWarningClientRpc Called (please show this on the client console :((((");
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
                        // creatureVoice.PlayOneShot(warning);
                        PlayWarningClientRpc();
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

                    creatureVoice.clip = chanting;
                    creatureVoice.time = Random.value * (chanting.length / 4);
                    creatureVoice.Play();

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

            SyncPositionToClients();
        }

        // courtesy of ExampleEnemy on Github
        bool FoundClosestPlayerInRange(float range)
        {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
            if(targetPlayer == null) { return false;  }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) <= range && this.HasLineOfSightToPosition(targetPlayer.transform.position);
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
            float distanceToPlayer = Vector3.Distance(base.transform.position, targetPlayer.transform.position);

            stalkingTime += Time.deltaTime;
            stalkingTimeThreshold = (float)(enemyRandom.NextDouble() * 1.3) + 2.5f;

            //stalkingTimerSynced.Value += Time.deltaTime;

            // Debug.Log("stalkingTime: " + stalkingTime + " repositionTime: " + repositionTime);
            //NetworkLog.LogInfo("stalkingTime: " + stalkingTime + " repositionTime: " + repositionTime);

            if(targetPlayer == null || !IsOwner)
            {
                Debug.Log("target player null while stalking");
                return;
            }

            if (distanceToPlayer <= stalkRange || targetPlayer.HasLineOfSightToPosition(base.transform.position))
            {
                repositionTime += Time.deltaTime;

                // switch to aggressive if player follows around too much while fleeing
                if(repositionTime >= repositionTimerThreshold)
                {
                    repositionTime = 0;
                    creatureVoice.PlayOneShot(warning); // FIX WITH RPC IF IT WORKS
                    SwitchToBehaviourClientRpc((int)State.Chasing);
                } else if(repositionTime % (repositionTimerThreshold/4) <= 0.02 && !creatureVoice.isPlaying)
                {
                    creatureVoice.PlayOneShot(hissing[enemyRandom.Next(0, hissing.Length - 1)]);
                }

                AvoidClosestPlayer(distanceToPlayer);
                return;
            } else if(stalkingTime >= stalkingTimeThreshold && distanceToPlayer <= beginChantRange)
            {
                stalkingTime = 0;
                repositionTime = 0;
                /*creatureVoice.clip = chanting;
                creatureVoice.time = Random.value * (chanting.length / 4);
                creatureVoice.Play();*/
                //creatureVoice.PlayOneShot(chanting);
                SwitchToBehaviourClientRpc((int)State.Chanting);
            }

            repositionTime = 0;

            agent.speed = 5f;
            agent.acceleration = 8f;

            Transform playerNode = ChooseClosestNodeToPosition(targetPlayer.transform.position, avoidLineOfSight: true);
            SetDestinationToPosition(playerNode.position);

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
                 creatureVoice.Stop();
            }
        }

        private IEnumerator Flee()
        {
            isFleeing = true;
            SwitchToBehaviourClientRpc((int)State.Fleeing);

            while (true)
            {
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

        private void AvoidClosestPlayer(float distanceToPlayer = 1f)
        {

            Transform farthestNodeTransform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true);
            Transform farthestNodeTransformBackup = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: false);
            Transform target;
            
            if(Vector3.Distance(farthestNodeTransform.position, targetPlayer.transform.position) > Vector3.Distance(farthestNodeTransformBackup.position, targetPlayer.transform.position)){
                target = farthestNodeTransform;
            }
            else
            {
                target = farthestNodeTransformBackup;
            }
            
            if (target != null) //&& farthestNodeTransform != lastTraversedNode)
            {
                agent.acceleration = 150f;
                agent.speed = 60f / (distanceToPlayer / 3);
                Debug.Log("Speed: " + agent.speed + " distance: " + distanceToPlayer);
                targetNode = farthestNodeTransform;
                SetDestinationToPosition(targetNode.position);
                // lastTraversedNode = farthestNodeTransform;
                return;
            }
            else
            {
                SwitchToBehaviourClientRpc((int)State.Attacking); 
            }

        }
    }
}
