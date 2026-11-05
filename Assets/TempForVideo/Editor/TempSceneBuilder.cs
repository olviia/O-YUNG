using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TempForVideo.Editor
{
    // Builds the throwaway video scene: ground, small stairs to a
    // platform, and a red player cube with a third-person camera.
    // Primitives pick up the active pipeline's default material.
    static class TempSceneBuilder
    {
        const string Folder = "Assets/TempForVideo";

        [MenuItem("Tools/TempForVideo/Build Scene")]
        static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Box("Ground", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 20f));

            // 6 steps, 0.2 high and 0.4 deep each, then a 1.2 high platform.
            const int steps = 6;
            const float rise = 0.2f, run = 0.4f, width = 2f, startZ = 3f;
            for (int i = 0; i < steps; i++)
            {
                float h = (i + 1) * rise;
                Box($"Step{i + 1}", new Vector3(0f, h / 2f, startZ + i * run),
                    new Vector3(width, h, run));
            }
            float top = steps * rise;
            float edge = startZ + (steps - 0.5f) * run;
            Box("Platform", new Vector3(0f, top / 2f, edge + 1.5f),
                new Vector3(width * 2f, top, 3f));

            var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            Object.DestroyImmediate(player.GetComponent<BoxCollider>());
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1f;
            cc.radius = 0.5f;
            cc.stepOffset = 0.3f;
            player.GetComponent<Renderer>().sharedMaterial =
                RedMaterial(player.GetComponent<Renderer>().sharedMaterial);

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, 0.5f, 0f);
            pivot.localRotation = Quaternion.Euler(15f, 0f, 0f);

            var cam = Camera.main.transform;
            cam.SetParent(pivot, false);
            cam.localPosition = new Vector3(0f, 0.5f, -5f);
            cam.localRotation = Quaternion.identity;

            player.AddComponent<CubeController>();

            EditorSceneManager.SaveScene(scene, $"{Folder}/TempForVideo.unity");
        }

        static void Box(string name, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
        }

        static Material RedMaterial(Material source)
        {
            string path = $"{Folder}/Red.mat";
            AssetDatabase.DeleteAsset(path);
            var red = new Material(source) { color = Color.red };
            if (red.HasProperty("_BaseColor")) red.SetColor("_BaseColor", Color.red);
            AssetDatabase.CreateAsset(red, path);
            return red;
        }
    }
}
