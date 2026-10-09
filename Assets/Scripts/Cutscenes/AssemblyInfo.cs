using System.Runtime.CompilerServices;

// Only the composition root may see this module's internals, so it can create them.
[assembly: InternalsVisibleTo("Oyung.Root")]
// The Unity side (assets, player) reaches the internal store and port.
[assembly: InternalsVisibleTo("Oyung.Cutscenes.Unity")]
