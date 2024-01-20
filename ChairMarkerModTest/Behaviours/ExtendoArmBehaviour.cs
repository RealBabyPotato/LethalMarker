using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Collections;

namespace ChairMarkerModTest.Behaviours
{
    internal class ExtendoArmBehaviour : GrabbableObject 
    {
        public GameObject pistonBlock;
        public GameObject piston;

        private Coroutine? shootCoroutine;

        public bool isShooting;

        private Vector3 origPos;

        public override void ItemActivate(bool used, bool buttonDown = false)
        {
            base.ItemActivate(used, buttonDown);

            if (!isShooting && shootCoroutine == null)
            {
                shootCoroutine = StartCoroutine(shootPiston());
                // StartCoroutine(shootPiston());
            } 

        }

        private GameObject firstItem()
        {
            Collider[] colls = Physics.OverlapBox(pistonBlock.transform.position, pistonBlock.transform.localScale / 2f, Quaternion.identity); // change to actual hitbox at the end of piston thing

            foreach(Collider coll in colls)
            {
                Debug.Log("LAYER + " + coll.gameObject.layer);
                /*if(coll.gameObject.layer == LayerMask.NameToLayer("Colliders"){
                    return ;
                }*/

                return coll.gameObject;

                GrabbableObject grabbableObject = coll.gameObject.GetComponent<GrabbableObject>();

                if (grabbableObject != null)
                {
                    return grabbableObject.gameObject;
                }


            }

            return null;
        }

        public override void Update()
        {
            base.Update();
            while (isShooting)
            {
                GameObject returnedItem = firstItem();
                if(returnedItem != null)
                {
                    StopCoroutine(shootCoroutine);
                    shootCoroutine = null;
                    isShooting = false;
                    piston.transform.localPosition = origPos != null ? origPos : new Vector3(0, 0.5f, 0);
                    break;
                    
                }
            }
        }

        private IEnumerator shootPiston()
        {
            origPos = piston.transform.localPosition;
            float origY = piston.transform.localPosition.y;
            isShooting = true;
            // extend
            for(float yOffset = 0; yOffset < 1.5f; yOffset += 0.05f)
            {
                piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + origY, piston.transform.localPosition.z);

                yield return null;
            }


            // retract
            for(float yOffset = 1.5f; yOffset > 0f; yOffset -= 0.05f)
            {
                piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + origY, piston.transform.localPosition.z);
                yield return null;
            }

            piston.transform.localPosition = origPos;
            isShooting = false;
        }
    }
}
