using UnityEngine;

namespace Oyung.Save.Unity
{
    /// <summary>
    /// Dev tool: pick a test point here and Play starts from it. Empty
    /// starts a new game. Works in the editor only.
    /// </summary>
    public sealed class TestStart : MonoBehaviour
    {
        [Tooltip("A save file from Assets/Content/TestPoints; type save_ "
                 + "in the picker to find them.")]
        [SerializeField] private TextAsset testPoint;

        internal TextAsset TestPoint => testPoint;
    }
}
