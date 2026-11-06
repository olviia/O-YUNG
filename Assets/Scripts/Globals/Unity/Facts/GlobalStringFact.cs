using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: exists only because Unity can't make assets of a generic
    // CLAUDE: type. Internal: other modules see ModuleFact<string>.
    [CreateAssetMenu(menuName = "Oyung/Globals/String",
        fileName = "NewGlobalStringFact")]
    internal sealed class GlobalStringFact : GlobalFact<string>
    {
    }
}
