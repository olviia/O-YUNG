using System.Runtime.CompilerServices;

// Only the composition root may see this module's internals, so it can create them.
[assembly: InternalsVisibleTo("Oyung.Root")]
// The Unity side implements IInteractable and feeds the Interactor.
[assembly: InternalsVisibleTo("Oyung.Interaction.Unity")]
// EditMode tests drive the core with fake targets and conditions.
[assembly: InternalsVisibleTo("Oyung.Interaction.Tests")]
