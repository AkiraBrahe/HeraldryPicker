using HBS.Logging;
using UnityEngine;

namespace HeraldryPicker.Utils
{
    internal class HierarchyLogger
    {
        /// <summary>
        /// Logs the hierarchy of GameObjects for debugging purposes.
        /// </summary>
        internal static readonly ILog Log = HBS.Logging.Logger.GetLogger("Unity", LogLevel.Debug);
        public static void LogHierarchy(GameObject root, string prefix = "")
        {
            if (root == null)
            {
                Log.LogDebug(prefix + "(null)");
                return;
            }
            Log.LogDebug(prefix + root.name);
            foreach (Transform child in root.transform)
            {
                LogHierarchy(child.gameObject, prefix + "  ");
            }
        }
    }
}