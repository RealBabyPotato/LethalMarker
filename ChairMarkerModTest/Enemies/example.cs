/*using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace ChairMarkerModTest.Enemies
{
    public class SpringManAI : EnemyAI
    {
        public AISearchRoutine searchForPlayers;

        private float checkLineOfSightInterval;

        private bool hasEnteredChaseMode;

        private bool stoppingMovement;

        private bool hasStopped;

        public AnimationStopPoints animStopPoints;

        private float currentChaseSpeed = 14.5f;

        private float currentAnimSpeed = 1f;

        private PlayerControllerB previousTarget;

        private bool wasOwnerLastFrame;

        private float stopAndGoMinimumInterval;

        private float timeSinceHittingPlayer;

        public AudioClip[] springNoises;

        public Collider mainCollider;

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (StartOfRound.Instance.allPlayersDead || isEnemyDead)
            {
                return;
            }
            switch (currentBehaviourStateIndex)
            {
                case 0:
                    {
                        if (!base.IsServer)
                        {
                            ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
                            break;
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) && !Physics.Linecast(base.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < 30f)
                            {
                                SwitchToBehaviourState(1);
                                return;
                            }
                        }
                        agent.speed = 6f;
                        if (!searchForPlayers.inProgress)
                        {
                            movingTowardsTargetPlayer = false;
                            StartSearch(base.transform.position, searchForPlayers);
                        }
                        break;
                    }
                case 1:
                    if (searchForPlayers.inProgress)
                    {
                        StopSearch(searchForPlayers);
                    }
                    if (TargetClosestPlayer())
                    {
                        if (previousTarget != targetPlayer)
                        {
                            previousTarget = targetPlayer;
                            ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
                        }
                        movingTowardsTargetPlayer = true;
                    }
                    else
                    {
                        SwitchToBehaviourState(0);
                        ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
                    }
                    break;
            }
        }

        public override void Update()
        {
            base.Update();
            if (isEnemyDead)
            {
                return;
            }
            if (timeSinceHittingPlayer >= 0f)
            {
                timeSinceHittingPlayer -= Time.deltaTime;
            }
            int num = currentBehaviourStateIndex;
            if (num == 0 || num != 1)
            {
                return;
            }
            if (base.IsOwner)
            {
                if (stopAndGoMinimumInterval > 0f)
                {
                    stopAndGoMinimumInterval -= Time.deltaTime;
                }
                if (!wasOwnerLastFrame)
                {
                    wasOwnerLastFrame = true;
                    if (!stoppingMovement && timeSinceHittingPlayer < 0.12f)
                    {
                        agent.speed = currentChaseSpeed;
                    }
                    else
                    {
                        agent.speed = 0f;
                    }
                }
                bool flag = false;
                for (int i = 0; i < 4; i++)
                {
                    if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) && StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position + Vector3.up * 1.6f, 68f) && Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, eye.position) > 0.3f)
                    {
                        flag = true;
                    }
                }
                if (stunNormalizedTimer > 0f)
                {
                    flag = true;
                }
                if (flag != stoppingMovement && stopAndGoMinimumInterval <= 0f)
                {
                    stopAndGoMinimumInterval = 0.15f;
                    if (flag)
                    {
                        SetAnimationStopServerRpc();
                    }
                    else
                    {
                        SetAnimationGoServerRpc();
                    }
                    stoppingMovement = flag;
                }
            }
            if (stoppingMovement)
            {
                if (!animStopPoints.canAnimationStop)
                {
                    return;
                }
                if (!hasStopped)
                {
                    hasStopped = true;
                    if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 70f, 25))
                    {
                        float num2 = Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
                        if (num2 < 4f)
                        {
                            GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f);
                        }
                        else if (num2 < 9f)
                        {
                            GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f);
                        }
                    }
                    if (currentAnimSpeed > 2f)
                    {
                        RoundManager.PlayRandomClip(creatureVoice, springNoises, randomize: false);
                        if (animStopPoints.animationPosition == 1)
                        {
                            creatureAnimator.SetTrigger("springBoing");
                        }
                        else
                        {
                            creatureAnimator.SetTrigger("springBoingPosition2");
                        }
                    }
                }
                if (mainCollider.isTrigger && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) > 0.25f)
                {
                    mainCollider.isTrigger = false;
                }
                creatureAnimator.SetFloat("walkSpeed", 0f);
                currentAnimSpeed = 0f;
                if (base.IsOwner)
                {
                    agent.speed = 0f;
                }
            }
            else
            {
                if (hasStopped)
                {
                    hasStopped = false;
                    mainCollider.isTrigger = true;
                }
                currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, 6f, 5f * Time.deltaTime);
                creatureAnimator.SetFloat("walkSpeed", currentAnimSpeed);
                if (base.IsOwner)
                {
                    agent.speed = Mathf.Lerp(agent.speed, currentChaseSpeed, 4.5f * Time.deltaTime);
                }
            }
        }

        [ServerRpc]
        public void SetAnimationStopServerRpc()
        {
            NetworkManager networkManager = base.NetworkManager;
            if ((object)networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
            {
                if (base.OwnerClientId != networkManager.LocalClientId)
                {
                    if (networkManager.LogLevel <= LogLevel.Normal)
                    {
                        Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
                    }
                    return;
                }
                ServerRpcParams serverRpcParams = default(ServerRpcParams);
                FastBufferWriter bufferWriter = __beginSendServerRpc(1502362896u, serverRpcParams, RpcDelivery.Reliable);
                __endSendServerRpc(ref bufferWriter, 1502362896u, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
            {
                SetAnimationStopClientRpc();
            }
        }

        [ClientRpc]
        public void SetAnimationStopClientRpc()
        {
            NetworkManager networkManager = base.NetworkManager;
            if ((object)networkManager != null && networkManager.IsListening)
            {
                if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
                {
                    ClientRpcParams clientRpcParams = default(ClientRpcParams);
                    FastBufferWriter bufferWriter = __beginSendClientRpc(718630829u, clientRpcParams, RpcDelivery.Reliable);
                    __endSendClientRpc(ref bufferWriter, 718630829u, clientRpcParams, RpcDelivery.Reliable);
                }
                if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
                {
                    stoppingMovement = true;
                }
            }
        }

        [ServerRpc]
        public void SetAnimationGoServerRpc()
        {
            NetworkManager networkManager = base.NetworkManager;
            if ((object)networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
            {
                if (base.OwnerClientId != networkManager.LocalClientId)
                {
                    if (networkManager.LogLevel <= LogLevel.Normal)
                    {
                        Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
                    }
                    return;
                }
                ServerRpcParams serverRpcParams = default(ServerRpcParams);
                FastBufferWriter bufferWriter = __beginSendServerRpc(339140592u, serverRpcParams, RpcDelivery.Reliable);
                __endSendServerRpc(ref bufferWriter, 339140592u, serverRpcParams, RpcDelivery.Reliable);
            }
            if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
            {
                SetAnimationGoClientRpc();
            }
        }

        [ClientRpc]
        public void SetAnimationGoClientRpc()
        {
            NetworkManager networkManager = base.NetworkManager;
            if ((object)networkManager != null && networkManager.IsListening)
            {
                if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
                {
                    ClientRpcParams clientRpcParams = default(ClientRpcParams);
                    FastBufferWriter bufferWriter = __beginSendClientRpc(3626523253u, clientRpcParams, RpcDelivery.Reliable);
                    __endSendClientRpc(ref bufferWriter, 3626523253u, clientRpcParams, RpcDelivery.Reliable);
                }
                if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
                {
                    stoppingMovement = false;
                }
            }
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (!stoppingMovement && currentBehaviourStateIndex == 1 && !(timeSinceHittingPlayer >= 0f))
            {
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
                if (playerControllerB != null)
                {
                    timeSinceHittingPlayer = 0.2f;
                    playerControllerB.DamagePlayer(90, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 2);
                    playerControllerB.JumpToFearLevel(1f);
                }
            }
        }

        protected override void __initializeVariables()
        {
            base.__initializeVariables();
        }

        [RuntimeInitializeOnLoadMethod]
        internal static void InitializeRPCS_SpringManAI()
        {
            NetworkManager.__rpc_func_table.Add(1502362896u, __rpc_handler_1502362896);
            NetworkManager.__rpc_func_table.Add(718630829u, __rpc_handler_718630829);
            NetworkManager.__rpc_func_table.Add(339140592u, __rpc_handler_339140592);
            NetworkManager.__rpc_func_table.Add(3626523253u, __rpc_handler_3626523253);
        }

        private static void __rpc_handler_1502362896(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            NetworkManager networkManager = target.NetworkManager;
            if ((object)networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
            {
                if (networkManager.LogLevel <= LogLevel.Normal)
                {
                    Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
                }
            }
            else
            {
                target.__rpc_exec_stage = __RpcExecStage.Server;
                ((SpringManAI)target).SetAnimationStopServerRpc();
                target.__rpc_exec_stage = __RpcExecStage.None;
            }
        }

        private static void __rpc_handler_718630829(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            NetworkManager networkManager = target.NetworkManager;
            if ((object)networkManager != null && networkManager.IsListening)
            {
                target.__rpc_exec_stage = __RpcExecStage.Client;
                ((SpringManAI)target).SetAnimationStopClientRpc();
                target.__rpc_exec_stage = __RpcExecStage.None;
            }
        }

        private static void __rpc_handler_339140592(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            NetworkManager networkManager = target.NetworkManager;
            if ((object)networkManager == null || !networkManager.IsListening)
            {
                return;
            }
            if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
            {
                if (networkManager.LogLevel <= LogLevel.Normal)
                {
                    Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
                }
            }
            else
            {
                target.__rpc_exec_stage = __RpcExecStage.Server;
                ((SpringManAI)target).SetAnimationGoServerRpc();
                target.__rpc_exec_stage = __RpcExecStage.None;
            }
        }

        private static void __rpc_handler_3626523253(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            NetworkManager networkManager = target.NetworkManager;
            if ((object)networkManager != null && networkManager.IsListening)
            {
                target.__rpc_exec_stage = __RpcExecStage.Client;
                ((SpringManAI)target).SetAnimationGoClientRpc();
                target.__rpc_exec_stage = __RpcExecStage.None;
            }
        }

        protected internal override string __getTypeName()
        {
            return "SpringManAI";
        }
    }

}*/
