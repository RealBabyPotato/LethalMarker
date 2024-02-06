using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using ChairMarkerModTest.Behaviours;
using JetBrains.Annotations;
using UnityEngine.Assertions;

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
        internal ManualLogSource mls;

        AssetBundle bundle;
        
        void Awake()
        {
            instance = this;



            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lethalmarker");
            bundle = AssetBundle.LoadFromFile(assetDir);

            SetupItems();
            SetupEnemies();

            mls = BepInEx.Logging.Logger.CreateLogSource(GUID);
            mls.LogInfo("Chair Marker mod up and running!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
        }

        private void SetupEnemies()
        {
            SetupNiceGuy();
        }

        private void SetupItems()
        {
            SetupCubeThing();
            SetupGrenade();
            SetupExtendoArm();
        }

        private void SetupNiceGuy()
        {
            EnemyType niceGuyType = bundle.LoadAsset<EnemyType>("Assets/Mod/Nice Guy/Nice Guy.asset");
            Debug.Log(niceGuyType.enemyName); // niceguytype is null
            //var tlTerminalNode = bundle.LoadAsset<TerminalNode>("Assets/Mod/Nice Guy/Bestiary/Nice Guy Tn.asset");
            //var tkTerminalNode = bundle.LoadAsset<TerminalKeyword>("Assets/Mod/Nice Guy/Bestiary/Nice Guy Tk.asset"); 

            //NetworkPrefabs.RegisterNetworkPrefab(niceGuyType.enemyPrefab);
            //LethalLib.Modules.Enemies.RegisterEnemy(niceGuyType, 100, Levels.LevelTypes.All, LethalLib.Modules.Enemies.SpawnType.Outside, tlTerminalNode, tkTerminalNode);
            Debug.Log("--------------------------------------- enemy loiaded ---------------------------");
        }

        private void SetupGrenade()
        {
            Item FragGrenade = bundle.LoadAsset<Item>("Assets/Mod/Frag Grenade/FragGrenade.asset");

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(FragGrenade.spawnPrefab);
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "kill your foes!\n\n";
            Items.RegisterShopItem(FragGrenade, null, null, node, 0);
        }

        private void SetupExtendoArm() // todo: offload these hardcoded things to unity
        {
            Item ExtendoArm = bundle.LoadAsset<Item>("Assets/Mod/Extendo Arm/Extendo Arm.asset");

            ExtendoArm.weight = 1.13f;
            ExtendoArm.itemId = 69699;
            ExtendoArm.canBeGrabbedBeforeGameStart = true;
            ExtendoArm.itemSpawnsOnGround = false;

            ExtendoArmBehaviour script = ExtendoArm.spawnPrefab.AddComponent<ExtendoArmBehaviour>();
            script.itemProperties = ExtendoArm;
            script.grabbable = true;
            script.grabbableToEnemies = true;

            script.piston = ExtendoArm.spawnPrefab.transform.GetChild(2).gameObject;
            script.pistonBlock = ExtendoArm.spawnPrefab.transform.GetChild(2).gameObject.transform.GetChild(0).gameObject;
            script.stopFlag = ExtendoArm.spawnPrefab.transform.GetChild(2).gameObject.transform.GetChild(1).gameObject;

            script.armAudio = ExtendoArm.spawnPrefab.GetComponent<AudioSource>();

            script.instantRetract = bundle.LoadAsset<AudioClip>("Assets/Mod/Extendo Arm/Audio/default.wav");
            script.retract1 = bundle.LoadAsset<AudioClip>("Assets/Mod/Extendo Arm/Audio/fastestin.wav"); // fast
            script.retract2 = bundle.LoadAsset<AudioClip>("Assets/Mod/Extendo Arm/Audio/fastin.wav"); // fast
            script.retract3 = bundle.LoadAsset<AudioClip>("Assets/Mod/Extendo Arm/Audio/slowin.wav");

            script.extend1 = bundle.LoadAsset<AudioClip>("Assets/Mod/Extendo Arm/Audio/longout.wav");
            script.extend2 = bundle.LoadAsset<AudioClip>("Assets/Mod/Extendo Arm/Audio/out.wav");
            script.outIn = bundle.LoadAsset<AudioClip>("Assets/Mod/Extendo Arm/Audio/outin.wav");

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(ExtendoArm.spawnPrefab);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "Useful for siphoning loot!";
            Items.RegisterShopItem(ExtendoArm, null, null, node, 0);
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