﻿using HarmonyLib;
using System.Reflection;

namespace PulsarManager.Injections
{
    public static class HarmonyInjector
    {
        public static void InitializeHarmony()
        {
            var harmony = new Harmony("wiki.pulsar.pml");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
