using System;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Quests.Unity
{
    /// <summary>
    /// A question about a quest, for any decider (another quest, a
    /// cutscene, an NPC): "has it started?", "did it end as 'failed'?",
    /// "is objective 'find_mum' done?". Knows the quest asset, what to
    /// ask and, if needed, the objective or outcome id.
    /// </summary>
    [Serializable]
    public sealed class QuestCondition : Condition
    {
        public enum Question
        {
            /// <summary>Active or closed.</summary>
            Started,
            Active,
            Closed,

            /// <summary>Closed with the outcome named in Id.</summary>
            EndedWith,

            /// <summary>The objective in Id is shown, done or failed.
            /// </summary>
            ObjectiveAppeared,
            ObjectiveDone,
            ObjectiveFailed
        }

        [SerializeField] private QuestAsset quest;
        [SerializeField] private Question question;

        [Tooltip("Objective or outcome id; only for questions that name " +
                 "one.")]
        [SerializeField] private string id;

        public override bool IsMet
        {
            get
            {
                var asset = Quest;
                switch (question)
                {
                    case Question.Started:
                        return asset.Value != QuestState.NotStarted;
                    case Question.Active:
                        return asset.Value == QuestState.Active;
                    case Question.Closed:
                        return asset.Value == QuestState.Closed;
                    case Question.EndedWith:
                        RequireId(asset.HasOutcome(id), "outcome");
                        return asset.Value == QuestState.Closed &&
                               asset.Outcome == id;
                    case Question.ObjectiveAppeared:
                        return Objective(asset) != ObjectiveState.Hidden;
                    case Question.ObjectiveDone:
                        return Objective(asset) == ObjectiveState.Done;
                    case Question.ObjectiveFailed:
                        return Objective(asset) == ObjectiveState.Failed;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(question), question, null);
                }
            }
        }

        // CLAUDE: Progressed, not Changed: objective answers change
        // CLAUDE: without the quest's own state changing.
        public override event Action Changed
        {
            add => Quest.Progressed += value;
            remove => Quest.Progressed -= value;
        }

        private ObjectiveState Objective(QuestAsset asset)
        {
            RequireId(asset.HasObjective(id), "objective");
            return asset.StateOf(id);
        }

        // CLAUDE: a typo in the id would make the condition silently never
        // CLAUDE: hold: fail fast instead.
        private void RequireId(bool exists, string what)
        {
            if (!exists)
                throw new InvalidOperationException(
                    $"QuestCondition: quest '{quest.Name}' has no {what} " +
                    $"'{id}'.");
        }

        // CLAUDE: an empty slot is a designer mistake: fail fast.
        private QuestAsset Quest => quest != null
            ? quest
            : throw new InvalidOperationException(
                "QuestCondition has no quest.");
    }
}
