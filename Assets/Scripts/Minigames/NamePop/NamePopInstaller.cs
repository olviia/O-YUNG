using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    /// <summary>
    /// NamePop: needs saving to be blockable. Started by a StartNamePop
    /// instruction; hands its name out through the config's instruction.
    /// </summary>
    public class NamePopInstaller : MonoBehaviour
    {
        [SerializeField] private NamePopGame game;

        public void Build(ISaveBlocker saveBlocker) => game.Use(saveBlocker);
    }
}
