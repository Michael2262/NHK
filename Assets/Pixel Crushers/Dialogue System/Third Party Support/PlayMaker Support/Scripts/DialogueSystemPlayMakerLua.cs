using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    /// <summary>
    /// This component adds Lua functions to work with PlayMaker. 
    /// Add it to your Dialogue Manager.
    /// </summary>
    [AddComponentMenu("Dialogue System/Third Party/PlayMaker/Dialogue System PlayMaker Lua")]
    public class DialogueSystemPlayMakerLua : MonoBehaviour
    {

        protected static bool areFunctionsRegistered = false;

        private bool didIRegisterFunctions = false;

        void OnEnable()
        {
            if (areFunctionsRegistered)
            {
                didIRegisterFunctions = false;
            }
            else
            {
                // Make the functions available to Lua:
                didIRegisterFunctions = true;
                areFunctionsRegistered = true;
                Lua.RegisterFunction(nameof(FSMEvent), this, SymbolExtensions.GetMethodInfo(() => FSMEvent(string.Empty, string.Empty, string.Empty)));
                Lua.RegisterFunction(nameof(GetFsmFloat), this, SymbolExtensions.GetMethodInfo(() => GetFsmFloat(string.Empty)));
                Lua.RegisterFunction(nameof(GetFsmInt), this, SymbolExtensions.GetMethodInfo(() => GetFsmInt(string.Empty)));
                Lua.RegisterFunction(nameof(GetFsmBool), this, SymbolExtensions.GetMethodInfo(() => GetFsmBool(string.Empty)));
                Lua.RegisterFunction(nameof(GetFsmString), this, SymbolExtensions.GetMethodInfo(() => GetFsmString(string.Empty)));
                Lua.RegisterFunction(nameof(SetFsmFloat), this, SymbolExtensions.GetMethodInfo(() => SetFsmFloat(string.Empty, (double)0)));
                Lua.RegisterFunction(nameof(AddFsmFloat), this, SymbolExtensions.GetMethodInfo(() => AddFsmFloat(string.Empty, (double)0)));
                Lua.RegisterFunction(nameof(SubtractFsmFloat), this, SymbolExtensions.GetMethodInfo(() => SubtractFsmFloat(string.Empty, (double)0)));
                Lua.RegisterFunction(nameof(SetFsmInt), this, SymbolExtensions.GetMethodInfo(() => SetFsmInt(string.Empty, (double)0)));
                Lua.RegisterFunction(nameof(AddFsmInt), this, SymbolExtensions.GetMethodInfo(() => AddFsmInt(string.Empty, (double)0)));
                Lua.RegisterFunction(nameof(SubtractFsmInt), this, SymbolExtensions.GetMethodInfo(() => SubtractFsmInt(string.Empty, (double)0)));
                Lua.RegisterFunction(nameof(SetFsmBool), this, SymbolExtensions.GetMethodInfo(() => SetFsmBool(string.Empty, false)));
                Lua.RegisterFunction(nameof(SetFsmString), this, SymbolExtensions.GetMethodInfo(() => SetFsmString(string.Empty, string.Empty)));
            }
        }

        void OnDisable()
        {
            if (didIRegisterFunctions)
            {
                // Remove the functions from Lua:
                didIRegisterFunctions = false;
                areFunctionsRegistered = false;
                Lua.UnregisterFunction("FSMEvent");
                Lua.UnregisterFunction("GetFsmFloat");
                Lua.UnregisterFunction("GetFsmInt");
                Lua.UnregisterFunction("GetFsmBool");
                Lua.UnregisterFunction("GetFsmString");
                Lua.UnregisterFunction("SetFsmFloat");
                Lua.UnregisterFunction("SetFsmInt");
                Lua.UnregisterFunction("SetFsmBool");
                Lua.UnregisterFunction("SetFsmString");
            }
        }

        public void FSMEvent(string eventName, string objectName, string fsmName)
        {
            bool all = string.Equals(objectName, "all", StringComparison.OrdinalIgnoreCase);
            var subject = all ? null : GameObject.Find(objectName);
            if (all)
            {
                DialogueSystemPlayMakerTools.SendEventToAllFSMs(eventName, fsmName);
            }
            else if (subject != null)
            {
                DialogueSystemPlayMakerTools.SendEventToFSMs(subject.transform, eventName, fsmName);
            }
        }

        public double GetFsmFloat(string name)
        {
            return DialogueSystemPlayMakerTools.GetFsmFloat(name);
        }

        public double GetFsmInt(string name)
        {
            return DialogueSystemPlayMakerTools.GetFsmInt(name);
        }

        public bool GetFsmBool(string name)
        {
            return DialogueSystemPlayMakerTools.GetFsmBool(name);
        }

        public string GetFsmString(string name)
        {
            return DialogueSystemPlayMakerTools.GetFsmString(name);
        }

        public void SetFsmFloat(string name, double value)
        {
            DialogueSystemPlayMakerTools.SetFsmFloat(name, (float) value);
        }

        public void AddFsmFloat(string name, double value)
        {
            DialogueSystemPlayMakerTools.AddFsmFloat(name, (float)value);
        }

        public void SubtractFsmFloat(string name, double value)
        {
            DialogueSystemPlayMakerTools.SubtractFsmFloat(name, (float)value);
        }

        public void SetFsmInt(string name, double value)
        {
            DialogueSystemPlayMakerTools.SetFsmInt(name, (int)value);
        }

        public void AddFsmInt(string name, double value)
        {
            DialogueSystemPlayMakerTools.AddFsmInt(name, (int)value);
        }

        public void SubtractFsmInt(string name, double value)
        {
            DialogueSystemPlayMakerTools.SubtractFsmInt(name, (int)value);
        }

        public void SetFsmBool(string name, bool value)
        {
            DialogueSystemPlayMakerTools.SetFsmBool(name, value);
        }

        public void SetFsmString(string name, string value)
        {
            DialogueSystemPlayMakerTools.SetFsmString(name, value);
        }

    }
}