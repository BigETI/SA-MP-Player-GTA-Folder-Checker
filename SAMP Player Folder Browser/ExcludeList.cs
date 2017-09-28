using System.Collections.Generic;
using System.IO;

/// <summary>
/// SAMP_Player_Folder_Browser namespace
/// </summary>
namespace SAMP_Player_Folder_Browser
{
    /// <summary>
    /// Exclude list class
    /// </summary>
    public class ExcludeList
    {
        /// <summary>
        /// Exclusions
        /// </summary>
        private string[] exclusions = new string[0];

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="excludeListPath">Exclude list path</param>
        public ExcludeList(string excludeListPath)
        {
            if (File.Exists(excludeListPath))
            {
                using (StreamReader reader = new StreamReader(excludeListPath))
                {
                    List<string> exclusions = new List<string>();
                    while (reader.Peek() >= 0)
                    {
                        string line = reader.ReadLine();
                        if (line.Length > 0)
                            exclusions.Add(line);
                    }
                    this.exclusions = exclusions.ToArray();
                }
            }
        }

        /// <summary>
        /// Filter results
        /// </summary>
        /// <param name="arr">Array</param>
        /// <returns>Result</returns>
        public string[] Filter(string[] arr)
        {
            List<string> ret = new List<string>();
            foreach (string item in arr)
            {
                bool allow = true;
                foreach (string exclusion in exclusions)
                {
                    if (item.Trim().ToLower().Contains(exclusion.Trim().ToLower()))
                    {
                        allow = false;
                        break;
                    }
                }
                if (allow)
                    ret.Add(item);
            }
            return ret.ToArray();
        }
    }
}
