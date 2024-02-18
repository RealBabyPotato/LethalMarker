using System.Collections;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.Networking;
using UnityEngine.AI;
using System.Collections.Generic;

/* TODO:
 * Longer he spends near you more something happens?
 * Bell sounds? the bell tolls!
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

        public AudioClip[] footsteps;

        public Transform leftPos;
        //public Transform rightPos;

        public Vector3 leftOffset = new Vector3(0, 1.4f, 0);
        //public Vector3 rightOffset;

        private bool flag;

        public AudioClip bell;
        public Transform turnCompass;
        public Material bodyMaterial;

        //private NetworkVariable<float> stalkingTimerSynced = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public AISearchRoutine searchRoutine;

        #region timers

        private float rotationalUpdateTimer;
        private const float rotationUpdateThreshold = 0.5f;

        private float stalkingTime;
        private float stalkingTimeThreshold;

        private float repositionTime;
        private const float repositionTimerThreshold = 1.3f;

        private float chantingTime;
        private const float chantingTimeThreshold = 10f;
        private int numTimesChanted;

        private float idleTime;

        // public GameObject mapDot;

        #endregion

        System.Random enemyRandom;

        private bool isFleeing;
        private bool isChanting;

        private const float stalkRange = 15f;
        private const float beginChantRange = 30f;

        private Color col;

        private Vector3 expectedPos;

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
            StartSearch(transform.position);

            //agent.angularSpeed = 20f;
            agent.angularSpeed = 1000f; // helps with janky positioning when rotating

            UseDefaultWalkSettings();

            col = bodyMaterial.color;

            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            creatureVoice.volume = 1.6f;

        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            if (!flag)
            {
                flag = true;
            }

        }

        public override void Update()
        {
            base.Update();

            Debug.Log("current pos: " + base.transform.position + " expected pos: " + expectedPos);

            rotationalUpdateTimer += Time.deltaTime;
            if(rotationalUpdateTimer >= rotationUpdateThreshold)
            {
                //Debug.Log("State: " + (State)currentBehaviourStateIndex + " repositionTimer: " + repositionTime + " stalkngTime: " + stalkingTime + " stalkingTimeThreshold: " + stalkingTimeThreshold + " chatningTime: " + chantingTime);
                Debug.Log((State)currentBehaviourStateIndex);
                rotationalUpdateTimer = 0;
            }

            if (stunNormalizedTimer > 0)
            {
                agent.speed = 0;
            }

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Searching:

                    if(bodyMaterial.color != col)
                    {
                        bodyMaterial.color = col;
                    }

                    stalkingTime = 0;
                    break;

                case (int)State.Stalking:

                    stalkingTime += Time.deltaTime;
                    stalkingTimeThreshold = (float)(enemyRandom.NextDouble() * 1.3) + 12.5f;

                    if(agent.velocity == Vector3.zero)
                    {
                        idleTime += Time.deltaTime;

                        if(idleTime > 0.6f && !isFleeing)
                        {
                            Debug.Log("ASSUMING NO NODES AVAILABLE, FLEEING");
                            SwitchToBehaviourClientRpc((int)State.Fleeing);
                            return;
                        }
                    }
                    else
                    {
                        idleTime = 0;
                    }

                    break;

                case (int)State.Chanting:
                    stalkingTime = 0;
                    chantingTime += Time.deltaTime;

                    if(!isChanting)
                    {
                        Debug.Log("!!!!!!!!!! SHOLD BE BEGINNING CHANT");
                        isChanting = true;
                        HandleChantClientRpc(false);
                        ChangeAnimatorSpeedClientRpc(1);
                        DoAnimationClientRpc("stopWalk");
                    }

                    if (targetPlayer != null)
                    {
                        turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
                    }

                    if (chantingTime >= chantingTimeThreshold) // finished chanting cycle
                    {
                        isChanting = false;
                        chantingTime = 0;
                        numTimesChanted++;
                        targetPlayer.JumpToFearLevel(0.75f);

                        Debug.Log("Num times chanted: " + numTimesChanted + " TARGETPLAYER: " + targetPlayer.playerUsername);

                        if(numTimesChanted == 4)
                        {
                            UseDefaultWalkSettings();
                            SwitchToBehaviourClientRpc((int)State.Chasing);
                        }

                        else
                        {
                            HandleChantClientRpc(true);
                            PlayRattleClientRpc();
                            UseDefaultWalkSettings();
                            SwitchToBehaviourClientRpc((int)State.Searching);
                        }
                    }

                    break;

                case (int)State.Fleeing:

                    stalkingTime = 0;

                    if (!isFleeing)
                    {
                        ChangeAnimatorSpeedClientRpc(1);
                        DoAnimationClientRpc("stopWalk");
                        StartCoroutine(Flee());
                    }

                    break;

                case (int)State.Chasing:
                    stalkingTime = 0;
                    break;

                case (int)State.Attacking:
                    stalkingTime = 0;
                    break;

                default:
                    stalkingTime = 0;
                    Debug.Log("Current behaviour state doesn't exist!");
                    break;
            }
        }

        public void PlayFootstepSound()
        {
            creatureSFX.PlayOneShot(footsteps[enemyRandom.Next(0, footsteps.Length - 1)]);
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
                    UseDefaultWalkSettings();

                    if (FoundClosestPlayerInRange(stalkRange))
                    {
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.Stalking);
                    }

                    break;

                case (int)State.Stalking:
                    if (!TargetClosestPlayerInAnyCase())  
                    {
                        // UseDefaultWalkSettings();
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
            float distanceToPlayer = Vector3.Distance(base.transform.position, targetPlayer.transform.position);

            if(targetPlayer == null || !IsOwner)
            {
                Debug.Log("target player null while stalking or we aren't owner!");
                return;
            }

            // player is too close -- reposition or get angry!
            if (distanceToPlayer <= stalkRange || targetPlayer.HasLineOfSightToPosition(base.transform.position))
            {
                /*repositionTime += Time.deltaTime;

                // switch to aggressive if player follows around too much while fleeing
                if(repositionTime >= repositionTimerThreshold)
                {
                    repositionTime = 0;
                    PlayWarningClientRpc();
                    SwitchToBehaviourClientRpc((int)State.Chasing);
                } else if(repositionTime % (repositionTimerThreshold/4) <= 0.05 && !creatureVoice.isPlaying) // 
                {
                    PlayRattleClientRpc();
                }*/

                AvoidClosestPlayer(distanceToPlayer);

                return;
            } else if(stalkingTime >= stalkingTimeThreshold && distanceToPlayer <= beginChantRange)
            {
                stalkingTime = 0;
                repositionTime = 0;

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
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(leftPos.position) && !isFleeing)
                {
                    // StartCoroutine(Flee());
                    isChanting = false;
                    chantingTime = 0;
                    SwitchToBehaviourClientRpc((int)State.Fleeing);
                    HandleChantClientRpc(true);
                }
            }
        }

        private IEnumerator Flee()
        {
            isFleeing = true;

            // wave animation!!

            DoAnimationClientRpc("stopWalk");
            StopSearch(currentSearch);

            Color col = bodyMaterial.color;

            for(int i = 200; i > -1; i--)
            {
                col.a = i;
                bodyMaterial.color = col;

                yield return null;
            }

            while (true)
            {

                Vector3 telePos = RoundManager.Instance.insideAINodes[enemyRandom.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                //Transform floorNode = RoundManager.Instance.allEnemyVents[enemyRandom.Next(0, RoundManager.Instance.allEnemyVents.Length)].floorNode;
                //Vector3 telePos = floorNode.position; 
                telePos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(telePos, 30f, default(NavMeshHit), enemyRandom);

                float distanceToPlayer = Vector3.Distance(telePos, targetPlayer.transform.position);
                Debug.Log("trying tele: " + distanceToPlayer + " with vent pos: " + telePos + " num vents: " + RoundManager.Instance.allEnemyVents.Length);
                
                // player has successfully scared off nice guy
                if(!NearOtherPlayers(telePos, 50f))
                {
                    expectedPos = telePos;

                    agent.Warp(telePos);

                    SyncPositionToClients();

                    isFleeing = false;
                    bodyMaterial.color = col;

                    UseDefaultWalkSettings();
                    SwitchToBehaviourClientRpc((int)State.Searching);
                    StartSearch(telePos);

                    // SwitchToBehaviourClientRpc((int)State.Chasing);


                    yield break;
                }

                yield return null;
            }
        }

        private bool NearOtherPlayers(Vector3 checkPos, float checkRadius)
        {
            base.gameObject.layer = 0;
            bool result = Physics.CheckSphere(checkPos, checkRadius, 8, QueryTriggerInteraction.Ignore);
            base.gameObject.layer = 19;
            return result;
        }

        private void AvoidClosestPlayer(float distanceToPlayer = 1f)
        {

            Transform farthestNodeTransform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true);
            
            if (farthestNodeTransform != null) 
            {
                agent.acceleration = 150f;
                agent.speed = 40f / (distanceToPlayer / 3);

                if(distanceToPlayer >= 5f)
                {
                    ChangeAnimatorSpeedClientRpc(Mathf.Max(1/(distanceToPlayer / 12), 1));
                }
                else
                {
                    ChangeAnimatorSpeedClientRpc(3f);
                }


                targetNode = farthestNodeTransform;
                SetDestinationToPosition(targetNode.position);
                return;
            }
            else
            {
                // SwitchToBehaviourClientRpc((int)State.Attacking); 
                Debug.Log("AvoidClosestPlayer farthestNodeTransform null");
            }

        }

        private void UseDefaultWalkSettings()
        {
            DoAnimationClientRpc("startWalk");
            ChangeAnimatorSpeedClientRpc(1);
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName)
        {
            creatureAnimator.SetTrigger(animationName);
        }

        [ClientRpc]
        private void ChangeAnimatorSpeedClientRpc(float speed)
        {
            creatureAnimator.speed = speed;
        }

        [ClientRpc]
        private void PlayRattleClientRpc()
        {
            creatureVoice.PlayOneShot(hissing[enemyRandom.Next(0, hissing.Length - 1)]);
        }

        [ClientRpc]
        private void HandleChantClientRpc(bool stop) // rn this randmoizes the clip starting length, meant for chanting state
        {
            if (stop)
            {
                creatureVoice.Stop();
                return;
            }

            creatureVoice.clip = chanting; 
            creatureVoice.time = Random.value * (chanting.length / 4); // this doesn't get synced, but i don't think it needs to 
            creatureVoice.Play();
        }

        [ClientRpc]
        private void PlayWarningClientRpc()
        {
            creatureVoice.PlayOneShot(warning);
        }

        [ClientRpc]
        private void DebugMeClientRpc()
        {
            Debug.Log(" ------------------ DebugMe Client Rpc --------------- ");
        }
    }
}
