using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: exists only because Unity can't make assets of a generic
    // CLAUDE: type. Internal: other modules see ModuleFact<bool>.
    [CreateAssetMenu(menuName = "Oyung/Globals/Bool",
        fileName = "NewGlobalBoolFact")]
    internal sealed class GlobalBoolFact : GlobalFact<bool>
    {
    }
}
