using Oyung.SharedKernel;
using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Scratch
{
    // CLAUDE: temporary, delete after testing. Play mode: press the
    // CLAUDE: Inspector button; onPress runs; the condition's
    // CLAUDE: Changed fires and the test logs once it is met.
    public sealed class ConditionDrawerTest : MonoBehaviour
    {
        [SerializeReference] private Condition condition;
        [SerializeReference] private Instruction onPress;
        [SerializeField] private ModuleFact<int> pressCounter;

        private bool _listening;

        // CLAUDE: Start, not Awake: CompositionRoot binds facts in Awake.
        private void Start()
        {
            condition.Changed += OnConditionChanged;
            _listening = true;
        }

        private void OnDestroy() => StopListening();

        private void OnConditionChanged()
        {
            Debug.Log($"[Test] {pressCounter.Name} = {pressCounter.Value}");
            if (!condition.IsMet) return;
            Debug.Log("[Test] Condition complete.");
            StopListening();
        }

        private void StopListening()
        {
            if (!_listening) return;
            condition.Changed -= OnConditionChanged;
            _listening = false;
        }

        public void Press() => onPress.Execute();
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ConditionDrawerTest))]
    internal sealed class ConditionDrawerTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (new UnityEditor.EditorGUI.DisabledScope(
                       !Application.isPlaying))
                if (GUILayout.Button("Press"))
                    ((ConditionDrawerTest)target).Press();
        }
    }
#endif
}
