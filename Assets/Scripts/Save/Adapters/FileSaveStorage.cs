using System.Collections.Generic;
using System.IO;

namespace Oyung.Save.Adapters
{
    // CLAUDE: adapter: implements ISaveStorage with one file at the path
    // CLAUDE: it is given. Writes a temporary file first and swaps it in,
    // CLAUDE: so a crash or a full disk mid-write can't ruin the old save.
    internal sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _path;

        internal FileSaveStorage(string path) => _path = path;

        public void Write(IReadOnlyDictionary<string,
            IReadOnlyDictionary<string, object>> sections)
        {
            // CLAUDE: composed first: a bad value throws before any file
            // CLAUDE: is touched.
            var text = SaveFileJson.Compose(sections);

            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, text);

            if (File.Exists(_path)) File.Replace(temporary, _path, null);
            else File.Move(temporary, _path);
        }
    }
}
