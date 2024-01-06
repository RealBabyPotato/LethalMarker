using BepInEx;
using ChairMarkerModTest.Behaviours;
using HarmonyLib;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ChairMarkerModTest
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        const string GUID = "jaden.chairMarkerMod";
        const string NAME = "Chair Marker";
        const string VERSION = "0.0.1";

        public static Plugin instance;
        
        void Awake()
        {
            instance = this;

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "itemm");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);

            Item cubeThing = bundle.LoadAsset<Item>("Assets/Mod/Cube Thing.asset");

            /*HurtOnUse hurtOnUse = cubeThing.spawnPrefab.AddComponent<HurtOnUse>();
            hurtOnUse.grabbable = true;
            hurtOnUse.grabbableToEnemies = true;
            hurtOnUse.itemProperties = cubeThing;
            //cubeThing.spawnPrefab.AddComponent<HurtOnUse>();*/

            NetworkPrefabs.RegisterNetworkPrefab(cubeThing.spawnPrefab);
            Utilities.FixMixerGroups(cubeThing.spawnPrefab);
            Items.RegisterScrap(cubeThing, 1000, Levels.LevelTypes.All);
    
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "This is info about Cube Thing.\n\n";
            Items.RegisterShopItem(cubeThing, null, null, node, 0);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
            Logger.LogInfo("Patched Cmod");
        }
    }
}