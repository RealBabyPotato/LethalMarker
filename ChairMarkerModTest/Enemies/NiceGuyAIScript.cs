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
        int tick = 0;

        //private const float moveSpeed = 4f;
        //private const float rotationSpeed = 4f;

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            Debug.Log("Skibidi Toilet");

        }

        public override void Update()
        {
            base.Update();
            tick++;
            var direction = (targetPlayer.playerGlobalHead.position - transform.position).normalized;

            if (!targetPlayer)
            {
                targetPlayer = GetClosestPlayer();

                Debug.Log("found player: " + targetPlayer.playerUsername);
            }
            
            if(tick % 5 == 0)
            {
                Debug.Log("tick");
                var lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 4f);
            }
            transform.position += direction * (Time.deltaTime * 4f);
        }
    }
}
