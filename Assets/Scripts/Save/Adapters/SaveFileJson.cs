using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Oyung.Save.Adapters
{
    // CLAUDE: class comment (your words). Draft: knows the save file's
    // CLAUDE: format: a version and the sections, readable and
    // CLAUDE: hand-editable. The only place in the game that knows saves
    // CLAUDE: are JSON; modules deal in plain dictionaries.
    // CLAUDE: Used by FileSaveStorage to write and the loader to read.
    internal static class SaveFileJson
    {
        // CLAUDE: raise when an old file needs converting; conversion goes
        // CLAUDE: in Parse, the only place that reads a version.
        private const int CurrentVersion = 1;

        private const string VersionKey = "version";
        private const string SectionsKey = "sections";

        internal static string Compose(IReadOnlyDictionary<string,
            IReadOnlyDictionary<string, object>> sections)
        {
            var nested = new JObject();
            foreach (var section in sections)
                nested[section.Key] = ToToken(section.Value);

            return new JObject
            {
                [VersionKey] = CurrentVersion,
                [SectionsKey] = nested
            }.ToString(Formatting.Indented);
        }

        /// <summary>The sections of a save file. Throws if the text isn't
        /// one, or comes from a newer build.</summary>
        internal static IReadOnlyDictionary<string,
            IReadOnlyDictionary<string, object>> Parse(string text)
        {
            if (!(ParseToken(text) is JObject root)
                || !(root[VersionKey] is JValue version)
                || version.Type != JTokenType.Integer
                || !(root[SectionsKey] is JObject nested))
                throw new InvalidDataException(
                    "Not a save file: expected a version and sections.");

            if ((long)version > CurrentVersion)
                throw new InvalidDataException(
                    $"Save version {version} is newer than this build's "
                    + $"{CurrentVersion}.");

            var sections = new Dictionary<string,
                IReadOnlyDictionary<string, object>>();
            foreach (var section in nested)
            {
                if (!(section.Value is JObject content))
                    throw new InvalidDataException(
                        $"Save section '{section.Key}' is not a section.");
                sections[section.Key] = ToSection(content);
            }
            return sections;
        }

        // CLAUDE: an allowed list on purpose, not JToken.FromObject: that
        // CLAUDE: takes anything, e.g. an enum as a bare number that means
        // CLAUDE: something else once the enum changes.
        private static JToken ToToken(object value)
        {
            switch (value)
            {
                case null: return JValue.CreateNull();
                case bool v: return new JValue(v);
                case int v: return new JValue(v);
                case long v: return new JValue(v);
                case float v: return new JValue(v);
                case double v: return new JValue(v);
                case string v: return new JValue(v);
                case IReadOnlyDictionary<string, object> section:
                    var nested = new JObject();
                    foreach (var entry in section)
                        nested[entry.Key] = ToToken(entry.Value);
                    return nested;
                case IEnumerable<object> list:
                    return new JArray(list.Select(ToToken));
                default:
                    throw new ArgumentException(
                        $"A save can't hold a {value.GetType().Name}; see "
                        + "ISaveable.");
            }
        }

        private static IReadOnlyDictionary<string, object> ToSection(
            JObject content)
        {
            var section = new Dictionary<string, object>();
            foreach (var entry in content)
                section[entry.Key] = FromToken(entry.Value);
            return section;
        }

        private static object FromToken(JToken token)
        {
            switch (token)
            {
                case JObject content: return ToSection(content);
                case JArray list: return list.Select(FromToken).ToList();
                case JValue value: return value.Value;
                default:
                    throw new InvalidDataException(
                        $"Save holds an unexpected {token.Type}.");
            }
        }

        // CLAUDE: dates off: by default a string that looks like a date
        // CLAUDE: comes back rewritten, so a value wouldn't round-trip.
        private static JToken ParseToken(string text)
        {
            using (var reader = new JsonTextReader(new StringReader(text))
                   {
                       DateParseHandling = DateParseHandling.None
                   })
            {
                return JToken.ReadFrom(reader);
            }
        }
    }
}
