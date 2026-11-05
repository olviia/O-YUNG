using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oyung.Speech.Unity
{
    // CLAUDE: class comment (your words). Draft: gives a character a voice.
    // CLAUDE: Takes a line, plans it with its voice's language and plays it
    // CLAUDE: syllable by syllable. Knows nothing about who tells it to.
    // CLAUDE: Timing runs on the audio clock, not on frames: each syllable
    // CLAUDE: is scheduled just before it sounds, so a slow frame can't
    // CLAUDE: break the rhythm and stopping needs no undoing.
    // CLAUDE: Switched off while silent, so an idle character costs nothing.
    public sealed class Speaker : MonoBehaviour
    {
        // Seconds from Say to the first sound, so it can be scheduled.
        private const double StartDelay = 0.1;
        // How far ahead a syllable is handed to the audio system.
        private const double ScheduleAhead = 0.1;
        // Matches tools/voice/preview_line.py.
        private const float UnstressedVolume = 0.75f;

        [SerializeField] private VoiceAsset voice;
        [Tooltip("Where the voice comes from: its position, distance and "
                 + "volume settings apply to every syllable. On a child "
                 + "object of its own, since it is cloned for overlaps.")]
        [SerializeField] private AudioSource mouth;
        // One AudioSource plays one clip, but a bup still rings when the
        // next syllable starts: bups last up to 1.2 s, syllables start
        // every 0.2 to 0.3 s. So the mouth is cloned, overlap sources in
        // all, taken in turn. Too few and the oldest tail is cut off.
        [Tooltip("How many syllables can ring at once; also how many "
                 + "AudioSources this speaker has. Too few cuts tails off.")]
        [Min(1)]
        [SerializeField] private int overlap = 6;

        private Sound[] _sounds;
        private int _nextSound;
        private float _volume;

        private IReadOnlyList<SyllableEvent> _line;
        private int _nextSyllable;
        private double _lineStart;
        private double _silentAt;
        private Action _done;

        /// <summary>Speaks the line, then calls done (may be null) once.
        /// A new line replaces the old one, whose done is never called.
        /// A line with no sayable words is done at once.</summary>
        public void Say(string text, Action done)
        {
            StopLine();
            var line = voice.Plan(text);
            if (line.Count == 0)
            {
                done?.Invoke();
                return;
            }
            _line = line;
            _nextSyllable = 0;
            _lineStart = AudioSettings.dspTime + StartDelay;
            _done = done;
            enabled = true;
        }

        private void Awake()
        {
            // Checked here, not left to a NullReferenceException later.
            if (voice == null) throw Missing(nameof(voice));
            if (mouth == null) throw Missing(nameof(mouth));
            if (mouth.gameObject == gameObject)
                throw new InvalidOperationException(
                    $"Speaker '{name}': the mouth must be on a child object;"
                    + " cloning this one would clone the Speaker too.");
            mouth.playOnAwake = false;
            mouth.loop = false;
            _volume = mouth.volume;
            _sounds = new Sound[overlap];
            _sounds[0] = new Sound(mouth);
            for (var i = 1; i < overlap; i++)
                _sounds[i] = new Sound(
                    Instantiate(mouth, mouth.transform.parent));
        }

        private void OnDisable() => StopLine();

        private void Update()
        {
            var now = AudioSettings.dspTime;
            if (_line != null)
                while (_nextSyllable < _line.Count && SyllableTime(_nextSyllable)
                       <= now + ScheduleAhead)
                    Schedule(_nextSyllable++);
            // Keeps gliding after the line is done: tails still ring.
            foreach (var sound in _sounds) sound.Glide(now, voice.BasePitch);
            if (_line != null && _nextSyllable == _line.Count
                              && now >= LineEnd())
                FinishLine();
            // Checked after FinishLine: its done may have started a new line.
            if (_line == null && now >= _silentAt) enabled = false;
        }

        private void Schedule(int index)
        {
            var syllable = _line[index];
            var sound = _sounds[_nextSound];
            _nextSound = (_nextSound + 1) % _sounds.Length;
            var volume = syllable.Stressed ? _volume
                : _volume * UnstressedVolume;
            // The take goes by position, as in preview_line.py, so one
            // syllable said twice in a row doesn't sound copied.
            var clip = voice.Clip(syllable.Syllable, index);
            var at = SyllableTime(index);
            sound.Play(clip, volume, at, syllable, voice.BasePitch);
            // Lower pitch plays slower, so the lowest one rings the longest.
            var lowest = voice.BasePitch
                         * Mathf.Min(syllable.PitchFrom, syllable.PitchTo);
            _silentAt = Math.Max(_silentAt, at + clip.length / lowest);
        }

        private double SyllableTime(int index) =>
            _lineStart + _line[index].Start;

        private double LineEnd() =>
            SyllableTime(_line.Count - 1) + _line[_line.Count - 1].Duration;

        private void FinishLine()
        {
            var done = _done;
            _line = null;
            _done = null;
            done?.Invoke();
        }

        private void StopLine()
        {
            _line = null;
            _done = null;
            _silentAt = 0;
            if (_sounds == null) return;
            foreach (var sound in _sounds) sound.Stop();
        }

        private Exception Missing(string field) =>
            new InvalidOperationException(
                $"Speaker '{name}' has no {field}; set it in the Inspector.");

        // One syllable ringing on its own source; a question or an
        // exclamation slides its pitch while it rings.
        private sealed class Sound
        {
            private readonly AudioSource _source;
            private double _start;
            private float _length = 1f;
            private float _pitchFrom = 1f;
            private float _pitchTo = 1f;

            internal Sound(AudioSource source) => _source = source;

            internal void Play(AudioClip clip, float volume, double at,
                SyllableEvent syllable, float basePitch)
            {
                _start = at;
                _length = clip.length;
                _pitchFrom = syllable.PitchFrom;
                _pitchTo = syllable.PitchTo;
                _source.clip = clip;
                _source.volume = volume;
                _source.pitch = basePitch * _pitchFrom;
                _source.PlayScheduled(at);
            }

            internal void Glide(double now, float basePitch)
            {
                if (_pitchFrom == _pitchTo) return;
                var progress = (float)((now - _start) / _length);
                _source.pitch = basePitch
                                * Mathf.Lerp(_pitchFrom, _pitchTo, progress);
            }

            internal void Stop() => _source.Stop();
        }
    }
}
