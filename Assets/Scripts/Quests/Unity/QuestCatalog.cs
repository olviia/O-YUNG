using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Quests.Unity
{
    // CLAUDE: every quest asset. Used by the installer (bind,
    // CLAUDE: definitions) and QuestTexts.
    [CreateAssetMenu(menuName = "Oyung/Quests/Catalog",
        fileName = "QuestCatalog")]
    internal sealed class QuestCatalog : Catalog<QuestAsset> { }
}
