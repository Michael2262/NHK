using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    /// <summary>
    /// Utility functions for Dialogue System PlayMaker integration.
    /// </summary>
    public static class DialogueSystemPlayMakerTools
    {

        /// <summary>
        /// Sends an event to all GameObjects in the scene.
        /// </summary>
        /// <param name="eventName">The event to send.</param>
        /// <param name="fsmName">If not empty, the FSM to receive the event. Otherwise all FSMs receive it.</param>
        public static void SendEventToAllFSMs(string eventName, string fsmName)
        {
            GameObject[] gameObjects = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
            for (int i = 0; i < gameObjects.Length; i++)
            {
                SendEventToFSMs(gameObjects[i].transform, eventName, fsmName);
            }
        }

        /// <summary>
        /// Sends an event to a GameObject.
        /// </summary>
        /// <param name="subject">The </param>
        /// <param name="eventName">The event to send.</param>
        /// <param name="fsmName">If not empty, the FSM to receive the event. Otherwise all FSMs receive it.</param>
        public static void SendEventToFSMs(Transform subject, string eventName, string fsmName)
        {
            if (subject == null) return;
            var fsms = subject.GetComponents<PlayMakerFSM>();
            for (int i = 0; i < fsms.Length; i++)
            {
                var fsm = fsms[i];
                if (string.IsNullOrEmpty(fsmName) || string.Equals(fsmName, fsm.FsmName))
                {
                    fsm.SendEvent(eventName);
                }
            }
        }

        public static PlayMakerFSM GetFSM(GameObject subject, string fsmName)
        {
            if (subject != null)
            {
                var fsms = subject.GetComponents<PlayMakerFSM>();
                for (int i = 0; i < fsms.Length; i++)
                {
                    var fsm = fsms[i];
                    if (fsm != null && (string.IsNullOrEmpty(fsmName) || string.Equals(fsmName, fsm.FsmName)))
                    {
                        return fsms[i];
                    }
                }
            }
            if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Can't find FSM named '" + fsmName + "'.");
            return null;
        }

        public static float GetFsmFloat(string name)
        {
            return HutongGames.PlayMaker.FsmVariables.GlobalVariables.GetFsmFloat(name).Value;
        }

        public static int GetFsmInt(string name)
        {
            return HutongGames.PlayMaker.FsmVariables.GlobalVariables.GetFsmInt(name).Value;
        }

        public static bool GetFsmBool(string name)
        {
            return HutongGames.PlayMaker.FsmVariables.GlobalVariables.GetFsmBool(name).Value;
        }

        public static string GetFsmString(string name)
        {
            return HutongGames.PlayMaker.FsmVariables.GlobalVariables.GetFsmString(name).Value;
        }

        public static void SetFsmFloat(string name, float value)
        {
            var var = HutongGames.PlayMaker.FsmVariables.GlobalVariables.FindFsmFloat(name);
            if (var == null)
            {
                Debug.LogWarning("Dialogue System: Can't find global variable named '" + name + "'.");
            }
            else
            {
                var.Value = value;
            }
        }

        public static void AddFsmFloat(string name, float value)
        {
            SetFsmFloat(name, GetFsmFloat(name) + value);
        }

        public static void SubtractFsmFloat(string name, float value)
        {
            SetFsmFloat(name, GetFsmFloat(name) - value);
        }

        public static void SetFsmInt(string name, int value)
        {
            var var = HutongGames.PlayMaker.FsmVariables.GlobalVariables.FindFsmInt(name);
            if (var == null)
            {
                Debug.LogWarning("Dialogue System: Can't find global variable named '" + name + "'.");
            }
            else
            {
                var.Value = value;
            }
        }

        public static void AddFsmInt(string name, int value)
        {
            SetFsmInt(name, GetFsmInt(name) + value);
        }

        public static void SubtractFsmInt(string name, int value)
        {
            SetFsmInt(name, GetFsmInt(name) - value);
        }

        public static void SetFsmBool(string name, bool value)
        {
            var var = HutongGames.PlayMaker.FsmVariables.GlobalVariables.FindFsmBool(name);
            if (var == null)
            {
                Debug.LogWarning("Dialogue System: Can't find global variable named '" + name + "'.");
            }
            else
            {
                var.Value = value;
            }
        }

        public static void SetFsmString(string name, string value)
        {
            var var = HutongGames.PlayMaker.FsmVariables.GlobalVariables.FindFsmString(name);
            if (var == null)
            {
                Debug.LogWarning("Dialogue System: Can't find global variable named '" + name + "'.");
            }
            else
            {
                var.Value = value;
            }
        }

        public static Vector3 StringToVector3(string s)
        {
            var fields = !string.IsNullOrEmpty(s) ? s.Split(':') : new string[0];
            var x = (fields.Length >= 1) ? Tools.StringToFloat(fields[0]) : 0;
            var y = (fields.Length >= 2) ? Tools.StringToFloat(fields[1]) : 0;
            var z = (fields.Length >= 3) ? Tools.StringToFloat(fields[2]) : 0;
            return new Vector3(x, y, z);
        }

        public static string Vector3ToString(Vector3 v)
        {
            return v.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + 
                v.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + 
                v.z.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static Quaternion StringToQuaternion(string s)
        {
            var fields = !string.IsNullOrEmpty(s) ? s.Split(':') : new string[0];
            var x = (fields.Length >= 1) ? Tools.StringToFloat(fields[0]) : 0;
            var y = (fields.Length >= 2) ? Tools.StringToFloat(fields[1]) : 0;
            var z = (fields.Length >= 3) ? Tools.StringToFloat(fields[2]) : 0;
            var w = (fields.Length >= 4) ? Tools.StringToFloat(fields[3]) : 0;
            return new Quaternion(x, y, z, w);
        }

        public static string QuaternionToString(Quaternion q)
        {
            return q.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + 
                q.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + 
                q.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + 
                q.w.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static GameObject StringToGameObject(string s, bool searchSceneObjects = true, bool searchPrefabs = false)
        {
            return FindOrLoadGameObject(s, searchSceneObjects, searchPrefabs);
        }

        public static object[] StringToArray(string s, VariableType variableType, bool searchSceneObjects = true, bool searchPrefabs = false)
        {
            var list = new List<object>();
            var isStringEmpty = string.IsNullOrEmpty(s) || string.Equals(s, "nil");
            var fields = !isStringEmpty ? s.Split(new string[] { "%;%" }, System.StringSplitOptions.None) : new string[0];
            foreach (var field in fields)
            {
                object value = field;
                switch (variableType)
                {
                    case VariableType.Bool:
                        value = Tools.StringToBool(field);
                        break;
                    case VariableType.Int:
                        value = Tools.StringToInt(field);
                        break;
                    case VariableType.Float:
                        value = Tools.StringToFloat(field);
                        break;
                    case VariableType.Vector3:
                        value = StringToVector3(field);
                        break;
                    case VariableType.Quaternion:
                        value = StringToQuaternion(field);
                        break;
                    case VariableType.GameObject:
                        value = StringToGameObject(field);
                        break;
                }
                list.Add(value);
            }
            return list.ToArray();
        }

        public static string ArrayToString(object[] values)
        {
            var s = string.Empty;
            if (values != null)
            {
                var first = true;
                for (int i = 0; i < values.Length; i++)
                {
                    if (!first) s += "%;%";
                    first = false;
                    var value = values[i];
                    var stringRepresentation = string.Empty;
                    if (value != null)
                    {
                        var type = value.GetType();
                        if (type == typeof(Vector3))
                        {
                            stringRepresentation = Vector3ToString((Vector3)value);
                        }
                        else if (type == typeof(Quaternion))
                        {
                            stringRepresentation = QuaternionToString((Quaternion)value);
                        }
                        else if (type == typeof(GameObject))
                        {
                            stringRepresentation = ((GameObject)value).name;
                        }
                        else if (type == typeof(float) || type == typeof(double))
                        {
                            stringRepresentation = ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            stringRepresentation = value.ToString();
                        }
                    }
                    s += stringRepresentation;
                }
            }
            return s;
        }

        public static GameObject FindOrLoadGameObject(string gameObjectName, bool searchSceneObjects, bool searchPrefabs)
        {
            if ((string.IsNullOrEmpty(gameObjectName) || string.Equals(gameObjectName, "null-object"))) return null;
            GameObject go = null;
            if (searchSceneObjects) go = Tools.GameObjectHardFind(gameObjectName);
            if (go != null) return go;
            return searchPrefabs ? (DialogueManager.LoadAsset(gameObjectName, typeof(GameObject)) as GameObject) : null;

        }

    }

}
