using System;
using UnityEngine;

namespace Oyung.Locations
{
    // CLAUDE: class comment (your words). Draft: a placeholder in the
    // CLAUDE: scene that marks a named spot: its transform is the spot,
    // CLAUDE: its forward the facing (turned flat). Registers on its
    // CLAUDE: id while enabled, so whoever has the id finds it. Has no
    // CLAUDE: mesh; the editor draws it (LocationGizmo).
    public sealed class Location : MonoBehaviour, ILocation
    {
        [SerializeField] private LocationId id;

        public LocationId Id => id;

        public Vector3 Position => transform.position;

        public Quaternion Facing =>
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        private void OnEnable()
        {
            if (id == null)
                throw new InvalidOperationException(
                    $"Location '{name}' has no id; set it in the " +
                    "Inspector.");
            id.Bind(this);
        }

        private void OnDisable()
        {
            if (id != null) id.Unbind(this);
        }
    }
}
