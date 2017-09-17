﻿using Harmony;
using NitroxPatcher.Patches;
using NitroxReloader;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NitroxPatcher
{
    public static class Main
    {
        public static void Execute()
        {
            Console.WriteLine("[NITROX] Patching Subnautica...");

            // Enabling this creates a log file on your desktop (why there?), showing the emitted IL instructions.
            HarmonyInstance.DEBUG = false;

            HarmonyInstance harmony = HarmonyInstance.Create("com.nitroxmod.harmony");

            string serverNameSpace = "NitroxPatcher.Patches.Server";
            bool serverPatching = (Array.IndexOf(Environment.GetCommandLineArgs(), "-server") >= 0);

            Console.WriteLine("[NITROX] Applying " + ((serverPatching) ? "server" : "client") + " patches");

            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(p => typeof(NitroxPatch).IsAssignableFrom(p) &&
                            p.IsClass &&
                            !p.IsAbstract &&
                            p.Namespace != serverNameSpace ^ serverPatching
                      )
                .Select(Activator.CreateInstance)
                .Cast<NitroxPatch>()
                .ToList()
                .ForEach(patch =>
                {
                    Console.WriteLine("[NITROX] Applying " + patch.GetType());
                    patch.Patch(harmony);
                });

            Console.WriteLine("[NITROX] Completed patching using " + Assembly.GetExecutingAssembly().FullName);

            InitializeReloader(serverPatching);

            DevConsole.disableConsole = false;
        }

        [Conditional("DEBUG")]
        private static void InitializeReloader(bool serverPatching)
        {
            // Whitelist needs to be split, as both game instances load all four libraries
            // (because this patcher references both server and client, so no matter what instance we are on,
            //  AppDomain.CurrentDomain.GetAssemblies() returns both).
            if (serverPatching)
            {
                Reloader.Initialize("NitroxModel.dll", "NitroxPatcher.dll", "NitroxServer.dll");
            }
            else
            {
                Reloader.Initialize("NitroxModel.dll", "NitroxPatcher.dll", "NitroxClient.dll");
            }
        }
    }
}
