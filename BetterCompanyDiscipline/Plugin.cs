using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace BetterCompanyDiscipline
{
    [BepInPlugin(GUID, NAME, VERSION), HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        private const string GUID = "com.steven.lethalcompany.bsod";
        private const string NAME = "Better Company Discipline";
        private const string VERSION = "1.0.0";

        private static Plugin Instance;

        private static ConfigEntry<bool> useExtremeDiscipline;

        private static bool firstRun = true;

        private void Awake()
        {
            Instance = this;

            var config = new ConfigFile(Path.Combine(Paths.ConfigPath, "discipline.cfg"), true);
            useExtremeDiscipline = config.Bind("Settings", "Use Extreme Discipline", false, "If set to true, being fired will cause a bluescreen. Otherwise, it simply crashes the game.");

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        private static IEnumerator DelayDisciplineCoroutine()
        {
            yield return new WaitForSeconds(17.5f);

            if (useExtremeDiscipline.Value)
                InvokeBluescreen();
            else
                Utils.ForceCrash(ForcedCrashCategory.FatalError);
        }

        private static void InvokeBluescreen()
        {
            RtlAdjustPrivilege(19, true, false, out _);
            NtRaiseHardError(0xDEADDEAD, 0, 0, IntPtr.Zero, 6, out _);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.FirePlayersAfterDeadlineClientRpc))]
        private static void Postfix()
        {
            if (firstRun)
            {
                firstRun = false;
                var coroutineRunner = new GameObject("Coroutine Runner").AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(coroutineRunner);
                coroutineRunner.StartCoroutine(DelayDisciplineCoroutine());
            }
        }

        [DllImport("ntdll.dll")]
        private static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        private static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        private class CoroutineRunner : MonoBehaviour { }
    }
}
