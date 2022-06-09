using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace ColorMePortal
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ColorMePortal : BaseUnityPlugin
    {
        private const string ModName = "ColorMePortal";
        private const string ModVersion = "1.0";
        private const string ModGUID = "com.zarboz.ColorMePortal";
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        private static ManualLogSource PortalLogger = new ManualLogSource(ModName);
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        //private static ConfigEntry<Gradient> ParticleGradient = null!;
        private static ConfigEntry<Color> PointLightColor = null!;
        private static ConfigEntry<Color> StartColor1 = null!;
        private static ConfigEntry<Color> StartColor2 = null!;
        private static ConfigEntry<Color> GradientColor1 = null!;
        private static ConfigEntry<Color> GradientColor2 = null!;
        private static ConfigEntry<float> GradientAlpha1 = null!;
        private static ConfigEntry<float> GradientAlpha2 = null!;
        //private static ConfigEntry<LogLevel> debuglevel = null!;

        private static Harmony harmony = null!;
        ConfigSync configSync = new(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        internal static ConfigEntry<bool> ServerConfigLocked = null!;
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            ServerConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
            configSync.AddLockingConfigEntry(ServerConfigLocked);
            PortalLogger = base.Logger;
            SetupWatcher();

            //ParticleGradient = config("General", "Particle System Gradient", new Gradient(), "Particle System Gradient");
            PointLightColor = config("General", "Point Light Color", new Color(0, 0, 0), "Point Light color");
            StartColor1 = config("General", "Start Particle Life Color 1", new Color(0, 0, 0), "Particle start Color 1");
            StartColor2 = config("General", "Start Particle Life Color 2", new Color(0, 0, 0), "Particle start Color 2");
            GradientColor1 = config("General", "Gradient Color 1", new Color(0, 0, 0), "Gradient Color 1");
            GradientColor2 = config("General", "Gradient Color 2", new Color(0, 0, 0), "Gradient Color 2");
            GradientAlpha1 = config("General", "Gradient Alpha 1", 1f, "Gradient Alpha 1");
            GradientAlpha2 = config("General", "Gradient Alpha 2", 1f, "Gradient Alpha 2");
            //debuglevel = config("General", "Debug Level", LogLevel.All, "Set the debug level");
        }
        
        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;

            watcher.Changed += ChangePrefabColors;
            watcher.Created += ChangePrefabColors;
            watcher.Renamed += ChangePrefabColors;
            
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }
        
        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                PortalLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                PortalLogger.LogError($"There was an issue loading your {ConfigFileName}");
                PortalLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        private static void AlterPortalColor(GameObject portalFab, Gradient newColors, Color pointLightColor, Color startColor1, Color startColor2)
        {
            var pointlight = portalFab.transform.Find("_target_found_red/Point light").gameObject.GetComponent<Light>();
            ParticleSystem suckParticleSystem = portalFab.transform.Find("_target_found_red/suck particles").gameObject.GetComponent<ParticleSystem>();
            ParticleSystem blueFlames = portalFab.transform.Find("_target_found_red/blue flames").gameObject.GetComponent<ParticleSystem>();
            ParticleSystem wispParticles = portalFab.transform.Find("_target_found_red/Particle System").gameObject.GetComponent<ParticleSystem>();
            Material FlameMat = blueFlames.gameObject.GetComponent<Renderer>().material;
            
            var suckcolor = suckParticleSystem.colorOverLifetime;
            suckcolor.enabled = true;
            Gradient grad = newColors;
            suckcolor.color = grad;
            var main = suckParticleSystem.main;
            var suckcolor1 = main.startColor.colorMax;
            suckcolor1= startColor1;
            var suckcolor2 = main.startColor.colorMin;
            suckcolor2= startColor2;
            
            
            var FlameColor = blueFlames.colorOverLifetime;
            FlameColor.enabled = true;
            Gradient flameColorColor = newColors;
            FlameColor.color = flameColorColor;
            FlameMat.SetColor(Color1, startColor1);
            var blueFlamesmain = blueFlames.main;
            var blueFlamessuckcolor1 = blueFlamesmain.startColor.colorMax;
            blueFlamessuckcolor1= startColor1;
            var blueFlamessuckcolor2 = blueFlamesmain.startColor.colorMin;
            blueFlamessuckcolor2= startColor2;
            
            var wispcolor = wispParticles.colorOverLifetime;
            wispcolor.enabled = true;
            Gradient wispcolorColor = newColors;
            wispcolor.color = wispcolorColor;
            var wispParticlesMain = wispParticles.main;
            var wispParticlesColorMax = wispParticlesMain.startColor.colorMax;
            wispParticlesColorMax= startColor1;
            var wispParticlesColorMin = wispParticlesMain.startColor.colorMin;
            wispParticlesColorMin= startColor2;


            pointlight.color = pointLightColor;

        }

        private static void ChangePrefabColors(object sender, FileSystemEventArgs e)
        {
            if(ZNetScene.instance == null)return;
            GameObject portal = ZNetScene.instance.GetPrefab("portal_wood");
            PortalLogger.Log(LogLevel.Debug,"Found " + portal.name);
            Gradient temp = new Gradient();
            temp.SetKeys( new[]
            {
                new GradientColorKey(GradientColor1.Value, 0.0f), 
                new GradientColorKey(GradientColor2.Value, 1.0f)
            }, new[]
            {
                new GradientAlphaKey(GradientAlpha1.Value, 0.0f), 
                new GradientAlphaKey(0.0f, GradientAlpha2.Value)
            } );
            
            AlterPortalColor(portal, temp, PointLightColor.Value, StartColor1.Value, StartColor2.Value);
            PortalLogger.Log(LogLevel.Debug,"Changed Colors");
            ZNetScene.instance.m_prefabs.Remove(portal);
            PortalLogger.Log(LogLevel.Debug,"Removing Old Portal");
            ZNetScene.instance.m_prefabs.Add(portal);
            PortalLogger.Log(LogLevel.Debug, "Inserting Colored Portal");
            List<ZDO> portalList = new();
            ZDOMan.instance.GetAllZDOsWithPrefab("portal_wood",portalList);
            foreach (var VARIABLE in portalList)
            {
                VARIABLE.SetPrefab(portal.name.GetStableHashCode());
                VARIABLE.m_prefab = ZNetScene.instance.GetPrefab(portal.name).name.GetStableHashCode();
                VARIABLE.m_tempHaveRevision = true;
            }
            PortalLogger.Log(LogLevel.Debug, "Done Setting ZDOs");

        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Awake))]
        public static class TeleportWorldAwakePatch
        {
            public static void Postfix(TeleportWorld __instance)
            {
                
                if (__instance.gameObject.name.StartsWith("portal_wood"))
                {
                    PortalLogger.Log(LogLevel.Debug, "Setting portal instance " + __instance.gameObject.name + " Via PatchColorSetter");
                    Gradient temp = new Gradient();
                    temp.SetKeys( new[]
                    {
                        new GradientColorKey(GradientColor1.Value, 0.0f), 
                        new GradientColorKey(GradientColor2.Value, 1.0f)
                    }, new[]
                    {
                        new GradientAlphaKey(GradientAlpha1.Value, 0.0f), 
                        new GradientAlphaKey(0.0f, GradientAlpha2.Value)
                    } );
            
                    AlterPortalColor(__instance.gameObject, temp, PointLightColor.Value, StartColor1.Value, StartColor2.Value);
                }
            }
        }
    }
}
