using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Cutscenes.Unity
{
    // CLAUDE: every cutscene asset. Used by the installer (bind) and the
    // CLAUDE: player (id -> prefab).
    [CreateAssetMenu(menuName = "Oyung/Cutscenes/Catalog",
        fileName = "CutsceneCatalog")]
    internal sealed class CutsceneCatalog : Catalog<CutsceneAsset> { }
}
