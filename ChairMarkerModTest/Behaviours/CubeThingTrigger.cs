using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChairMarkerModTest.Behaviours
{
    public class CubeThingTrigger : MonoBehaviour
    {
        private CubeThingScript itemScript;

        private void OnTriggerEnter(Collider other)
        {
            if (!itemScript.isHeld && (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Enemy")))
            {
                Debug.Log("Collided");
            }
        }
    }
}
