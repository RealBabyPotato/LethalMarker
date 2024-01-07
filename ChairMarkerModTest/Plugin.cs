using BepInEx;
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
            Logger.LogInfo("Patched Cmod");
        }

        private void SetupItems() // this is messy. oh well.
        {
            SetupCubeThing();
            SetupVoodooDoll();
        }

        private void SetupVoodooDoll()
        {
            
        }

        private void SetupCubeThing()
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

            NetworkPrefabs.RegisterNetworkPrefab(CubeThing.spawnPrefab);
            
            Items.RegisterScrap(CubeThing, 1000, Levels.LevelTypes.All);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "This is info about Cube Thing.\n\n";
            Items.RegisterShopItem(CubeThing, null, null, node, 20);
        }
    }
}