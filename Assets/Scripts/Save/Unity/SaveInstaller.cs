using System;
using System.Collections.Generic;
using System.IO;
using Oyung.Save.Adapters;
using Oyung.SharedKernel;
using UnityEngine;

namespace Oyung.Save.Unity
{
    /// <summary>
    /// Save: needs nothing from other modules. Creates the save system on
    /// a file, offers saving and blocking; the root hands it every module
    /// that keeps state, and asks it to load between create and start.
    /// </summary>
    public class SaveInstaller : MonoBehaviour, IDisposable
    {
        private const string QuickSaveFile = "save_quick.json";

        [SerializeField] private SaveNotice notice;

        [Tooltip("Optional dev tool: Play starts from its test point.")]
        [SerializeField] private TestStart testStart;

        private SaveSystem _system;

        public (ISaveGame saveGame, ISaveBlocker blocker) Build()
        {
            _system = new SaveSystem(new FileSaveStorage(SavePath));
            _system.Blocked += LogBlocked;
            _system.WriteFailed += LogWriteFailed;
            notice.Bind(_system);
            return (_system, _system);
        }

        public void Add(params ISaveable[] saveables)
        {
            foreach (var saveable in saveables) _system.Add(saveable);
        }

        /// <summary>Load phase: gives every module its section from the
        /// test point, if one is picked. Does nothing otherwise.</summary>
        public void Load()
        {
#if UNITY_EDITOR
            if (testStart == null || testStart.TestPoint == null) return;
            var file = testStart.TestPoint;
            var unclaimed = _system.Load(SaveFileJson.Parse(file.text));
            Debug.Log($"Started from test point '{file.name}'.", file);
            foreach (var section in unclaimed)
                Debug.LogWarning(
                    $"Test point '{file.name}' has section '{section}' "
                    + "that no module claims; ignored.", file);
#endif
        }

        public void Dispose()
        {
            notice.Unbind();
            if (_system == null) return;
            _system.Blocked -= LogBlocked;
            _system.WriteFailed -= LogWriteFailed;
            _system = null;
        }

        // CLAUDE: in the editor F5 writes straight into the test points
        // CLAUDE: folder: renaming the file there keeps it as a test point.
        // CLAUDE: The next F5 starts a new save_quick.json.
        private static string SavePath =>
#if UNITY_EDITOR
            Path.Combine(Application.dataPath, "Content", "TestPoints",
                QuickSaveFile);
#else
            Path.Combine(Application.persistentDataPath, QuickSaveFile);
#endif

        private static void LogBlocked(IReadOnlyList<string> reasons) =>
            Debug.Log("Save refused, blocked by: "
                      + string.Join(", ", reasons));

        private static void LogWriteFailed(string why) =>
            Debug.LogError("Save failed: " + why);
    }
}
