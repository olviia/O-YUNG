using Oyung.Actors;
using UnityEngine;

namespace Oyung.Scratch
{
    // CLAUDE: temporary, delete after testing. Play mode: pick an actor,
    // CLAUDE: type a line, press the Inspector button. Logs when it's done.
    public sealed class SayTest : MonoBehaviour
    {
        [SerializeField] private ActorId actor;
        [TextArea]
        [SerializeField] private string text = "Look at our baby! "
            + "What will we name her? I don't know.";

        public void Say()
        {
            var speaking = (ISpeakingActor)actor.Current;
            Debug.Log($"[SayTest] {actor.name} says: {text}");
            speaking.Say(text,
                () => Debug.Log($"[SayTest] {actor.name} is done."));
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SayTest))]
    internal sealed class SayTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (new UnityEditor.EditorGUI.DisabledScope(
                       !Application.isPlaying))
                if (GUILayout.Button("Say"))
                    ((SayTest)target).Say();
        }
    }
#endif
}
