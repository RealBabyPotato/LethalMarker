﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using BepInEx.Bootstrap;
using ChairMarkerModTest.Behaviours;
using Unity.Netcode;

namespace ChairMarkerModTest
{
    [BepInPlugin(GUID, NAME, VERSION)]
    // [BepInDependency("evaisa.lethallib", "0.6.0")]
    public class Plugin : BaseUnityPlugin
    {
        readonly Harmony harmony = new Harmony(GUID);
        const string GUID = "jaden.chairMarkerMod";
        const string NAME = "Chair Marker";
        const string VERSION = "0.0.1";

        public static Plugin instance;

        AssetBundle bundle;
        
        void Awake()
        {
            instance = this;


            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lethalmarker");
            bundle = AssetBundle.LoadFromFile(assetDir);

            SetupItems();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
            Logger.LogInfo("CMOD Loaded!");
        }

        private void SetupItems() // this is messy. oh well.
        {
            SetupCubeThing();
            SetupGrenade();
        }

        AnimationCurve CreateGravityCurve(float duration, float gravity)
        {
            AnimationCurve curve = new AnimationCurve();

            curve.AddKey(0f, 0f);

            float currentTime = 0f;
            float timeStep = 0.1f;

            while (currentTime <= duration)
            {
                float velocity = gravity * currentTime;
                curve.AddKey(currentTime, velocity);

                currentTime += timeStep;
            }

            return curve;
        }

        private void SetupGrenade()
        {
            Item FragGrenade = bundle.LoadAsset<Item>("Assets/Mod/Frag Grenade/FragGrenade.asset");

            //AudioSource fragAudio = FragGrenade.spawnPrefab.AddComponent<AudioSource>();
            //fragAudio.clip = bundle.LoadAsset<AudioClip>("Assets/Mod/Cube Thing/fb64d9f6-7584-4a3f-930b-ba6094d37fd5.mp3"); // this kind of works :(
            // AudioSource fragAudio = FragGrenade.spawnPrefab.GetComponent<AudioSource>(); // omg this works!!! let's go!!!

            if (FragGrenade == null) return;

            FragGrenade.minValue = 20;
            FragGrenade.maxValue = 30;
            FragGrenade.creditsWorth = 32;

            FragGrenade.weight = 1.04f;
            FragGrenade.itemId = 69698;

            FragGrenadeScript fragScript = FragGrenade.spawnPrefab.AddComponent<FragGrenadeScript>();
            fragScript.itemProperties = FragGrenade;
            fragScript.grabbable = true;
            fragScript.grabbableToEnemies = true;

            fragScript.fragGrenadeExplosion = FragGrenade.spawnPrefab;
            fragScript.itemAudio = FragGrenade.spawnPrefab.GetComponent<AudioSource>();
            fragScript.itemAnimator = FragGrenade.spawnPrefab.GetComponent<Animator>();
            fragScript.DestroyGrenade = true;
            fragScript.grenadeFallCurve = CreateGravityCurve(1f, 9.8f);

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(FragGrenade.spawnPrefab);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "kill your foes!\n\n";
            Items.RegisterShopItem(FragGrenade, null, null, node, 10);

        }

        private void SetupCubeThing() // TODO: refactor this/change to weather totem, also make an asset
        {
            Item CubeThing = bundle.LoadAsset<Item>("Assets/Mod/Cube Thing/Cube Thing.asset");
            if (CubeThing == null) return;

            CubeThing.creditsWorth = 30;
            CubeThing.itemId = 2001;
            CubeThing.weight = 1.05f;
            CubeThing.itemSpawnsOnGround = true;
            CubeThing.canBeGrabbedBeforeGameStart = true;

            CubeThingScript cubeScript = CubeThing.spawnPrefab.AddComponent<CubeThingScript>();
            cubeScript.itemProperties = CubeThing;
            cubeScript.grabbable = true;
            cubeScript.grabbableToEnemies = true;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(CubeThing.spawnPrefab);
            
            Items.RegisterScrap(CubeThing, 1000, Levels.LevelTypes.All);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "This is info about Cube Thing.\n\n";
            Items.RegisterShopItem(CubeThing, null, null, node, 20);
        }
    }
}