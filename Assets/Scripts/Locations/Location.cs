using System;
using UnityEngine;

namespace Oyung.Locations
{
    // CLAUDE: class comment (your words). Draft: a placeholder in the
    // CLAUDE: scene that marks a named spot: its transform is the spot,
    // CLAUDE: its forward the facing (turned flat). Registers on its
    // CLAUDE: id while enabled, so whoever has the id finds it. Draws an arrow
    // CLAUDE: and its name in the Scene view, since it has no mesh.
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

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var at = Position;
            var ahead = Facing * Vector3.forward * 0.4f;
            Gizmos.color = new Color(1f, 0.6f, 0.2f);
            Gizmos.DrawWireSphere(at, 0.12f);
            Gizmos.DrawLine(at, at + ahead);
            Gizmos.DrawLine(at + ahead, at + ahead * 0.7f +
                                        Facing * Vector3.right * 0.08f);
            Gizmos.DrawLine(at + ahead, at + ahead * 0.7f -
                                        Facing * Vector3.right * 0.08f);
            UnityEditor.Handles.Label(at + Vector3.up * 0.25f,
                id != null ? id.name : name);
        }
#endif
    }
}
