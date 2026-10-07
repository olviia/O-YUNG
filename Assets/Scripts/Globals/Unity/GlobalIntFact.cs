using UnityEngine;

namespace Oyung.Globals.Unity
{
    // CLAUDE: exists only because Unity can't make assets of a generic
    // CLAUDE: type. Internal: other modules see ModuleFact<int>.
    [CreateAssetMenu(menuName = "Oyung/Globals/Int",
        fileName = "NewGlobalIntFact")]
    internal sealed class GlobalIntFact : GlobalFact<int>
    {
    }
}
