using System.Runtime.CompilerServices;

// Only the composition root may see this module's internals, so it can
// create them.
[assembly: InternalsVisibleTo("Oyung.Root")]
// The Unity side (language and voice assets, playback) builds the planner.
[assembly: InternalsVisibleTo("Oyung.Speech.Unity")]
// EditMode tests check the planner against the Python reference.
[assembly: InternalsVisibleTo("Oyung.Speech.Tests")]
