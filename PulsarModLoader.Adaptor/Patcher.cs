﻿using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using PulsarModLoader.Injections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PulsarModLoader.Adaptor
{
    public class Patcher
    {

        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };
        internal static readonly ManualLogSource Log = Logger.CreateLogSource("PML-Adaptor");

        public static void Initialize()
        {
            NoTranspilerNormalization();
            InjectAssemblies("PulsarModLoader");
        }

        public static void InjectAssemblies(string AssemblyName)
        {
            /// Load local .dll Assembly (In BepInEx patches directory)
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == AssemblyName))
            {
                int location = Assembly.GetExecutingAssembly().Location.LastIndexOf('\\');
                int length = $"\\{Assembly.GetExecutingAssembly().GetName().Name}.dll".Length;
                string path = Assembly.GetExecutingAssembly().Location.Remove(location, length) + $"\\{AssemblyName}.dll";
                if (File.Exists(path))
                {
                    Assembly.LoadFrom(path);
                    Log.LogInfo($"Loaded '{AssemblyName}' successfully.");
                }
                else
                {
                    Log.LogWarning($"Failed to load '{AssemblyName}' - not found at expected location!");
                }
            }
            else
            {
                Log.LogInfo($"Failed to load '{AssemblyName}' - assembly already loaded!");
            }
        }

        internal static void NoTranspilerNormalization()
        { // The following code is to clear the ShortToLong map (Dictionary used to convert BR_S into BR) of HarmonyX
            Assembly harmonyAssembly = Assembly.GetAssembly(typeof(HarmonyLib.Harmony));
            Type ilManipulatorType = harmonyAssembly.GetType("HarmonyLib.Internal.Patching.ILManipulator");

            if (ilManipulatorType == null)
            {
                return;
            }

            FieldInfo fieldInfo = ilManipulatorType.GetField("ShortToLongMap", BindingFlags.NonPublic | BindingFlags.Static);

            if (fieldInfo != null)
            {
                var shortToLongMap = fieldInfo.GetValue(null) as Dictionary<global::System.Reflection.Emit.OpCode, global::System.Reflection.Emit.OpCode>;

                if (shortToLongMap != null)
                {
                    // Clear the dictionary
                    shortToLongMap.Clear();
                    Log.LogInfo($"Transpiler ShortToLongMap cleared (Used for Transpiler Normalization).");
                }
                else
                {
                    Log.LogWarning("The ShortToLongMap is null. Transpiler short patches will be converted as standard with HarmonyX.");
                }
            }
            else
            {
                Log.LogWarning("Field ShortToLongMap not found. Transpiler short patches will be converted as standard with HarmonyX.");
            }
        }
        
        public static void Patch(AssemblyDefinition assembly)
        { // The following code is the regular Injector patch. It is temporary and the IsModified is used so that regular injector still runs.
            var modsDirField = typeof(ModManager).GetField("ModsDir", BindingFlags.Public | BindingFlags.Static);
            if (modsDirField != null)
            {
                var modsDir = modsDirField.GetValue(null) as IList<string>;
                modsDir?.Add(Paths.PluginPath);
                Log.LogInfo($"Added {Paths.PluginPath} to the mod directories.");
            }
            else
            {
                Log.LogWarning("PulsarModLoader is outdated and cannot locate other directories!");
            }

            if (IsModified(assembly))
            {
                Log.LogInfo("The assembly is already modified.");
                return;
            }

            PatchMethod(assembly, "PLGlobal", "Start", typeof(LoggingInjections), "LoggingCleanup");
            PatchMethod(assembly, "PLGlobal", "Awake", typeof(HarmonyInjector), "InitializeHarmony");
        }

        internal static bool IsModified(AssemblyDefinition targetAssembly)
        {
            string targetClassName = "PLGlobal";
            string targetMethodName = "Awake";

            // Find the methods involved
            MethodDefinition targetMethod = targetAssembly.MainModule.GetType(targetClassName).Methods.First(m => m.Name == targetMethodName);

            if (targetMethod == null)
            {
                throw new ArgumentNullException("Couldn't find method in target assembly!");
            }

            if (targetMethod.Body.Instructions[0].OpCode == OpCodes.Call)
            {
                return true;
            }
            return false;
        }

        internal static void PatchMethod(AssemblyDefinition targetAssembly, string targetClassName, string targetMethodName, Type sourceClassType, string sourceMethodName)
        {
            Log.LogDebug($"Attempting {sourceClassType.ToString()} injection");

            // Find the methods involved
            MethodDefinition targetMethod = targetAssembly.MainModule.GetType(targetClassName).Methods.First(m => m.Name == targetMethodName);
            MethodReference sourceMethod = targetAssembly.MainModule.ImportReference(sourceClassType.GetMethod(sourceMethodName));

            if (targetMethod == null)
            {
                Log.LogError($"Failed {sourceClassType.ToString()} injection - Couldn't find method in target assembly!");
                return;
            }
            if (sourceMethod == null)
            {
                Log.LogError($"Failed {sourceClassType.ToString()} injection - Couldn't find method in source assembly!");
                return;
            }

            Log.LogDebug("Found relevant methods.  Injecting hook...");

            // Inject source method into front of target method
            ILProcessor targetProcessor = targetMethod.Body.GetILProcessor();

            Instruction oldFirstInstruction = targetMethod.Body.Instructions[0];
            Instruction callToInjectedMethod = targetProcessor.Create(OpCodes.Call, sourceMethod);
            
            targetProcessor.InsertBefore(oldFirstInstruction, callToInjectedMethod);
            Log.LogInfo($"Injected {sourceClassType.ToString()} successfully.");
        }
    }
}
