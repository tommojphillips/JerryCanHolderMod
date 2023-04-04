using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using System;
using System.IO;
using TommoJProductions.ModApi;
using TommoJProductions.ModApi.Database;
using TommoJProductions.ModApi.Attachable;
using TommoJProductions.ModApi.Attachable.CallBacks;
using UnityEngine;
using GUI = UnityEngine.GUILayout;
namespace TommoJProductions.JerryCanHolderMod
{
    public class JerryCanHolder : Mod
    {
        // Project start date, 12.06.2022

        #region classes

        public class JerryCanHolderSaveData
        {
            // Written, 12.06.2022

            public PartSaveInfo holderPart = new PartSaveInfo() { position = new Vector3(-9.7f, 0, 6.2f) };
            public PartSaveInfo dieselJerry = new PartSaveInfo();
            public PartSaveInfo gasJerry = new PartSaveInfo();
        }

        #endregion

        #region Mod Properties

        public override string ID => "JerryHolder"; //Your mod ID (unique)
        public override string Name => "Jerry Holder"; //You mod name
        public override string Author => "tommojphillips"; //Your Username
        public override string Version => VersionInfo.version; //Version
        public override string Description => $"Adds a jerry can holder part to the game. attaches to satsuma. Latest Release: {VersionInfo.lastestRelease}\nComplied With: ModApi v{ModApi.VersionInfo.version}."; //Short description of your mod
        public override bool UseAssetsFolder => true;

        #endregion

        #region Fields

        private GameObject jerryCanHolder;
        private GameObject jerryCanHolderPrefab;

        private Rigidbody bootlidRigidbody;

        private Part holderPart;
        private Trigger triggerCan;
        private Part gasJerryPart;
        private Part dieselJerryPart;
        private Trigger triggerHolderSatsuma;
        private Trigger triggerHolderBootLid;

        private FsmBool rearSeatInstalled;
        private FsmGameObject rearSeatTrigger;
        private FsmState oh;

        private JerryCanHolderSaveData loadedSaveData;
        
        private readonly string saveFileName = "jerryCanSaveData.txt";
        private readonly string assetBundleFileName = "jerrycanholder.unity3d";

        private float jerryHolderMass;

        private Vector3 jerryHolder_rearSeatInstalled = new Vector3(-0.5099998f, 0.25f, -0.5899997f);
        private Vector3 jerryHolder_rearSeatNotInstalled = new Vector3(-0.5099998f, 0.099f, -0.5899997f);
        private Vector3 gasJerryScale;
        private Vector3 dieselJerryScale;

        #endregion

