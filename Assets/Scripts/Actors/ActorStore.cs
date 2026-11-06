using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Actors
{
    // CLAUDE: where an actor stood and how, while it isn't there to say so
    // CLAUDE: itself. Pose null: not holding one, just standing or walking.
    internal readonly struct ActorState
    {
        internal ActorState(Vector3 position, Quaternion rotation, string pose)
        {
            Position = position;
            Rotation = rotation;
            Pose = pose;
        }

        internal Vector3 Position { get; }
        internal Quaternion Rotation { get; }
        internal string Pose { get; }
    }

    // CLAUDE: class comment (your words). Draft: knows where each actor
    // CLAUDE: last stood, by id name. Outlives the actors: a chunk unloading
    // CLAUDE: leaves its actors' state here. Knows no actor; beforeGive lets
    // CLAUDE: the installer write down the ones standing in the world now.
    // CLAUDE: Used by Actor (places itself from it) and by Save.
    internal sealed class ActorStore : ISaveable
    {
        private const string PositionKey = "position";
        private const string RotationKey = "rotation";
        private const string PoseKey = "pose";

        private readonly Dictionary<string, ActorState> _states =
            new Dictionary<string, ActorState>();

        private readonly Action _beforeGive;

        internal ActorStore(Action beforeGive) => _beforeGive = beforeGive;

        /// <summary>False if this actor has no state yet: it stands where
        /// the scene put it.</summary>
        internal bool TryGet(string id, out ActorState state) =>
            _states.TryGetValue(id, out state);

        internal void Set(string id, ActorState state) => _states[id] = state;

        public string Section => "actors";

        public IReadOnlyDictionary<string, object> Give()
        {
            _beforeGive();
            return _states.ToDictionary(s => s.Key, s => (object)ToSection(s.Value));
        }

        public void Take(IReadOnlyDictionary<string, object> section)
        {
            _states.Clear();
            foreach (var entry in section)
                _states[entry.Key] = FromSection(
                    (IReadOnlyDictionary<string, object>)entry.Value);
        }

        private static Dictionary<string, object> ToSection(ActorState state)
        {
            var p = state.Position;
            var r = state.Rotation;
            return new Dictionary<string, object>
            {
                [PositionKey] = new List<object> { p.x, p.y, p.z },
                [RotationKey] = new List<object> { r.x, r.y, r.z, r.w },
                [PoseKey] = state.Pose
            };
        }

        private static ActorState FromSection(
            IReadOnlyDictionary<string, object> section)
        {
            var p = Floats(section[PositionKey]);
            var r = Floats(section[RotationKey]);
            return new ActorState(new Vector3(p[0], p[1], p[2]),
                new Quaternion(r[0], r[1], r[2], r[3]),
                (string)section[PoseKey]);
        }

        // CLAUDE: Convert, not a cast: a save returns decimals as double,
        // CLAUDE: and a hand-edited 3 instead of 3.0 as long.
        private static float[] Floats(object list) =>
            ((IEnumerable<object>)list).Select(Convert.ToSingle).ToArray();
    }
}
