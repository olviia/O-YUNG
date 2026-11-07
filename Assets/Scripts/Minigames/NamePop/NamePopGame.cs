using System;
using System.Collections;
using System.Collections.Generic;
using Oyung.Actors;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: runs the minigame: bubbles come from the parents and take
    // CLAUDE: their keyboard places; popping one types, removes or finishes.
    // CLAUDE: Saving stays blocked for the whole run. Aiming and pressing
    // CLAUDE: are the game's ordinary gaze and Interact: bubbles are
    // CLAUDE: Interactables. The finished name leaves through the config's
    // CLAUDE: instruction, last, after everything else let go.
    public sealed class NamePopGame : MonoBehaviour
    {
        private const string SaveBlockReason = "name minigame";

        [SerializeField] private NamePopConfig config;
        [Tooltip("Sits at the keyboard's fixed place in the world.")]
        [SerializeField] private Transform keyboard;
        [SerializeField] private NameDisplay nameDisplay;
        [Tooltip("Draws the sharp layer over the blurred view. Off "
                 + "between runs.")]
        [SerializeField] private Camera overlayCamera;
        [SerializeField] private Camera viewCamera;
        [Tooltip("The aiming mark at the centre of the view.")]
        [SerializeField] private GameObject reticle;
        [Tooltip("New bubbles appear where these actors speak from.")]
        [SerializeField] private ActorId[] parents;
        [SerializeField] private LetterBubble bubblePrefab;
        [SerializeField] private GameObject burstPrefab;
        [SerializeField] private AudioSource popSound;

        private readonly List<LetterBubble> _letters =
            new List<LetterBubble>();
        private readonly Dictionary<char, Color> _colours =
            new Dictionary<char, Color>();
        private LetterBubble _remove;
        private LetterBubble _done;
        private ISaveBlocker _saveBlocker;
        private NameEntry _entry;
        private bool _finishing;

        internal void Use(ISaveBlocker saveBlocker) =>
            _saveBlocker = saveBlocker;

        internal void Play()
        {
            if (_entry != null)
                throw new InvalidOperationException(
                    "The name minigame was asked to play while it plays.");

            _saveBlocker.Block(SaveBlockReason);
            _entry = new NameEntry(config.MaxLength);
            _finishing = false;
            overlayCamera.gameObject.SetActive(true);
            reticle.SetActive(true);
            if (_letters.Count == 0) BuildKeyboard();

            foreach (var bubble in _letters)
                bubble.SendFrom(SpawnPoint(),
                    UnityEngine.Random.value * config.FirstWaveSeconds,
                    config.ArrivalSeconds);
            ShowActionKeys(false);
        }

        internal void OnPopped(LetterBubble bubble)
        {
            if (_entry == null || _finishing) return;

            Burst(bubble.transform.position);
            popSound.pitch = config.PopPitch +
                             config.PopPitchStep * _entry.Length;
            popSound.Play();

            switch (bubble.Type)
            {
                case LetterBubble.Kind.Letter:
                    TypeLetter(bubble);
                    break;
                case LetterBubble.Kind.Remove:
                    RemoveLetter();
                    break;
                case LetterBubble.Kind.Done:
                    StartCoroutine(Finish());
                    break;
            }
        }

        private void TypeLetter(LetterBubble bubble)
        {
            bubble.SendFrom(SpawnPoint(), 0f, config.ArrivalSeconds);
            // CLAUDE: a full name just doesn't take it: that shows the limit.
            if (!_entry.Add(bubble.Letter)) return;
            nameDisplay.Add(config.MeshOf(bubble.Letter),
                _colours[bubble.Letter], config);
            if (_entry.Length == 1) ShowActionKeys(true);
        }

        private void RemoveLetter()
        {
            _entry.RemoveLast();
            nameDisplay.RemoveLast();
            if (!_entry.CanFinish)
            {
                ShowActionKeys(false);
                return;
            }
            // CLAUDE: back at once: taking back several shouldn't wait.
            _remove.GrowInPlace(config.ActionKeyGrowSeconds);
        }

        private IEnumerator Finish()
        {
            _finishing = true;
            foreach (var bubble in _letters) PopAway(bubble);
            PopAway(_remove);
            _done.Hide();
            reticle.SetActive(false);

            yield return new WaitForSeconds(config.FinishedHoldSeconds);

            nameDisplay.Clear();
            overlayCamera.gameObject.SetActive(false);
            var finished = _entry.Text;
            _entry = null;
            _saveBlocker.Unblock(SaveBlockReason);
            // CLAUDE: last: the fact may start what comes next.
            if (config.OnNamed != null) config.OnNamed.Execute(finished);
        }

        private void PopAway(LetterBubble bubble)
        {
            if (bubble.IsShown) Burst(bubble.transform.position);
            bubble.Hide();
        }

        private void ShowActionKeys(bool show)
        {
            if (show)
            {
                _remove.GrowInPlace(config.ActionKeyGrowSeconds);
                _done.GrowInPlace(config.ActionKeyGrowSeconds);
                return;
            }
            _remove.Hide();
            _done.Hide();
        }

        private void BuildKeyboard()
        {
            for (var row = 0; row < config.Rows.Count; row++)
            {
                var keys = config.Rows[row];
                for (var column = 0; column < keys.Length; column++)
                {
                    var colour = config.ColourAt(row, column);
                    _colours[keys[column]] = colour;
                    var slot = KeyPlace(row, column - (keys.Length - 1) / 2f);
                    _letters.Add(MakeBubble(LetterBubble.Kind.Letter,
                        keys[column], config.MeshOf(keys[column]), colour,
                        slot));
                }
            }

            var lastRow = config.Rows.Count - 1;
            _remove = MakeBubble(LetterBubble.Kind.Remove, '\0',
                config.RemoveMesh, config.RemoveColour,
                KeyPlace(lastRow, -config.ActionKeysOffset));
            _done = MakeBubble(LetterBubble.Kind.Done, '\0',
                config.DoneMesh, config.DoneColour,
                KeyPlace(lastRow, config.ActionKeysOffset));
        }

        private Vector3 KeyPlace(int row, float keysFromCentre) => new Vector3(
            keysFromCentre * config.KeySpacing,
            ((config.Rows.Count - 1) / 2f - row) * config.RowSpacing, 0f);

        private LetterBubble MakeBubble(LetterBubble.Kind kind, char letter,
            Mesh mesh, Color colour, Vector3 slot)
        {
            var bubble = Instantiate(bubblePrefab, keyboard);
            bubble.Init(this, config, kind, letter, mesh, colour, slot);
            return bubble;
        }

        private Vector3 SpawnPoint()
        {
            var id = parents[UnityEngine.Random.Range(0, parents.Length)];
            if (!(id.Current is IPhysicalActor parent))
                throw new InvalidOperationException(
                    $"Actor '{id.name}' is not in the scene to make bubbles.");
            return parent.Position
                   + Vector3.up * (parent.Height * config.SpeakingHeight)
                   + UnityEngine.Random.insideUnitSphere * config.SpawnScatter;
        }

        // CLAUDE: destroys itself when done (Stop Action on the prefab).
        private void Burst(Vector3 place) =>
            Instantiate(burstPrefab, place, Quaternion.identity);

        // CLAUDE: Cinemachine owns the lens; the overlay must match it.
        private void LateUpdate()
        {
            if (_entry != null)
                overlayCamera.fieldOfView = viewCamera.fieldOfView;
        }
    }
}
