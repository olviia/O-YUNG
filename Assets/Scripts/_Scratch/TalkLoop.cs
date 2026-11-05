using System.Collections;
using Oyung.Speech.Unity;
using UnityEngine;

namespace Oyung.Scratch
{
    // CLAUDE: dev-only: the speakers take turns saying random phrases for
    // CLAUDE: as long as Play mode runs, so voices and talk motion can be
    // CLAUDE: heard, watched and tuned live. Used by the Test scene.
    public sealed class TalkLoop : MonoBehaviour
    {
        private static readonly string[] Phrases =
        {
            "Look at our baby!",
            "What will we name her?",
            "I don't know.",
            "She has your eyes.",
            "Is she sleeping?",
            "Shh, don't wake the baby.",
            "Did you feed her today?",
            "She smiled at me!",
            "Tomorrow we finally go outside.",
            "Who is a little sweetheart?",
            "I think she is hungry again.",
            "We should rest a little.",
        };

        [Tooltip("They speak in this order, then start over.")]
        [SerializeField] private Speaker[] speakers;
        [Tooltip("Seconds of silence between two lines.")]
        [Min(0f)]
        [SerializeField] private float pause = 0.6f;

        private int _lastPhrase = -1;

        private IEnumerator Start()
        {
            var turn = 0;
            while (true)
            {
                var finished = false;
                speakers[turn].Say(NextPhrase(), () => finished = true);
                yield return new WaitUntil(() => finished);
                yield return new WaitForSeconds(pause);
                turn = (turn + 1) % speakers.Length;
            }
        }

        // Random, but never the same phrase twice in a row.
        private string NextPhrase()
        {
            var phrase = Random.Range(0, Phrases.Length - 1);
            if (phrase >= _lastPhrase && _lastPhrase >= 0) phrase++;
            _lastPhrase = phrase;
            return Phrases[phrase];
        }
    }
}
