using System.Runtime.CompilerServices;

// Only the composition root may see this module's internals, so it can create them.
[assembly: InternalsVisibleTo("Oyung.Root")]
// The Unity side (assets, installer) builds definitions and the journal.
[assembly: InternalsVisibleTo("Oyung.Quests.Unity")]
// EditMode tests drive the core with fake conditions.
[assembly: InternalsVisibleTo("Oyung.Quests.Tests")]
