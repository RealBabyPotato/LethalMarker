using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Collections;

namespace ChairMarkerModTest.Behaviours
{
    internal class ExtendoArmBehaviour : GrabbableObject 
    {
        public GameObject piston;
        public bool isShooting;

        private Coroutine shootPistonCoroutine;

        public override void ItemActivate(bool used, bool buttonDown = false)
        {
            base.ItemActivate(used, buttonDown);

            if (!isShooting && shootPistonCoroutine == null)
            {
                shootPistonCoroutine = StartCoroutine(shootPiston());
            } else
            {
                Debug.Log("Can't shoot! isShooting: " + isShooting + " shootPistonCoroutine: " + shootPistonCoroutine);
            }

        }

        private IEnumerator shootPiston()
        {
            Vector3 origPos = piston.transform.localPosition;
            float origY = piston.transform.localPosition.y;
            isShooting = true;
            // extend
            for(float yOffset = 0; yOffset < 1.5f; yOffset += 0.01f)
            {
                Debug.Log(yOffset);
                piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + origY, piston.transform.localPosition.z);
                yield return null;
            }


            // retract
            for(float yOffset = 1.5f; yOffset > 0f; yOffset -= 0.01f)
            {
                piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + origY, piston.transform.localPosition.z);
                Debug.Log(yOffset);
                yield return null;
            }

            piston.transform.localPosition = origPos;
            isShooting = false;
        }
    }
}
