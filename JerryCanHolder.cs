using MSCLoader;
using UnityEngine;

namespace JerryCanHolder
{
    public class JerryCanHolder : Mod
    {
        public override string ID => "JerryCanHolder"; //Your mod ID (unique)
        public override string Name => "Jerry Can Holder"; //You mod name
        public override string Author => "tommojphillips"; //Your Username
        public override string Version => VersionInfo.version; //Version
        public override string Description => $"Adds a jerry can holder part to the game. attaches to satsuma. uses ModApi {VersionInfo.lastestRelease}"; //Short description of your mod
        public override bool UseAssetsFolder => true;

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
        }


        private void Mod_OnLoad()
        {
            // Called once, when mod is loading after game is fully loaded
        }
    }
}
