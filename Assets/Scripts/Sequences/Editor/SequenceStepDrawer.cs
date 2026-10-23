using Oyung.Sequences.Unity;
using Oyung.SharedKernel.Editor;
using UnityEditor;

namespace Oyung.Sequences.Editor
{
    /// <summary>Type dropdown for a sequence's steps, like Condition and
    /// Instruction get.</summary>
    [CustomPropertyDrawer(typeof(SequenceStep), true)]
    internal sealed class SequenceStepDrawer : TypePickerDrawer { }
}
