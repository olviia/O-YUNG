using Oyung.SharedKernel.Unity;
using UnityEngine;

namespace Oyung.Actors
{
    // CLAUDE: every actor id. Used by ActorsInstaller.
    [CreateAssetMenu(menuName = "Oyung/Actors/Catalog",
        fileName = "ActorCatalog")]
    internal sealed class ActorCatalog : Catalog<ActorId> { }
}