        #region Mod Functions

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, load);
            SetupFunction(Setup.OnNewGame, newGame);
            SetupFunction(Setup.OnSave, save);
        }

        private void newGame()
        {
            // Written, 12.06.2022

            File.Delete(Path.Combine(ModLoader.GetModSettingsFolder(this), saveFileName));
        }
        private void save()
        {
            // Written, 12.06.2022

            this.saveData(new JerryCanHolderSaveData() 
            {
                dieselJerry = dieselJerryPart.getSaveInfo(),
                gasJerry = gasJerryPart.getSaveInfo(),
                holderPart = holderPart.getSaveInfo()
            }, saveFileName);
        }
        private void load()
        {
            // Written, 12.06.2022

            if (tryLoadAssets()) 
            {
                GameObject satsuma = Database.databaseVehicles.satsuma;
                GameObject databaseBody = GameObject.Find("Database/DatabaseBody");
                GameObject bootLid = databaseBody.transform.FindChild("Bootlid").GetPlayMaker("Data").FsmVariables.FindFsmGameObject("ThisPart").Value;
                PlayMakerFSM items = GameObject.Find("ITEMS").GetPlayMaker("SaveItems");
                PlayMakerFSM rearSeatData = databaseBody.transform.FindChild("Seat_Rear").GetPlayMaker("Data");
                rearSeatInstalled = rearSeatData.FsmVariables.FindFsmBool("Installed");
                rearSeatTrigger = rearSeatData.FsmVariables.FindFsmGameObject("Trigger");
                bootlidRigidbody = bootLid.GetComponent<Rigidbody>();
                PartSettings jerryHolderSettings = new PartSettings()
                {
                    setPhysicsMaterialOnInitialisePart = true,
                    assembleType = AssembleType.joint,                    
                    tightnessThreshold = 0.4f,                    
                    assemblyTypeJointSettings = new AssemblyTypeJointSettings()
                    {
                        breakForce = 3000,
                        boltTightnessEffectsBreakforce = true,
                        breakForceMin = 500,
                        installPointRigidbodies = new Rigidbody[2] { satsuma.GetComponent<Rigidbody>(), bootlidRigidbody }
                    }
                };
                PartSettings jerryPartSettings = new PartSettings()
                { 
                    setPositionRotationOnInitialisePart = false,
                    installEitherDirection = true 
                };

                FsmState isg = items.GetState("Save game");
                isg.prependNewAction(preFixTransform);
                isg.appendNewAction(postFixTransform);
                this.loadOrCreateData(out loadedSaveData, saveFileName);
                // Holder part
                jerryCanHolder = UnityEngine.Object.Instantiate(jerryCanHolderPrefab);
                holderPart = jerryCanHolder.AddComponent<Part>();
                triggerHolderSatsuma = new Trigger("JerryHolderSatsuma", satsuma, triggerPosition: jerryHolder_rearSeatInstalled);
                triggerHolderSatsuma.onPartDisassembledFromTrigger += jerryHolderPartOnAssemble;
                triggerHolderSatsuma.onPartDisassembledFromTrigger += jerryHolderPart_enableRearSeatTrigger;
                triggerHolderBootLid = new Trigger("JerryHolderBootLid", bootLid, new Vector3(0.3f, -0.44f, -0.14f), new Vector3(357, 90, 100));

                BoltSettings bootLidBoltSettings = new BoltSettings()
                {
                    boltSize = BoltSize._10mm,
                    boltType = BoltType.longBolt,
                    posDirection = Vector3.right,
                    posStep = 0.0055f,
                    addNut = true,
                    addNutSettings = new AddNutSettings()
                    {
                        nutSize = BoltSize._9mm,
                        nutOffset = 0.026f
                    }, 
                    parentBoltToTrigger = true,
                    parentBoltToTriggerIndex = 1, 
                };
                BoltSettings bodyBoltSettings = new BoltSettings()
                {
                    boltSize = BoltSize._9mm,
                    boltType = BoltType.longBolt,
                    posDirection = Vector3.right,
                    posStep = 0.0055f,
                    parentBoltToTrigger = true,
                    parentBoltToTriggerIndex = 0,
                };
                Vector3 boltRot = new Vector3(0, 270, 0);
                Bolt[] bolts = new Bolt[6] 
                { 
                    new Bolt(bootLidBoltSettings, new Vector3(0.0527f, -0.05f, 0.1f), boltRot),
                    new Bolt(bootLidBoltSettings, new Vector3(0.0527f, -0.05f, -0.1f), boltRot),
                    new Bolt(bootLidBoltSettings, new Vector3(0.0527f, -0.2f, 0.1f), boltRot, 0.024f),
                    new Bolt(bootLidBoltSettings, new Vector3(0.0527f, -0.2f, -0.1f), boltRot, 0.036f),

                    new Bolt(bodyBoltSettings, new Vector3(0.0527f, -0.05f, 0.1f), boltRot),
                    new Bolt(bodyBoltSettings, new Vector3(0.0527f, -0.05f, -0.1f), boltRot),
                };
                holderPart.initPart(loadedSaveData.holderPart, jerryHolderSettings, bolts, triggerHolderSatsuma, triggerHolderBootLid);
                // jerry parts
                gasJerryPart = items.FsmVariables.FindFsmGameObject("Gasoline").Value.AddComponent<Part>();
                dieselJerryPart = items.FsmVariables.FindFsmGameObject("Diesel").Value.AddComponent<Part>();
                triggerCan = new Trigger("JerryCanTrigger", jerryCanHolder, triggerEuler: new Vector3(-90, 0, 180));
                triggerCan.onPartAssembledToTrigger += updateJerryHolderMass;
                triggerCan.onPartDisassembledFromTrigger += updateJerryHolderMass;
                gasJerryPart.initPart(loadedSaveData.gasJerry, jerryPartSettings, triggerCan);
                dieselJerryPart.initPart(loadedSaveData.dieselJerry, jerryPartSettings, triggerCan);
                triggerCan.onPartDisassembledFromTrigger += updateHolderMass_jerryCanTriggerExitPre;
                triggerCan.triggerCallback.onTriggerExit += updateHolderMass_jerryCanTriggerExitPost;

                // inject boot open action
                PlayMakerFSM p = bootLid.transform.FindChild("Handles").gameObject.GetPlayMaker("Use");
                oh = p.GetState("Open hood");

                oh.insertNewAction(calTorqueTransition, 3, CallbackTypeEnum.onFixedUpdate, true);
                oh.RemoveAction(4); // remove addTorque action
            }
        }

        #endregion

        #region Methods

        private bool tryLoadAssets()
        {
            // Written, 12.06.2022

            string path = Path.Combine(ModLoader.GetModAssetsFolder(this), assetBundleFileName);
            if (File.Exists(path))
            {
                ModConsole.Print($"{Name} asset bundle found");
                AssetBundle ab = LoadAssets.LoadBundle(this, assetBundleFileName);
                jerryCanHolderPrefab = ab.LoadAsset($"{ID}.prefab") as GameObject;
                ab.Unload(false);
                return true;
            }
            ModConsole.Error($"[{ID}] - please install the mod correctly... file: '{path}' doesn't exist");
            return false;
        }

        #endregion

        #region Events

        private void calTorqueTransition() 
        {
            // Written, 14.06.2022

            Vector3 d = Vector3.forward;
            Vector3 p = bootlidRigidbody.transform.localPosition;
            Vector3 v = bootlidRigidbody.velocity;
            float mass = 6.2f;
            if (holderPart.installed)
            {
                mass += jerryHolderMass;

                if (triggerHolderBootLid.triggerCallback.part)
                {
                    mass += triggerHolderBootLid.triggerCallback.part.cachedRigidBody.mass;
                }
            }
            Vector3 force = (d - p - v * Time.fixedDeltaTime) / Time.fixedDeltaTime;

            bootlidRigidbody.AddRelativeForce(mass / 2 * force);
        }
        private void updateJerryHolderMass(Trigger t) 
        {
            holderPart.cachedRigidBody.mass = holderPart.cachedMass + (t.triggerCallback.part?.cachedMass ?? 0);
        }
        private void jerryHolderPart_enableRearSeatTrigger(Trigger t)
        {
            // Written, 12.06.2022

            rearSeatTrigger.Value.SetActive(true);
        }
        private void jerryHolderPartOnAssemble(Trigger t)
        {
            // Written, 12.06.2022

            if (rearSeatInstalled.Value)
            {
                t.triggerGameObject.transform.localPosition = jerryHolder_rearSeatInstalled;
            }
            else
            {
                t.triggerGameObject.transform.localPosition = jerryHolder_rearSeatNotInstalled;
                rearSeatTrigger.Value.SetActive(false);
            }
        }
        private void preFixTransform()
        {
            // Written, 12.06.2022

            if (gasJerryPart.installed)
            {
                gasJerryScale = gasJerryPart.transform.localScale;
                gasJerryPart.transform.localScale = Vector3.one;
            }
            if (dieselJerryPart.installed)
            {
                dieselJerryScale = dieselJerryPart.transform.localScale;
                dieselJerryPart.transform.localScale = Vector3.one;
            }
        }
        private void postFixTransform()
        {
            // Written, 13.06.2022

            if (gasJerryPart.installed)
            {
                gasJerryPart.transform.localScale = gasJerryScale;
            }
            if (dieselJerryPart.installed)
            {
                dieselJerryPart.transform.localScale = dieselJerryScale;
            }
        }
        private void updateHolderMass_jerryCanTriggerExitPost(Part p, TriggerCallback callback)
        {
            // Written, 18.06.2022

            jerryHolderMass = 0;
        }
        private void updateHolderMass_jerryCanTriggerExitPre(Trigger t)
        {
            // Written, 18.06.2022

            if (t.triggerCallback.part)
                jerryHolderMass = t.triggerCallback.part.cachedMass;
        }

        #endregion
    }
}
