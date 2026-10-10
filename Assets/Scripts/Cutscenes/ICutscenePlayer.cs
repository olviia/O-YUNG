namespace Oyung.Cutscenes
{
    // CLAUDE: class comment (your words). Draft: what a cutscene asks
    // CLAUDE: of whoever really plays cutscenes. Internal: only this
    // CLAUDE: module's Unity side implements it (TimelineCutscenePlayer).
    internal interface ICutscenePlayer
    {
        /// <summary>Plays the cutscene with this id.</summary>
        void Play(string id);
    }
}
