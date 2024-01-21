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
        public GameObject stopFlag; // remove this later? this was mostly for testing purposes.

        public bool isShooting;
        private bool isHoldingButton;

        private Vector3 origPos;
        private int[] ignoreLayers = { 0, 18, 3, 13, 29, 9, 22 };

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            float chargeTime = 0f;

            Debug.Log(buttonDown);
            // isHoldingButton = buttonDown;

            // StartCoroutine(handleHoldButton());

            /*while (buttonDown && !isShooting)
            {
                chargeTime += 0.01f;
                Debug.Log("charging: " + chargeTime);
                
                if(chargeTime > 3f)
                {
                    break;
                }
            }*/

            if (base.IsOwner)
            {
                playerHeldBy.activatingItem = buttonDown;
            }

            if(!isShooting)
            {
                StartCoroutine(shootPiston(chargeTime));
            } 

        }

        private GameObject? firstItem()
        {
            bool flag = false;
            Collider[] colls = Physics.OverlapBox(pistonBlock.transform.position, pistonBlock.transform.localScale / 2f, Quaternion.identity); 

            foreach(Collider coll in colls)
            {
                if(coll.gameObject != null)
                {
                    GrabbableObject grabbableObject = coll.gameObject.GetComponent<GrabbableObject>();

                    if (grabbableObject != null)
                    {
                        return grabbableObject.gameObject;
                    }
                    
                    else if (ignoreLayers.Contains(coll.gameObject.layer))
                    {
                        continue;
                    }

                    Debug.Log("layer " + coll.gameObject.layer);
                    flag = true;
                    break;
                }
            }

            return flag ? stopFlag : null;
        }

        private IEnumerator handleHoldButton()
        {
            // float chargeTime = 0f;
            yield return new WaitUntil(() => !isHoldingButton);
            Debug.Log("stopped");
        }

        private IEnumerator shootPiston(float chargeTime) // charge up to increase range
        {
            origPos = piston.transform.localPosition;
            float origY = piston.transform.localPosition.y;
            isShooting = true;

            float yThreshhold = 1.5f + chargeTime;

            // extend
            for(float yOffset = 0; yOffset < yThreshhold; yOffset += 0.05f + (chargeTime / 100))
            {
                piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + origY, piston.transform.localPosition.z);

                if(yOffset > 0.5f)
                {
                    GameObject? returnedItem = firstItem();
                    if(returnedItem != null)
                    {
                        isShooting = false;

                        piston.transform.localPosition = origPos;

                        GrabbableObject obj = returnedItem.gameObject.GetComponent<GrabbableObject>();
                        if(obj != null)
                        {
                            obj.transform.position = origPos;
                            obj.FallToGround();
                        }

                        yield break;
                    } 
                }

                yield return null;
            }


            // retract
            for(float yOffset = yThreshhold; yOffset > 0f; yOffset -= 0.05f + (chargeTime / 25))
            {
                piston.transform.localPosition = new Vector3(piston.transform.localPosition.x, yOffset + origY, piston.transform.localPosition.z);
                yield return null;
            }

            piston.transform.localPosition = origPos;
            isShooting = false;
        }
    }
}
