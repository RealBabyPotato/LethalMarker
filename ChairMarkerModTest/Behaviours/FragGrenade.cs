using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChairMarkerModTest.Behaviours
{
    public class FragGrenadeScript : StunGrenadeItem
    {
        public override void Update()
        {
            if (pinPulled && !hasExploded)
            {
                explodeTimer += Time.deltaTime;
                if (explodeTimer > TimeToExplode)
                {
                    // ExplodeStunGrenade(DestroyGrenade);
                    Debug.Log("EXPLODE!!!");
                }
            }
        }
    }
}
