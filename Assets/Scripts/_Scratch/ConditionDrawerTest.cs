using System.Reflection;
using Oyung.SharedKernel;
using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Scratch
{
    // CLAUDE: temporary, delete after testing. Play mode: press the
    // CLAUDE: Inspector button; the fact grows; the condition's
    // CLAUDE: Changed fires and the test logs once it is met.
    public sealed class ConditionDrawerTest : MonoBehaviour
    {
        [SerializeReference] private Condition condition;
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

        // CLAUDE: test-only hack: no "add to a fact" Instruction exists
        // CLAUDE: yet, so reach the internal store through reflection.
        public void Press()
        {
            var type = pressCounter.GetType();
            FieldInfo storeField = null;
            for (; type != null && storeField == null; type = type.BaseType)
                storeField = type.GetField("_store",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            var store = storeField?.GetValue(pressCounter);
            if (store == null)
            {
                Debug.LogError("[Test] Fact not bound; are you in Play?");
                return;
            }
            store.GetType()
                .GetMethod("Add", BindingFlags.Instance |
                                  BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(store, new object[] { pressCounter.Name, 1 });
        }
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
