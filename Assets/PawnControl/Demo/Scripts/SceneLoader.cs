#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;


[InitializeOnLoad]
public class SceneLoader {
    static SceneLoader() {
        EditorSceneManager.sceneOpened += SceneOpened;
    }

    private static void SceneOpened(Scene scene, OpenSceneMode mode) {
        if ((scene.name == "ObjectTable Pawn" ||
            scene.name == "ObjectTable Pawn VR" ||
            scene.name == "ObjectTable Humanoid") &&
            EditorSceneManager.loadedSceneCount == 1) {

            //Debug.Log("Additive opening ObjectTable scene");
            EditorSceneManager.OpenScene("Assets/PawnControl/Demo/Environments/ObjectTable.unity", OpenSceneMode.Additive);
        }
        else if ((scene.name == "ShootingRange Pawn" ||
            scene.name == "ShootingRange Pawn VR" ||
            scene.name == "ShootingRange Humanoid") &&
            EditorSceneManager.loadedSceneCount == 1) {

            //Debug.Log("Additive opening ShootingRange scene");
            EditorSceneManager.OpenScene("Assets/PawnControl/Demo/Environments/ShootingRange.unity", OpenSceneMode.Additive);
        }

    }
}
#endif
