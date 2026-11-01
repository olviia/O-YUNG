using System.Linq;
using NUnit.Framework;

namespace Oyung.Speech.Tests
{
    // CLAUDE: expected values come from tools/voice/preview_line.py, the
    // CLAUDE: Python reference the baked bups were made with. If a test
    // CLAUDE: breaks after a deliberate change, update both sides.
    internal sealed class LinePlannerTests
    {
        private const float Tolerance = 1e-3f;

        private static readonly string[] Syllables =
            "mblnw".SelectMany(o => "aouei".Select(v => $"{o}{v}")).ToArray();

        private static readonly string[] FunctionWords =
            ("a an the this that is are was our my your his her him " +
             "we you i it so to of and what will").Split(' ');

        private static LinePlanner Planner() =>
            new LinePlanner(new Language(Syllables, FunctionWords));

        [Test]
        public void Hash_MatchesFnv1aReference()
        {
            Assert.AreEqual(2166136261u, LinePlanner.Fnv1a(""));
            Assert.AreEqual(3826002220u, LinePlanner.Fnv1a("a"));
            Assert.AreEqual(2552157898u, LinePlanner.Fnv1a("baby#0"));
        }

        [TestCase("name", 1)]
        [TestCase("cute", 1)]
        [TestCase("baby", 2)]
        [TestCase("finally", 3)]
        [TestCase("shh", 1)]
        public void CountSyllables_FollowsVowelGroups(string word, int count) =>
            Assert.AreEqual(count, LinePlanner.CountSyllables(word));

        [TestCase("baby", "we nu")]
        [TestCase("dear", "wa")]
        [TestCase("finally", "mu be ni")]
        [TestCase("tomorrow", "lu ne wi")]
        public void WordSyllables_MatchPythonReference(string word, string sy) =>
            CollectionAssert.AreEqual(sy.Split(' '),
                Planner().WordSyllables(word));

        [Test]
        public void SameWord_SoundsTheSameEverywhere()
        {
            var events = Planner().Plan("Baby! Our baby? A baby.");
            var babies = events
                .GroupBy(e => e.Word).Where(g => g.Count() == 2)
                .Select(g => string.Join(" ", g.Select(e => e.Syllable)))
                .ToList();
            Assert.AreEqual(3, babies.Count);
            Assert.IsTrue(babies.All(b => b == "we nu"));
        }

        [Test]
        public void Plan_MatchesPythonReference()
        {
            var e = Planner().Plan(
                "Dear, this is our baby! So cute! What will we name him?");
            var expected = new (float start, string sy, float from, float to,
                bool stressed, int word)[]
            {
                (0.00f, "wa", 1.2f, 1.2f, true, 0),
                (0.33f, "no", 1.0638f, 1.0638f, false, 1),
                (0.56f, "bo", 1.0476f, 1.0476f, false, 2),
                (0.79f, "le", 1.0314f, 1.0314f, false, 3),
                (1.02f, "we", 1.1352f, 1.1352f, true, 4),
                (1.32f, "nu", 1.1189f, 0.8791f, false, 4),
                (2.09f, "ne", 1.08f, 1.08f, false, 5),
                (2.32f, "na", 1.3259f, 1.0417f, true, 6),
                (3.09f, "nu", 1.0f, 1.0f, false, 7),
                (3.32f, "wo", 0.985f, 0.985f, false, 8),
                (3.55f, "le", 0.97f, 0.97f, false, 9),
                (3.78f, "mu", 1.075f, 1.075f, true, 10),
                (4.11f, "be", 0.94f, 1.269f, false, 11),
            };
            Assert.AreEqual(expected.Length, e.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                var x = expected[i];
                Assert.AreEqual(x.start, e[i].Start, Tolerance, $"start {i}");
                Assert.AreEqual(x.sy, e[i].Syllable, $"syllable {i}");
                Assert.AreEqual(x.from, e[i].PitchFrom, Tolerance, $"from {i}");
                Assert.AreEqual(x.to, e[i].PitchTo, Tolerance, $"to {i}");
                Assert.AreEqual(x.stressed, e[i].Stressed, $"stress {i}");
                Assert.AreEqual(x.word, e[i].Word, $"word {i}");
            }
        }

        [Test]
        public void Question_RisesAtTheEnd()
        {
            var last = Planner().Plan("Will we name him?").Last();
            Assert.Greater(last.PitchTo, last.PitchFrom);
        }

        [Test]
        public void Exclamation_FallsAtTheEnd()
        {
            var last = Planner().Plan("So cute!").Last();
            Assert.Less(last.PitchTo, last.PitchFrom);
        }

        [Test]
        public void FunctionWords_AreNeverStressed()
        {
            var e = Planner().Plan("this is our");
            Assert.IsTrue(e.All(x => !x.Stressed));
        }

        [Test]
        public void Events_RunForwardWithoutOverlap()
        {
            var e = Planner().Plan("Look at his tiny hands. He sleeps!");
            for (var i = 1; i < e.Count; i++)
                Assert.GreaterOrEqual(e[i].Start,
                    e[i - 1].Start + e[i - 1].Duration - Tolerance);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("?!.")]
        public void NothingToSay_GivesNoEvents(string text) =>
            Assert.IsEmpty(Planner().Plan(text));
    }
}
