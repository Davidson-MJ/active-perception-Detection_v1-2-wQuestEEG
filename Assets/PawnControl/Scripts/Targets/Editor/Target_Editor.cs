using UnityEditor;
using UnityEngine;

namespace Passer {

    public static class Target_Editor {
        public static Target Inspector(Target target, string name) {
            if (target == null)
                return target;

            EditorGUILayout.BeginHorizontal();
            Transform defaultTargetTransform = null; // target.GetDefaultTarget(target.humanoid);
            Transform targetTransform = target.transform ?? defaultTargetTransform;
            targetTransform = (Transform)EditorGUILayout.ObjectField(name, targetTransform, typeof(Transform), true);

            if (!Application.isPlaying) {
                if (targetTransform == defaultTargetTransform && GUILayout.Button("Show", GUILayout.MaxWidth(60))) {
                    // Call static method CreateTarget on target
                    target = (Target)target.GetType().GetMethod("CreateTarget").Invoke(null, new object[] { target });
                    //} else if (targetTransform != target.transform) {
                    //    target = (HumanoidTarget)target.GetType().GetMethod("SetTarget").Invoke(null, new object[] { target.humanoid, targetTransform });
                }
            }
            EditorGUILayout.EndHorizontal();
            return target;
        }
    }

}