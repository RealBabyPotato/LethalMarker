using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace ChairMarkerModTest.Behaviours
{
    internal class ExtendoArmBehaviour : GrabbableObject 
    {
        public GameObject pistonBlock;
        public GameObject piston;

        private Coroutine? shootCoroutine;

        public bool isShooting;

        private Vector3 origPos;
        private int[] ignoreLayers = { 0, 18, 3};

        public override void ItemActivate(bool used, bool buttonDown = false)
        {
            base.ItemActivate(used, buttonDown);

            //if (!isShooting && shootCoroutine == null)
            if(!isShooting)
            {
                // shootCoroutine = StartCoroutine(shootPiston());
                StartCoroutine(shootPiston());
            } 

        }

        private GameObject? firstItem()
        {
            Collider[] colls = Physics.OverlapBox(pistonBlock.transform.position, pistonBlock.transform.localScale / 2f, Quaternion.identity); 
            Debug.Log(colls.Length);

            foreach(Collider coll in colls)
            {
                if(coll.gameObject != null)
                {
                    Debug.Log("layer " + coll.gameObject.layer);
                    GrabbableObject grabbableObject = coll.gameObject.GetComponent<GrabbableObject>();

                    if (grabbableObject != null)
                    {
                        return grabbableObject.gameObject;
                    }
                    
                    else if (ignoreLayers.Contains(coll.gameObject.layer))
                    {
                        continue;
                    }

                    // return new GameObject();
                    break;
                }
            }

            return null;
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

                GameObject? returnedItem = firstItem();
                if(returnedItem != null)
                {
                    isShooting = false;

                    /*for(float y = yOffset; y > 0f; yOffset -= 0.3f)
                    {
                        piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + y, piston.transform.localPosition.z);
                    }*/
                    piston.transform.localPosition = origPos;

                    GrabbableObject obj = returnedItem.gameObject.GetComponent<GrabbableObject>();
                    if(obj != null)
                    {
                        obj.transform.position = origPos;
                        obj.FallToGround();
                    }

                    yield break;
                }

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
