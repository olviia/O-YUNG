using System.Collections;
using Oyung.Locomotion;
using UnityEngine;

namespace Oyung.Scratch
{
    // CLAUDE: dev-only: walks a body back and forth between points so
    // CLAUDE: the gait can be watched and tuned live in Play mode (edit
    // CLAUDE: Gait / Walker values in the Inspector while it walks).
    // CLAUDE: Replaced by ScriptedBehaviour; delete then.
    public class WalkLoop : MonoBehaviour
    {
        [SerializeField] private Walker walker;
        [Tooltip("Each point's position and rotation is a stop.")]
        [SerializeField] private Transform[] points;
        [SerializeField] private float pause = 1.5f;

        private int next;

        private IEnumerator Start()
        {
            while (true)
            {
                var p = points[next];
                var arrived = false;
                void OnArrived() => arrived = true;
                walker.Arrived += OnArrived;
                walker.WalkTo(p.position, p.rotation);
                yield return new WaitUntil(() => arrived);
                walker.Arrived -= OnArrived;
                yield return new WaitForSeconds(pause);
                next = (next + 1) % points.Length;
            }
        }
    }
}
