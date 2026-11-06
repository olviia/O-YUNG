using System.Runtime.CompilerServices;

// The Unity side plays what the inside picks.
[assembly: InternalsVisibleTo("Oyung.Dialogues.Unity")]
// EditMode tests check the picking.
[assembly: InternalsVisibleTo("Oyung.Dialogues.Tests")]
