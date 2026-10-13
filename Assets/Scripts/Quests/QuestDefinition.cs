using System;
using System.Collections.Generic;
using System.Linq;
using Oyung.SharedKernel;

namespace Oyung.Quests
{
    // CLAUDE: plain data of one quest, translated from a QuestAsset by the
    // CLAUDE: Unity side. Knows: id, start condition, objectives,
    // CLAUDE: outcomes, instructions. Does: checks itself once when made
    // CLAUDE: (designer mistakes fail at startup, not mid-game).
    // CLAUDE: Used by QuestRun, which applies the rules.
    internal sealed class QuestDefinition
    {
        internal QuestDefinition(string id, Condition startWhen,
            IEnumerable<Instruction> onStart,
            IEnumerable<ObjectiveDefinition> objectives,
            IEnumerable<OutcomeDefinition> outcomes)
        {
            Id = string.IsNullOrEmpty(id)
                ? throw new ArgumentException("A quest has no id.")
                : id;
            StartWhen = startWhen ?? throw Invalid("has no Start-when.");
            OnStart = Instructions.Check(onStart, $"Quest '{id}' On-start");
            Objectives = (objectives ?? Enumerable.Empty<ObjectiveDefinition>())
                .ToList();
            Outcomes = (outcomes ?? Enumerable.Empty<OutcomeDefinition>())
                .ToList();

            ThrowIfDuplicate(Objectives.Select(o => o.Id), "objective");
            ThrowIfDuplicate(Outcomes.Select(o => o.Id), "outcome");
            if (Outcomes.Count == 0)
                throw Invalid("has no outcome, so it could never end.");
            // CLAUDE: a default outcome ("all required done") with nothing
            // CLAUDE: required would close the quest the moment it starts.
            if (Outcomes.Any(o => o.ReachedWhen == null) &&
                Objectives.All(o => o.IsOptional))
                throw Invalid("has a default outcome but no required " +
                              "objective; it would end at once.");
        }

        internal string Id { get; }
        internal Condition StartWhen { get; }
        internal IReadOnlyList<Instruction> OnStart { get; }
        internal IReadOnlyList<ObjectiveDefinition> Objectives { get; }

        /// <summary>In priority order: when several are reached at once,
        /// the first one closes the quest.</summary>
        internal IReadOnlyList<OutcomeDefinition> Outcomes { get; }

        private void ThrowIfDuplicate(IEnumerable<string> ids, string what)
        {
            var duplicate = ids.GroupBy(i => i)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                throw Invalid($"has {what} id '{duplicate.Key}' twice.");
        }

        private InvalidOperationException Invalid(string problem) =>
            new InvalidOperationException($"Quest '{Id}' {problem}");
    }

    // CLAUDE: one step the player sees. Knows: when it appears, when it is
    // CLAUDE: done / failed, whether it is optional, what it orders.
    internal sealed class ObjectiveDefinition
    {
        internal ObjectiveDefinition(string id, bool isOptional,
            Condition appearsWhen, Condition doneWhen, Condition failsWhen,
            IEnumerable<Instruction> onShown, IEnumerable<Instruction> onDone,
            IEnumerable<Instruction> onFailed)
        {
            Id = string.IsNullOrEmpty(id)
                ? throw new ArgumentException("An objective has no id.")
                : id;
            IsOptional = isOptional;
            AppearsWhen = appearsWhen;
            DoneWhen = doneWhen ?? throw new InvalidOperationException(
                $"Objective '{id}' has no Done-when.");
            FailsWhen = failsWhen;
            OnShown = Instructions.Check(onShown, $"Objective '{id}' On-shown");
            OnDone = Instructions.Check(onDone, $"Objective '{id}' On-done");
            OnFailed = Instructions.Check(onFailed,
                $"Objective '{id}' On-failed");
        }

        internal string Id { get; }
        internal bool IsOptional { get; }

        /// <summary>Null: appears once the previous objective is done or
        /// failed (the first one: when the quest starts).</summary>
        internal Condition AppearsWhen { get; }

        internal Condition DoneWhen { get; }

        /// <summary>Null: this objective can't fail.</summary>
        internal Condition FailsWhen { get; }

        internal IReadOnlyList<Instruction> OnShown { get; }
        internal IReadOnlyList<Instruction> OnDone { get; }
        internal IReadOnlyList<Instruction> OnFailed { get; }
    }

    // CLAUDE: one way the quest can end (success, failure, an alternative
    // CLAUDE: ending). Knows: when it is reached and what it orders.
    internal sealed class OutcomeDefinition
    {
        internal OutcomeDefinition(string id, Condition reachedWhen,
            IEnumerable<Instruction> onReached)
        {
            Id = string.IsNullOrEmpty(id)
                ? throw new ArgumentException("An outcome has no id.")
                : id;
            ReachedWhen = reachedWhen;
            OnReached = Instructions.Check(onReached,
                $"Outcome '{id}' On-reached");
        }

        internal string Id { get; }

        /// <summary>Null: reached when every required objective is done.
        /// </summary>
        internal Condition ReachedWhen { get; }

        internal IReadOnlyList<Instruction> OnReached { get; }
    }

    internal static class Instructions
    {
        // CLAUDE: an empty slot in an instruction list is a designer
        // CLAUDE: mistake: fail fast. A missing list is just "none".
        internal static IReadOnlyList<Instruction> Check(
            IEnumerable<Instruction> instructions, string where)
        {
            var list = (instructions ?? Enumerable.Empty<Instruction>())
                .ToList();
            if (list.Contains(null))
                throw new InvalidOperationException(
                    $"{where} has an empty instruction slot.");
            return list;
        }

        internal static void ExecuteAll(IReadOnlyList<Instruction> list)
        {
            foreach (var instruction in list) instruction.Execute();
        }
    }
}
