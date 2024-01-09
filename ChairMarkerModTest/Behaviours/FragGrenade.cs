using System.Collections;
using GameNetcodeStuff;
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

    public GameObject stunGrenadeExplosion;

    private PlayerControllerB playerThrownBy;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (inPullingPinAnimation)
        {
            return;
        }
        if (!pinPulled)
        {
            if (pullPinCoroutine == null)
            {
                playerHeldBy.activatingItem = true;
                pullPinCoroutine = StartCoroutine(pullPinAnimation());
            }
        }
        else if (base.IsOwner)
        {
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetGrenadeThrowDestination());
        }
    }

    public override void DiscardItem()
    {
        if (playerHeldBy != null)
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
        // string[] allLines = ((!pinPulled) ? new string[1] { "Pull pin: [RMB]" } : new string[1] { "Throw grenade: [RMB]" });
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
        fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
    }

    private IEnumerator pullPinAnimation()
    {
        inPullingPinAnimation = true;
        playerHeldBy.activatingItem = true;
        playerHeldBy.doingUpperBodyEmote = 1.16f;
        playerHeldBy.playerBodyAnimator.SetTrigger(playerAnimation);
        //itemAnimator.SetTrigger("pullPin");
        //itemAudio.PlayOneShot(pullPinSFX);
        WalkieTalkie.TransmitOneShotAudio(itemAudio, pullPinSFX, 0.8f);
        yield return new WaitForSeconds(1f);
        if (playerHeldBy != null)
        {
            if (!DestroyGrenade)
            {
                // ExplodeStunGrenade();
                playerHeldBy.activatingItem = false;
            }
            playerThrownBy = playerHeldBy;
        }
        inPullingPinAnimation = false;
        pinPulled = true;
        itemUsedUp = true;
        if (base.IsOwner && playerHeldBy != null)
        {
            SetControlTipForGrenade();
        }
    }

    public override void Update()
    {
        base.Update();
        if (pinPulled && !hasExploded)
        {
            explodeTimer += Time.deltaTime;
            if (explodeTimer > TimeToExplode)
            {
                ExplodeStunGrenade(DestroyGrenade);
                Debug.Log("BOOM---------------------------------------------!!");
            }
        }
    }

    private void ExplodeStunGrenade(bool destroy = false)
    {
        if (!hasExploded)
        {
            hasExploded = true;
            itemAudio.PlayOneShot(explodeSFX);
            WalkieTalkie.TransmitOneShotAudio(itemAudio, explodeSFX);
            Object.Instantiate(parent: (!isInElevator) ? RoundManager.Instance.mapPropsContainer.transform : StartOfRound.Instance.elevatorTransform, original: stunGrenadeExplosion, position: base.transform.position, rotation: Quaternion.identity);
            // StunExplosion(base.transform.position, affectAudio: true, 1f, 7.5f, 1f, isHeld, playerHeldBy, playerThrownBy);
            Landmine.SpawnExplosion(base.transform.position, true, 0.2f, 0.8f);
            if (DestroyGrenade)
            {
                DestroyObjectInHand(playerThrownBy);
            }
        }
    }

    public static void StunExplosion(Vector3 explosionPosition, bool affectAudio, float flashSeverityMultiplier, float enemyStunTime, float flashSeverityDistanceRolloff = 1f, bool isHeldItem = false, PlayerControllerB playerHeldBy = null, PlayerControllerB playerThrownBy = null)
    {
        PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
        {
            playerControllerB = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
        }
        float num = Vector3.Distance(playerControllerB.transform.position, explosionPosition);
        float num2 = 7f / (num * flashSeverityDistanceRolloff);
        if (Physics.Linecast(explosionPosition + Vector3.up * 0.5f, playerControllerB.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            num2 /= 13f;
        }
        else if (num < 2f)
        {
            num2 = 1f;
        }
        else if (!playerControllerB.HasLineOfSightToPosition(explosionPosition, 60f, 15, 2f))
        {
            num2 = Mathf.Clamp(num2 / 3f, 0f, 1f);
        }
        if (isHeldItem && playerHeldBy == GameNetworkManager.Instance.localPlayerController)
        {
            num2 = 1f;
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(20, hasDamageSFX: false, callRPC: true, CauseOfDeath.Blast);
        }
        num2 = Mathf.Clamp(num2 * flashSeverityMultiplier, 0f, 1f);
        HUDManager.Instance.flashbangScreenFilter.weight = num2;
        if (affectAudio)
        {
            SoundManager.Instance.earsRingingTimer = num2;
        }
        if (enemyStunTime <= 0f)
        {
            return;
        }
        Collider[] array = Physics.OverlapSphere(explosionPosition, 12f, 524288);
        if (array.Length == 0)
        {
            return;
        }
        for (int i = 0; i < array.Length; i++)
        {
            EnemyAICollisionDetect component = array[i].GetComponent<EnemyAICollisionDetect>();
            if (component == null)
            {
                continue;
            }
            Vector3 b = component.mainScript.transform.position + Vector3.up * 0.5f;
            if (component.mainScript.HasLineOfSightToPosition(explosionPosition + Vector3.up * 0.5f, 120f, 23, 7f) || (!Physics.Linecast(explosionPosition + Vector3.up * 0.5f, component.mainScript.transform.position + Vector3.up * 0.5f, 256) && Vector3.Distance(explosionPosition, b) < 11f))
            {
                if (playerThrownBy != null)
                {
                    component.mainScript.SetEnemyStunned(setToStunned: true, enemyStunTime, playerThrownBy);
                }
                else
                {
                    component.mainScript.SetEnemyStunned(setToStunned: true, enemyStunTime);
                }
            }
        }
    }

    public Vector3 GetGrenadeThrowDestination()
    {
        Vector3 position = base.transform.position;
        Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
        grenadeThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        position = ((!Physics.Raycast(grenadeThrowRay, out grenadeHit, 12f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) ? grenadeThrowRay.GetPoint(10f) : grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f));
        Debug.DrawRay(position, Vector3.down, Color.blue, 15f);
        grenadeThrowRay = new Ray(position, Vector3.down);
        if (Physics.Raycast(grenadeThrowRay, out grenadeHit, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            return grenadeHit.point + Vector3.up * 0.05f;
        }
        return grenadeThrowRay.GetPoint(30f);
    }

    protected override void __initializeVariables()
    {
        base.__initializeVariables();
    }

}