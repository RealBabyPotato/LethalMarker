﻿using System;
using System.Collections;
using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class FragGrenadeScript : GrabbableObject
{
    [Header("Frag grenade settings")]
    public float TimeToExplode = 2.25f;

    public bool DestroyGrenade;

    public string playerAnimation = "PullGrenadePin";

    [Space(3f)]
    public bool pinPulled;

    public bool inPullingPinAnimation;

    private Coroutine pullPinCoroutine;

    public Animator itemAnimator;

    public AudioSource itemAudio;

    public AudioClip pullPinSFX;

    public AudioClip explodeSFX;

    public AnimationCurve grenadeFallCurve;

    public AnimationCurve grenadeVerticalFallCurve;

    public AnimationCurve grenadeVerticalFallCurveNoBounce;

    public RaycastHit grenadeHit;

    public Ray grenadeThrowRay;

    public float explodeTimer;

    public bool hasExploded;

    public GameObject fragGrenadeExplosion;

    public GameObject trajectoryIndicator;

    private PlayerControllerB playerThrownBy;

    public override void ItemActivate(bool used, bool buttonDown = true) // buttonDown?
    {
        base.ItemActivate(used, buttonDown);
        
        // HUDManager.Instance.itemSlotIcons[1].etc change slot icon with this

        if (inPullingPinAnimation)
        {
            return;
        }

        if (!pinPulled && pullPinCoroutine == null)
        {
            itemAudio.PlayOneShot(pullPinSFX);
            playerHeldBy.activatingItem = true;
            pullPinCoroutine = StartCoroutine(pullPinAnimation());

        }

    }

    public override void DiscardItem()
    {
        if (playerHeldBy != null && !pinPulled)
        {
            playerHeldBy.activatingItem = false;
        }
        base.DiscardItem();
    }

    public override void EquipItem()
    {
        SetControlTipForGrenade();
        EnableItemMeshes(enable: true);
        isPocketed = false;
    }

    
    private void SetControlTipForGrenade()
    {
        if (base.IsOwner)
        {
            HUDManager.Instance.ChangeControlTipMultiple(new string[] { "Throw grenade: [LMB] " }, holdingItem: true, itemProperties);
        }
    }

    public override void FallWithCurve()
    {
        float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
        base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z), 14f * Time.deltaTime / magnitude);
        base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, grenadeFallCurve.Evaluate(fallTime));
        if (magnitude > 5f)
        {
            base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), grenadeVerticalFallCurveNoBounce.Evaluate(fallTime));
        } 
        else
        {
            base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), grenadeVerticalFallCurve.Evaluate(fallTime));
        }
        fallTime += Mathf.Abs(Time.deltaTime * 15f / magnitude);
    }


    private IEnumerator pullPinAnimation()
    {
        inPullingPinAnimation = true;
        playerHeldBy.activatingItem = true;
        playerHeldBy.doingUpperBodyEmote = 1.16f;
        playerHeldBy.playerBodyAnimator.SetTrigger(playerAnimation);
        itemAnimator.SetTrigger("pullPin");
        // itemAudio.PlayOneShot(pullPinSFX);
        // WalkieTalkie.TransmitOneShotAudio(itemAudio, pullPinSFX, 0.8f);
        yield return new WaitForSeconds(1.3f);
        if (playerHeldBy != null)
        {
            if (!DestroyGrenade)
            {
                playerHeldBy.activatingItem = false;
            }
            playerThrownBy = playerHeldBy;
        }
        inPullingPinAnimation = false;
        pinPulled = true;
        itemUsedUp = true;

        if (base.IsOwner && playerHeldBy != null)
        {
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetGrenadeThrowDestination());
        }
    }

    public override void Update()
    {
        base.Update();

        if (isHeld && playerHeldBy != null)
        {
            trajectoryIndicator.transform.position = GetGrenadeThrowDestination();
            trajectoryIndicator.SetActive(true);
        }
        else
        {
            trajectoryIndicator.SetActive(false);
        }

        if (pinPulled && !hasExploded)
        {
            explodeTimer += Time.deltaTime;
            if (explodeTimer > TimeToExplode)
            {
                ExplodeFragGrenade(DestroyGrenade);
            }
        }
    }

    private void ExplodeFragGrenade(bool destroy = false)
    {
        if (!hasExploded)
        {
            hasExploded = true;
            itemAudio.PlayOneShot(explodeSFX);
            WalkieTalkie.TransmitOneShotAudio(itemAudio, explodeSFX);
            UnityEngine.Object.Instantiate(parent: (!isInElevator) ? RoundManager.Instance.mapPropsContainer.transform : StartOfRound.Instance.elevatorTransform, original: fragGrenadeExplosion, position: base.transform.position, rotation: Quaternion.identity);
            FragExplosion(base.transform.position, true,52f, 100f);
            if (destroy)
            {
                DestroyObjectInHand(playerThrownBy);
            }
        }
    } 

    public void FragExplosion(Vector3 explosionPosition, bool spawnExplosion, float killRange, float damageRange) // modified landmine explosion
    {
        Landmine.SpawnExplosion(explosionPosition, spawnExplosion, killRange, damageRange);
        Destroy(gameObject);
    }

    public Vector3 GetGrenadeThrowDestination()
    {
        Vector3 position = base.transform.position;
        Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
        grenadeThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        position = ((!Physics.Raycast(grenadeThrowRay, out grenadeHit, 12f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) ? grenadeThrowRay.GetPoint(10f) : grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f));
        Debug.DrawRay(position, Vector3.down, Color.blue, 15f);
        grenadeThrowRay = new Ray(position, Vector3.down);
        if (Physics.Raycast(grenadeThrowRay, out grenadeHit, 60f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            return grenadeHit.point + Vector3.up * 0.05f;
        }
        return grenadeThrowRay.GetPoint(330f);
    }

    protected override void __initializeVariables()
    {
        base.__initializeVariables();
    }

}