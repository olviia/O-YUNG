using System;
using System.Collections;
using Oyung.Actors;
using Oyung.SharedKernel;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Oyung.Dialogues.Unity
{
    // CLAUDE: TEMPORARY, with ChatterAsset: delete both when the full
    // CLAUDE: dialogue tool exists (docs/ToDo.md item 13).
    // CLAUDE: class comment (your words). Draft: plays a chatter in the
    // CLAUDE: scene: random exchanges with random pauses, until its stop
    // CLAUDE: condition holds or a speaker leaves. Only hands out lines;
    // CLAUDE: the actors do the speaking.
    // CLAUDE: Known gap: if something else makes the speaker Say while a
    // CLAUDE: line plays, its done never comes and the chatter waits for
    // CLAUDE: good. Nothing else speaks yet; reactions will have to.
    // CLAUDE: Wrong on purpose for now: reads its conditions from Unity's
    // CLAUDE: Start. The full tool must start in the root's start phase.
    internal sealed class ChatterPlayer : MonoBehaviour
    {
        // The voice always speaks this locale; subtitles are another matter.
        private const string VoiceLocale = "en";

        [SerializeField] private ChatterAsset chatter;
        [Tooltip("Empty: starts at once.")]
        [SerializeReference] private Condition startWhen;
        [Tooltip("Empty: never stops. Checked before each line, so a line "
                 + "that has started always finishes.")]
        [SerializeReference] private Condition stopWhen;

        private bool ShouldStart => startWhen == null || startWhen.IsMet;
        private bool ShouldStop => stopWhen != null && stopWhen.IsMet;

        // Start, not Awake: CompositionRoot binds facts in Awake.
        private IEnumerator Start()
        {
            if (chatter == null)
                throw new InvalidOperationException(
                    $"ChatterPlayer '{name}' has no chatter; set it in the "
                    + "Inspector.");

            yield return LocalizationSettings.InitializationOperation;
            var voice = LocalizationSettings.AvailableLocales
                .GetLocale(VoiceLocale);
            if (voice == null)
                throw new InvalidOperationException(
                    $"No '{VoiceLocale}' locale: the voice has nothing to "
                    + "read.");

            yield return new WaitUntil(() => ShouldStart);

            var picker = new ExchangePicker(chatter.Exchanges.Count,
                n => UnityEngine.Random.Range(0, n));
            while (true)
            {
                var exchange = chatter.Exchanges[picker.Next()];
                for (var i = 0; i < exchange.Lines.Count; i++)
                {
                    if (ShouldStop) yield break;
                    if (i > 0)
                        yield return new WaitForSeconds(chatter.LineGap);

                    var line = exchange.Lines[i];
                    var id = chatter.SpeakerOf(line);
                    if (!(id.Current is ISpeakingActor speaking)) yield break;

                    var english = LocalizationSettings.StringDatabase
                        .GetLocalizedStringAsync(line.Text.TableReference,
                            line.Text.TableEntryReference, voice);
                    yield return english;

                    var finished = false;
                    speaking.Say(english.Result, () => finished = true);
                    yield return new WaitUntil(
                        () => finished || id.Current == null);
                }
                yield return new WaitForSeconds(chatter.RandomPause());
            }
        }
    }
}
