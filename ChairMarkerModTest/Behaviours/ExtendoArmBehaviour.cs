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

        public override void ItemActivate(bool used, bool buttonDown = false)
        {
            base.ItemActivate(used, buttonDown);

            if (!isShooting)
            {
                StartCoroutine(shootPiston());
            } 

        }

        private GrabbableObject firstItem()
        {
            Collider[] colls = Physics.OverlapBox(piston.transform.position, transform.localScale / 2f, Quaternion.identity); // change to actual hitbox at the end of piston thing

            foreach(Collider coll in colls)
            {
                GrabbableObject grabbableObject = coll.gameObject.GetComponent<GrabbableObject>();

                if (grabbableObject != null)
                {
                    return grabbableObject;
                }
            }

            return null;
        }

        private IEnumerator shootPiston()
        {
            Vector3 origPos = piston.transform.localPosition;
            float origY = piston.transform.localPosition.y;
            isShooting = true;
            // extend
            for(float yOffset = 0; yOffset < 1.5f; yOffset += 0.05f)
            {
                Debug.Log(yOffset);
                piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + origY, piston.transform.localPosition.z);
                yield return null;
            }


            // retract
            for(float yOffset = 1.5f; yOffset > 0f; yOffset -= 0.05f)
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
