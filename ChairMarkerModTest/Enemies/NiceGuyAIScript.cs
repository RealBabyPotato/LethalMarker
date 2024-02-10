using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;
using static UnityEngine.LightAnchor;

/* TODO:
 * Stalking state
 * Longer he spends near you more something happens?
 * Bell sounds? the bell tolls!
 * Destroys doors?
 * Walkie talkie -- play sounds if within radius??!? (use WalkieTalkie.TransmitOneShotAudio static method)
 * Sync rotation
 */


namespace ChairMarkerModTest.Enemies
{
    internal class NiceGuyAIScript : EnemyAI
    {
        public AudioClip hitSFX;

        public Transform leftPos;
        //public Transform rightPos;

        public Vector3 leftOffset = new Vector3(0, 1.4f, 0);
        //public Vector3 rightOffset;

        private const float moveSpeed = 4f;
        private const float rotationSpeed = 4f;

        private bool flag;

        public AudioClip bell;

        enum State
        {
            Stalking,
            Chasing,
            
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            if (!flag)
            {
                WalkieTalkie.TransmitOneShotAudio(creatureVoice, bell, 1);
                flag = true;
            }

        }

        public override void Update()
        {
            base.Update();
            leftPos.position = base.transform.position + leftOffset;

            if (!targetPlayer && Vector3.Distance(base.transform.position, GetClosestPlayer().transform.position) <= 10f)
            {
                targetPlayer = GetClosestPlayer();
                Debug.Log("found player: " + targetPlayer.playerUsername);

                var direction = (targetPlayer.playerGlobalHead.position - transform.position).normalized;
                var lookRotation = Quaternion.LookRotation(direction);

                if (!targetPlayer.HasLineOfSightToPosition(leftPos.position))
                {
                    // Debug.Log("Player doesn't have line of sight!");
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
                    transform.position += direction * (Time.deltaTime * moveSpeed);
                }
            }
            else
            {
                targetPlayer = null;
                Debug.Log("No player");
            }
        }
    }
}
