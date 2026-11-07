using UnityEngine;

namespace Oyung.Minigames.NamePop
{
    // CLAUDE: gives one letter's renderer its colour and sparkle, without
    // CLAUDE: making a material for each letter.
    internal static class ToyLetterLook
    {
        private static readonly int BaseColor =
            Shader.PropertyToID("_BaseColor");
        private static readonly int Highlight =
            Shader.PropertyToID("_Highlight");
        private static readonly MaterialPropertyBlock Block =
            new MaterialPropertyBlock();

        internal static void Apply(Renderer renderer, Color colour,
            float highlight)
        {
            renderer.GetPropertyBlock(Block);
            Block.SetColor(BaseColor, colour);
            Block.SetFloat(Highlight, highlight);
            renderer.SetPropertyBlock(Block);
        }
    }
}
