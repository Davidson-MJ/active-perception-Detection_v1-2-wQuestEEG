using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Passer {

    [CustomEditor(typeof(Spawner),true)]
    public class Spawner_Editor : Editor {


        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (Application.isPlaying && GUILayout.Button("Spawn")) {
                Spawner spawner = (Spawner)target;
                spawner.SpawnObject();
            }
        }
    }

}