﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreneticDataSyntax
{
    /// <summary>
    /// Represents a FreneticDataSyntax section or file.
    /// </summary>
    public class FDSSection
    {
        /// <summary>
        /// Constructs the FDS Section from textual data.
        /// </summary>
        /// <param name="contents">The contents of the data file.</param>
        public FDSSection(string contents)
        {
            StartingLine = 1;
            contents = FDSUtility.CleanFileData(contents);
            Dictionary<int, FDSSection> spacedsections = new Dictionary<int, FDSSection>();
            spacedsections[0] = this;
            List<string> ccomments = new List<string>();
            List<string> seccomments = new List<string>();
            FDSSection csection = this;
            string[] data = contents.SplitFast('\n');
            int pspaces = 0;
            string secwaiting = null;
            for (int i = 0; i < data.Length; i++)
            {
                string line = data[i];
                int spaces;
                for (spaces = 0; spaces < line.Length; spaces++)
                {
                    if (line[spaces] != ' ')
                    {
                        break;
                    }
                }
                if (spaces == line.Length)
                {
                    continue;
                }
                string datum = line.Substring(spaces).TrimEnd(' ');
                if (datum.StartsWith("#"))
                {
                    ccomments.Add(datum.Substring(1));
                    continue;
                }
                if (spaces < pspaces)
                {
                    FDSSection temp;
                    if (spacedsections.TryGetValue(spaces, out temp))
                    {
                        csection = temp;
                        foreach (int test in new List<int>(spacedsections.Keys))
                        {
                            if (test > spaces)
                            {
                                spacedsections.Remove(test);
                            }
                        }
                    }
                    else
                    {
                        Exception(i, line, "Spaced incorrectly. Spacing length is less than previous spacing length, but does not match the spacing value of any known section");
                    }
                }
                string startofline = "";
                string endofline = "";
                char type = '\0';
                for (int spot = 0; spot < datum.Length; spot++)
                {
                    if (datum[spot] == ':' || datum[spot] == '=')
                    {
                        type = datum[spot];
                        startofline = datum.Substring(0, spot);
                        endofline = spot == datum.Length - 1 ? "": datum.Substring(spot + 1);
                        break;
                    }
                }
                endofline = endofline.TrimStart(' ');
                if (type == '\0')
                {
                    Exception(i, line, "Line purpose unknown");
                }
                if (startofline.Length == 0)
                {
                    Exception(i, line, "Empty key label!");
                }
                if (spaces > pspaces && secwaiting != null)
                {
                    FDSSection sect = new FDSSection();
                    csection.SetRootData(FDSUtility.UnEscapeKey(secwaiting), new FDSData() { PrecedingComments = new List<string>(seccomments), Internal = sect });
                    seccomments.Clear();
                    csection = sect;
                    spacedsections[spaces] = sect;
                    secwaiting = null;
                }
                if (type == '=')
                {
                    if (endofline.Length == 0)
                    {
                        csection.SetRootData(FDSUtility.UnEscapeKey(startofline), new FDSData() { PrecedingComments = new List<string>(ccomments), Internal = new byte[0] });
                    }
                    else
                    {
                        csection.SetRootData(FDSUtility.UnEscapeKey(startofline), new FDSData() { PrecedingComments = new List<string>(ccomments), Internal = Convert.FromBase64String(endofline) });
                    }
                    ccomments.Clear();
                }
                else if (type == ':')
                {
                    if (endofline.Length == 0)
                    {
                        secwaiting = startofline;
                        seccomments = new List<string>(ccomments);
                        ccomments.Clear();
                    }
                    else
                    {
                        csection.SetRootData(FDSUtility.UnEscapeKey(startofline), new FDSData() { PrecedingComments = new List<string>(ccomments), Internal = FDSUtility.InterpretType(endofline) });
                        ccomments.Clear();
                    }
                }
                else
                {
                    Exception(i, line, "Internal issue: unrecognize 'type' value: " + type);
                }
                pspaces = spaces;
            }
        }

        private void Exception(int linenumber, string line, string reason)
        {
            throw new Exception("[FDS Parsing error] Line " + (linenumber + 1) + ": " + reason + ", from line as follows: `" + line + "`");
        }

        /// <summary>
        /// Constructs the FDS section from no data, preparing it for usage as a new section.
        /// </summary>
        public FDSSection()
        {
            // Do nothing, we're init'd enough!
        }

        /// <summary>
        /// The line number this section starts on.
        /// Note that files start at 1.
        /// Only accurate at file-load time.
        /// </summary>
        public int StartingLine = 0;

        /// <summary>
        /// All data contained by this section.
        /// </summary>
        public Dictionary<string, FDSData> Data = new Dictionary<string, FDSData>();

        /// <summary>
        /// Lowercase-stored data for this section.
        /// </summary>
        public Dictionary<string, FDSData> DataLowered = new Dictionary<string, FDSData>();

        /// <summary>
        /// Sets data direct on the root level.
        /// </summary>
        /// <param name="key">The key to set data to.</param>
        /// <param name="data">The data to read.</param>
        public void SetRootData(string key, FDSData data)
        {
            Data[key] = data;
            DataLowered[key.ToLowerFast()] = data;
        }

        /// <summary>
        /// Converts this FDSSection to a textual representation of itself.
        /// </summary>
        /// <param name="spacing">How many spaces to start with. Generally do not set this.</param>
        /// <param name="newline">What string to use as a new line. Generally do not set this.</param>
        /// <returns>The string.</returns>
        public string SaveToString(int spacing = 0, string newline = null)
        {
            if (newline == null)
            {
                newline = Environment.NewLine;
            }
            StringBuilder sb = new StringBuilder();
            foreach (string key in Data.Keys)
            {
                FDSData dat = Data[key];
                foreach (string str in dat.PrecedingComments)
                {
                    FDSUtility.AppendSpaces(sb, spacing);
                    sb.Append("#").Append(str).Append(newline);
                }
                FDSUtility.AppendSpaces(sb, spacing);
                sb.Append(FDSUtility.EscapeKey(key));
                if (dat.Internal is FDSSection)
                {
                    sb.Append(":").Append(newline).Append(((FDSSection)dat.Internal).SaveToString(spacing + 4, newline));
                }
                else if (dat.Internal is byte[])
                {
                    sb.Append("= ").Append(dat.Outputable()).Append(newline);
                }
                else
                {
                    sb.Append(": ").Append(FDSUtility.Escape(dat.Outputable())).Append(newline);
                }
            }
            return sb.ToString();
        }
    }
}