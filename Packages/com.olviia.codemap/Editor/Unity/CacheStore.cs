using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Olviia.CodeMap.Core.Model;
using UnityEngine;

namespace Olviia.CodeMap.Editor
{
    /// <summary>
    /// Saves parsed files between editor sessions and domain reloads, so only changed files are parsed again.
    /// Lives in Library/, which is per machine and never committed.
    /// </summary>
    internal static class CacheStore
    {
        // Bump when the model or parser output changes; an old cache is then ignored and everything is re-parsed.
        private const int Version = 3;
        private const string CachePath = "Library/CodeMap/cache.json";

        /// <summary>Loads the cache.</summary>
        /// <returns>Cached files, or null when there is no usable cache.</returns>
        public static IReadOnlyList<FileEntry> Load()
        {
            if (!File.Exists(CachePath))
                return null;
            try
            {
                CacheData data = JsonUtility.FromJson<CacheData>(File.ReadAllText(CachePath));
                if (data == null || data.version != Version)
                    return null;
                return data.files.Select(ToModel).ToList();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("CodeMap: cache unreadable, rebuilding. " + exception.Message);
                return null;
            }
        }

        /// <summary>Overwrites the cache.</summary>
        /// <param name="files">Current state of all parsed files.</param>
        public static void Save(IEnumerable<FileEntry> files)
        {
            var data = new CacheData { version = Version, files = files.Select(ToData).ToList() };
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
            File.WriteAllText(CachePath, JsonUtility.ToJson(data));
        }

        private static FileData ToData(FileEntry file)
        {
            return new FileData { path = file.Path, types = file.Types.Select(ToData).ToList() };
        }

        private static TypeData ToData(TypeEntry type)
        {
            return new TypeData
            {
                kind = (int)type.Kind, access = (int)type.Access, name = type.Name, baseTypes = type.BaseTypes.ToArray(),
                line = type.Line, doc = ToData(type.Doc), members = type.Members.Select(ToData).ToList()
            };
        }

        private static MemberData ToData(MemberEntry member)
        {
            return new MemberData
            {
                kind = (int)member.Kind, access = (int)member.Access, name = member.Name, parameterTypes = member.ParameterTypes.ToArray(),
                signature = member.Signature, isOverride = member.IsOverride, explicitInterface = member.ExplicitInterface,
                line = member.Line, doc = ToData(member.Doc)
            };
        }

        private static DocData ToData(DocComment doc)
        {
            return new DocData
            {
                summary = doc.Summary, paramNames = doc.Params.Select(p => p.Key).ToArray(), paramTexts = doc.Params.Select(p => p.Value).ToArray(),
                returns = doc.Returns, isInherited = doc.IsInherited
            };
        }

        private static FileEntry ToModel(FileData data)
        {
            return new FileEntry(data.path, data.types.Select(ToModel).ToList());
        }

        private static TypeEntry ToModel(TypeData data)
        {
            return new TypeEntry((TypeKind)data.kind, (Access)data.access, data.name, data.baseTypes, data.line, ToModel(data.doc), data.members.Select(ToModel).ToList());
        }

        private static MemberEntry ToModel(MemberData data)
        {
            return new MemberEntry((MemberKind)data.kind, (Access)data.access, data.name, data.parameterTypes, data.signature,
                data.isOverride, data.explicitInterface, data.line, ToModel(data.doc));
        }

        private static DocComment ToModel(DocData data)
        {
            if (data.summary.Length == 0 && data.returns.Length == 0 && data.paramNames.Length == 0 && !data.isInherited)
                return DocComment.None;
            var parameters = data.paramNames.Select((name, i) => new KeyValuePair<string, string>(name, data.paramTexts[i])).ToList();
            return new DocComment(data.summary, parameters, data.returns, data.isInherited);
        }

        // JsonUtility needs public fields on [Serializable] classes, so the immutable model is copied into these.
        [Serializable] private sealed class CacheData { public int version; public List<FileData> files; }
        [Serializable] private sealed class FileData { public string path; public List<TypeData> types; }
        [Serializable] private sealed class TypeData { public int kind; public int access; public string name; public string[] baseTypes; public int line; public DocData doc; public List<MemberData> members; }
        [Serializable] private sealed class MemberData { public int kind; public int access; public string name; public string[] parameterTypes; public string signature; public bool isOverride; public string explicitInterface; public int line; public DocData doc; }
        [Serializable] private sealed class DocData { public string summary; public string[] paramNames; public string[] paramTexts; public string returns; public bool isInherited; }
    }
}
