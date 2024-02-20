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

            if (weatherChange == 5) { weatherChange = -1; }
            else { weatherChange++; }


            HUDManager.Instance.DisplayTip("WEATHER", "Weather type: " + (LevelWeatherType)weatherChange, false, false, "LC_Tip1");
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            playerHeldBy = lastPlayer;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown && playerHeldBy != null)
            {
                if (StartOfRound.Instance.inShipPhase)
                {
                    RoundManager.Instance.currentLevel.currentWeather = (LevelWeatherType)weatherChange;
                    HUDManager.Instance.DisplayTip("CHANGING WEATHER TYPE", "New weather: " + (LevelWeatherType)weatherChange, false, false, "LC_Tip1");
                    playerHeldBy.DiscardHeldObject();
                    Destroy(gameObject);
                } else {
                    HUDManager.Instance.DisplayTip("WEATHER TOTEM ERROR", "You're not in orbit!", true, false, "LC_Tip1");
                }

            }
        }
   }
}
