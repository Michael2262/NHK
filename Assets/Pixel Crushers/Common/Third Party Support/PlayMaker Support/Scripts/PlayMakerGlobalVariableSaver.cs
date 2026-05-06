using System;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers
{

    /// <summary>
    /// Saves all global bool, int, float, and string variable values.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Save System/Savers/PlayMaker/PlayMaker Global Variable Saver")]
    public class PlayMakerGlobalVariableSaver : Saver
    {
        [Serializable]
        public class Data
        {
            public List<string> boolNames = new List<string>();
            public List<bool> boolValues = new List<bool>();
            public List<string> intNames = new List<string>();
            public List<int> intValues = new List<int>();
            public List<string> floatNames = new List<string>();
            public List<float> floatValues = new List<float>();
            public List<string> stringNames = new List<string>();
            public List<string> stringValues = new List<string>();
        }

        public override string RecordData()
        {
            var data = new Data();
            foreach (var boolVar in FsmVariables.GlobalVariables.BoolVariables)
            {
                data.boolNames.Add(boolVar.Name);
                data.boolValues.Add(boolVar.Value);
            }
            foreach (var intVar in FsmVariables.GlobalVariables.IntVariables)
            {
                data.intNames.Add(intVar.Name);
                data.intValues.Add(intVar.Value);
            }
            foreach (var floatVar in FsmVariables.GlobalVariables.FloatVariables)
            {
                data.floatNames.Add(floatVar.Name);
                data.floatValues.Add(floatVar.Value);
            }
            foreach (var stringVar in FsmVariables.GlobalVariables.StringVariables)
            {
                data.stringNames.Add(stringVar.Name);
                data.stringValues.Add(stringVar.Value);
            }
            return SaveSystem.Serialize(data);
        }

        public override void ApplyData(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            var data = SaveSystem.Deserialize<Data>(s);
            if (data == null) return;
            for (int i = 0; i < data.boolNames.Count; i++)
            {
                var boolVar = FsmVariables.GlobalVariables.FindFsmBool(data.boolNames[i]);
                if (boolVar != null) boolVar.Value = data.boolValues[i];
            }
            for (int i = 0; i < data.intNames.Count; i++)
            {
                var intVar = FsmVariables.GlobalVariables.FindFsmInt(data.intNames[i]);
                if (intVar != null) intVar.Value = data.intValues[i];
            }
            for (int i = 0; i < data.floatNames.Count; i++)
            {
                var floatVar = FsmVariables.GlobalVariables.FindFsmFloat(data.floatNames[i]);
                if (floatVar != null) floatVar.Value = data.floatValues[i];
            }
            for (int i = 0; i < data.stringNames.Count; i++)
            {
                var stringVar = FsmVariables.GlobalVariables.FindFsmString(data.stringNames[i]);
                if (stringVar != null) stringVar.Value = data.stringValues[i];
            }
        }
    }
}
