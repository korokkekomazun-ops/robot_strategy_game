using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RobotStrategy.Movement.Editor
{
    public static class TerrainMovementDemoMenu
    {
        [MenuItem("Tools/Robot Strategy/Open Terrain Movement Demo")]
        public static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(9f, 3.5f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            new GameObject("Terrain Movement Demo", typeof(TerrainMovementDemo));
        }
    }
}
