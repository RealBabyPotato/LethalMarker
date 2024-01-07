using GameNetcodeStuff;
using UnityEngine;

namespace ChairMarkerModTest.Behaviours
{
    internal class CubeThingScript : PhysicsProp 
    {

        PlayerControllerB? lastPlayer;
        int weatherChange = 0;

        public override void OnHitGround()
        {
            base.OnHitGround();
            Debug.Log(lastPlayer);

            // if (lastPlayer == null) return;

            if (weatherChange == 6) { weatherChange = 0; }
            else { weatherChange++; }

            HUDManager.Instance.DisplayTip("CHANGING WEATHER TYPE", "New weather type: " + (LevelWeatherType)weatherChange, false, false, "LC_Tip1");
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            playerHeldBy = lastPlayer;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                if (playerHeldBy != null)
                {
                    TimeOfDay.Instance.currentLevelWeather = (LevelWeatherType)weatherChange;
                    /*if(Random.Range(0, 0) == 0)
                    {
                        
                        // Landmine.S
                        //playerHeldBy.KillPlayer(Vector3.zero, true, CauseOfDeath.Electrocution);
                    }*/
                    HUDManager.Instance.DisplayTip("asdf", "fff" + (LevelWeatherType)weatherChange, false, false, "LC_Tip1");
                }
                else
                {
                    Debug.Log("playerHeldBy null!");
                }
            }
        }

        /*private void PlayThunderEffects(Vector3 strikePosition, AudioSource audio)
        {
            PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
            if (playerControllerB.isPlayerDead && playerControllerB.spectatedPlayerScript != null)
            {
                playerControllerB = playerControllerB.spectatedPlayerScript;
            }
            float num = Vector3.Distance(playerControllerB.gameplayCamera.transform.position, strikePosition);
            bool flag = false;
            if (num < 40f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            }
            else if (num < 110f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            }
            else
            {
                flag = true;
            }
            AudioClip[] array = ((!flag) ? strikeSFX : distantThunderSFX);
            if (!playerControllerB.isInsideFactory)
            {
                RoundManager.PlayRandomClip(audio, array);
            }
            WalkieTalkie.TransmitOneShotAudio(audio, array[UnityEngine.Random.Range(0, array.Length)]);
            if (StartOfRound.Instance.shipBounds.bounds.Contains(strikePosition))
            {
                StartOfRound.Instance.shipAnimatorObject.GetComponent<Animator>().SetTrigger("shipShake");
                RoundManager.PlayRandomClip(StartOfRound.Instance.ship3DAudio, StartOfRound.Instance.shipCreakSFX, randomize: false);
                StartOfRound.Instance.PowerSurgeShip();
            }
        }*/
    }
}
