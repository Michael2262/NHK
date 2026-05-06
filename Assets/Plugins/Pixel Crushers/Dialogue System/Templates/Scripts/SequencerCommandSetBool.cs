using UnityEngine;
using PixelCrushers.DialogueSystem;
using System.Reflection;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    
    public class SequencerCommandSetBool : SequencerCommand
    {
        public void Awake()
        {
            // 1. 拿到 Subject
            Transform subjectTransform = GetSubject(0);
            if (subjectTransform == null)
            {
                Debug.LogWarning("Sequencer: SetBool command requires a subject. " +
                                 "Use <ActorName>SetBool(ComponentName,FieldName,true/false)@time");
                Stop();
                return;
            }
            var go = subjectTransform.gameObject;

            // 2. 解析??
            string componentName = GetParameter(0);      // 要找的?本?名，不?命名空?
            string boolName = GetParameter(1);      // 布?字段或?性名
            bool setValue = GetParameterAsBool(2); // 要?成 true/false

            // 3. 在? GameObject 上找?本
            Component targetComp = null;
            foreach (var comp in go.GetComponents<MonoBehaviour>())
            {
                if (comp.GetType().Name == componentName)
                {
                    targetComp = comp;
                    break;
                }
            }
            if (targetComp == null)
            {
                Debug.LogWarning($"Sequencer: Could not find component '{componentName}' on '{go.name}'.");
                Stop();
                return;
            }

            var type = targetComp.GetType();
            // 4. 先??找字段
            var field = type.GetField(boolName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(targetComp, setValue);
                Stop();
                return;
            }
            // 5. 再??找?性
            var prop = type.GetProperty(boolName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite)
            {
                prop.SetValue(targetComp, setValue, null);
                Stop();
                return;
            }

            Debug.LogWarning($"Sequencer: Component '{componentName}' has no bool field or writable bool property named '{boolName}'.");
            Stop();
        }
    }
}
