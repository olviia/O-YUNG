using System.Collections.Generic;

namespace Oyung.Save
{
    /// <summary>
    /// Required port: keeps a save somewhere. Save knows THAT data is
    /// stored, not HOW.
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>Stores the sections by name, replacing the previous
        /// save whole. Throws IOException or UnauthorizedAccessException if
        /// the outside world prevents it, leaving the previous save intact.
        /// </summary>
        void Write(IReadOnlyDictionary<string,
            IReadOnlyDictionary<string, object>> sections);
    }
}
