using UnityEngine;

namespace ChairMarkerModTest.Behaviours
{
    internal class CubeThingScript : PhysicsProp
    {
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                if (playerHeldBy != null)
                {
                    Debug.Log("USED ITEM");
                    playerHeldBy.DamagePlayer(20);
                    //RoundManager.Instance.SpawnEnemyOnServer(playerHeldBy.thisPlayerBody.position, 0f, 2); // wip doesn't work as 2 is out of enemyIndex range
                }
                else
                {
                    Debug.Log("playerHeldBy null!");
                }
            }
        }
    }
}
