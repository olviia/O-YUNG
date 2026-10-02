using System;

namespace Olviia.CodeMap.Core.Modules
{
    /// <summary>
    /// Hexagonal folder convention: a module's ports live in <c>Ports/&lt;Role&gt;/</c>, e.g. <c>Ports/Offered/IVision.cs</c>.
    /// Offered ports are the module's API; any other role (Driven, Required) is something the module needs from outside.
    /// </summary>
    public static class PortConvention
    {
        private const string PortsFolder = "Ports";
        private const string OfferedRole = "Offered";

        /// <summary>Reads the port role from a file path.</summary>
        /// <param name="path">Path of the declaring file; relative or project-relative.</param>
        /// <returns>The folder name after <c>Ports/</c>, e.g. <c>Offered</c>; empty when the file is not a port.</returns>
        public static string RoleOf(string path)
        {
            string[] segments = path.Replace('\\', '/').Split('/');
            // The last segment is the file name, so a role folder must come before it.
            for (int i = 0; i < segments.Length - 2; i++)
            {
                if (string.Equals(segments[i], PortsFolder, StringComparison.OrdinalIgnoreCase))
                    return segments[i + 1];
            }
            return string.Empty;
        }

        /// <summary>Whether a role means the module offers this port to others.</summary>
        /// <param name="role">Role from <see cref="RoleOf"/>.</param>
        /// <returns>True for <c>Offered</c>.</returns>
        public static bool IsOffered(string role)
        {
            return string.Equals(role, OfferedRole, StringComparison.OrdinalIgnoreCase);
        }
    }
}
