using UnityEngine;

namespace ChairMarkerModTest.Behaviours
{
    internal class VoodoDoll : PhysicsProp 
    {
        ScriptableObject _obj;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                if(playerHeldBy != null)
                {
                    Debug.Log("used");
                }
            }
        }
    }
}
