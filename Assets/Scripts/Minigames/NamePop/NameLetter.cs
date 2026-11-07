using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: one letter of the name. Lands with a small drop and settles,
    // CLAUDE: while its soft shadow firms up under it, so it reads as
    // CLAUDE: standing on something. Shrinks away when taken back.
    internal sealed class NameLetter : MonoBehaviour
    {
        private const float GoneSeconds = 0.15f;

        [SerializeField] private MeshFilter letterMesh;
        [SerializeField] private MeshRenderer letterRenderer;
        [SerializeField] private Transform shadow;

        private Vector3 _restPlace;
        private Vector3 _restScale;
        private Vector3 _shadowScale;
        private float _drop;
        private float _seconds;
        private float _time;
        private float _goneTime = -1f;

        internal void Land(Mesh mesh, Color colour, float height,
            Vector3 place, float drop, float seconds)
        {
            letterMesh.sharedMesh = mesh;
            ToyLetterLook.Apply(letterRenderer, colour, 0f);
            _restPlace = place;
            _restScale = Vector3.one * height;
            _shadowScale = new Vector3(mesh.bounds.size.x * height * 1.1f,
                height * 0.25f, 1f);
            _drop = drop;
            _seconds = seconds;
            transform.localRotation = Quaternion.identity;
            Show(0f);
        }

        internal void Leave() => _goneTime = 0f;

        private void Update()
        {
            if (_goneTime >= 0f)
            {
                _goneTime += Time.deltaTime;
                if (_goneTime >= GoneSeconds) Destroy(gameObject);
                else transform.localScale =
                    _restScale * (1f - _goneTime / GoneSeconds);
                return;
            }
            if (_time >= 1f) return;
            _time = Mathf.Min(1f, _time + Time.deltaTime / _seconds);
            Show(_time);
        }

        private void Show(float t)
        {
            // CLAUDE: falls fast, then a small squash that settles to rest.
            var fall = Mathf.Clamp01(t / 0.45f);
            var settle = Mathf.Clamp01((t - 0.45f) / 0.55f);
            var squash = Mathf.Sin(settle * Mathf.PI) * 0.12f;

            transform.localPosition =
                _restPlace + Vector3.up * (_drop * (1f - fall * fall));
            letterMesh.transform.localScale =
                new Vector3(1f + squash, 1f - squash, 1f);
            transform.localScale = _restScale * Mathf.Lerp(1.15f, 1f, fall);

            // CLAUDE: the shadow firms up as the letter comes down.
            shadow.localScale = new Vector3(
                _shadowScale.x / _restScale.x * fall,
                _shadowScale.y / _restScale.y * fall, 1f);
        }
    }
}
