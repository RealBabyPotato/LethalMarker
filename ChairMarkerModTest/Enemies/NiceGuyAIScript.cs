using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.LightAnchor;

namespace ChairMarkerModTest.Enemies
{
    internal class NiceGuyAIScript : EnemyAI
    {
        public AudioClip hitSFX;

        public Transform leftPos;
        //public Transform rightPos;

        public Vector3 leftOffset = new Vector3(0, 0.5f, 0);
        //public Vector3 rightOffset;

        //private const float moveSpeed = 4f;
        //private const float rotationSpeed = 4f;

        enum State
        {
            
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            Debug.Log("Skibidi Toilet");

        }

        public override void Update()
        {
            base.Update();
            leftPos.position = base.transform.position + leftOffset;

            if (!targetPlayer)
            {
                targetPlayer = GetClosestPlayer();

                Debug.Log("found player: " + targetPlayer.playerUsername);
            }

            var direction = (targetPlayer.playerGlobalHead.position - transform.position).normalized;
            var lookRotation = Quaternion.LookRotation(direction);

            if (!targetPlayer.HasLineOfSightToPosition(leftPos.position))
            {
                // Debug.Log("Player doesn't have line of sight!");
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 4f);
                transform.position += direction * (Time.deltaTime * 4f);
            }
            
        }
    }
}
