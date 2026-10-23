using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Sequences.Unity
{
    // CLAUDE: every sequence asset. Used by the installer (bind, start).
    [CreateAssetMenu(menuName = "Oyung/Sequences/Catalog",
        fileName = "SequenceCatalog")]
    internal sealed class SequenceCatalog : Catalog<SequenceAsset> { }
}
