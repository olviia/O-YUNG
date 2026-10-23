using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: every global fact asset, whatever its value type.
    // CLAUDE: Used by GlobalsInstaller (later: Save / Yarn by name).
    [CreateAssetMenu(menuName = "Oyung/Globals/Catalog",
        fileName = "GlobalCatalog")]
    internal sealed class GlobalCatalog : Catalog<IGlobalFact> { }
}
