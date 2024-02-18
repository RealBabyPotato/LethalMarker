using System.Collections;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.Networking;
using UnityEngine.AI;

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
        private const float rotationUpdateThreshold = 3f;

        private float stalkingTime;
        private float stalkingTimeThreshold;

        private float repositionTime;
        private const float repositionTimerThreshold = 1.3f;

        private float chantingTime;
        private const float chantingTimeThreshold = 10f;
        private int numTimesChanted;

        // public GameObject mapDot;

        #endregion

        System.Random enemyRandom;

        private bool isFleeing;

        private const float stalkRange = 15f;
        private const float beginChantRange = 30f;

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

            agent.angularSpeed = 20f;

            UseDefaultWalkSettings();


            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            creatureVoice.volume = 2f;

            StartSearch(transform.position);
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

            rotationalUpdateTimer += Time.deltaTime;
            if(rotationalUpdateTimer >= rotationUpdateThreshold)
            {
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
                    stalkingTime = 0;
                    break;

                case (int)State.Stalking:

                    stalkingTime += Time.deltaTime;
                    stalkingTimeThreshold = (float)(enemyRandom.NextDouble() * 1.3) + 12.5f;

                    break;

                case (int)State.Chanting:
                    stalkingTime = 0;
                    chantingTime += Time.deltaTime;

                    if(!creatureVoice.isPlaying)
                    {
                        HandleChantClientRpc(false);
                        DoAnimationClientRpc("stopWalk");
                    }

                    if (targetPlayer != null)
                    {
                        turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
                    }

                    if (chantingTime >= chantingTimeThreshold) // finished chanting cycle
                    {
                        chantingTime = 0;
                        numTimesChanted++;

                        Debug.Log("Num times chanted: " + numTimesChanted);

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
                        DebugMeClientRpc();
                        UseDefaultWalkSettings();
                        // ChangeAnimatorSpeedClientRpc(2.5f);
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

            if(agent.velocity == Vector3.zero)
            {
                Debug.Log("ASSUMING NO NODES AVAILABLE, FLEEING");
                SwitchToBehaviourClientRpc((int)State.Fleeing);
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
                        UseDefaultWalkSettings();
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
                repositionTime += Time.deltaTime;

                // switch to aggressive if player follows around too much while fleeing
                if(repositionTime >= repositionTimerThreshold)
                {
                    repositionTime = 0;
                    PlayWarningClientRpc();
                    SwitchToBehaviourClientRpc((int)State.Chasing);
                } else if(repositionTime % (repositionTimerThreshold/4) <= 0.05 && !creatureVoice.isPlaying)
                {
                    PlayRattleClientRpc();
                }

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

            // DoAnimationClientRpc("stopWalk");
            agent.speed = 0f;
        }

        private void HandlePlayerVision()
        {
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(leftPos.position) && !isFleeing)
                {
                    // StartCoroutine(Flee());
                    SwitchToBehaviourClientRpc((int)State.Fleeing);
                    HandleChantClientRpc(true);
                }
            }

        }

        private IEnumerator Flee()
        {

            isFleeing = true;
            Color initialColour = bodyMaterial.color;

            // wave animation!!
            DoAnimationClientRpc("stopWalk");

            for(int i = 200; i > -1; i--)
            {
                Color col = bodyMaterial.color;
                col.a = i;
                bodyMaterial.color = col;

                yield return null;
            }

            while (true)
            {
                float distanceToPlayer = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
                
                // player has successfully scared off nice guy
                if(distanceToPlayer >= 50)
                {
                    isFleeing = false;

                    bodyMaterial.color = initialColour;

                    SwitchToBehaviourClientRpc((int)State.Searching);
                    UseDefaultWalkSettings();
                    StartSearch(transform.position);

                    yield break;
                }

                //AvoidClosestPlayer();
                TeleportFlee();
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void TeleportFlee()
        {
            Vector3 telePos = RoundManager.Instance.insideAINodes[enemyRandom.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
            telePos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(telePos, 10f, default(NavMeshHit), enemyRandom);

            // Transform farthestNodeTransform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true);
            base.transform.position = telePos;

            float distanceToPlayer = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
            Debug.Log("Trying teleport: " + base.transform.position + " distanceToPlayer: " + distanceToPlayer);

            SyncPositionToClients();
        }

        private void AvoidClosestPlayer(float distanceToPlayer = 1f)
        {

            Transform farthestNodeTransform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true);
            
            if (farthestNodeTransform != null) 
            {
                agent.acceleration = 150f;
                agent.speed = 60f / (distanceToPlayer / 3);

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
            Debug.Log("Setting animation: " + animationName);
        }

        [ClientRpc]
        private void ChangeAnimatorSpeedClientRpc(float speed)
        {
            creatureAnimator.speed = speed;
            Debug.Log("! ! ! Changing animator speed: " + speed);
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
