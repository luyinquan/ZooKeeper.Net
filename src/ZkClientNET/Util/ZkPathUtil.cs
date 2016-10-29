using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZKClientNET.Client;

namespace ZKClientNET.Util
{
    public class ZKPathUtil
    {
        public static string LeadingZeros(long number, int numberOfLeadingZeros)
        {
            return number.ToString().PadLeft(numberOfLeadingZeros, '0');
        }

        public static string ToString(ZKClient zkClient)
        {
            return ToString(zkClient, "/", new PathFilter());
        }

        public static string ToString(ZKClient zkClient, string startPath, IPathFilter pathFilter)
        {
            int level = 1;
            StringBuilder builder = new StringBuilder("+ (" + startPath + ")");
            builder.Append("\n");
            AddChildrenTostringBuilder(zkClient, pathFilter, level, builder, startPath);
            return builder.ToString();
        }

        private static void AddChildrenTostringBuilder(ZKClient zkClient, IPathFilter pathFilter, int level, StringBuilder builder, string startPath)
        {
            List<string> children = zkClient.GetChildren(startPath);
            foreach (string node in children)
            {
                string nestedPath;
                if (startPath.EndsWith("/"))
                {
                    nestedPath = startPath + node;
                }
                else
                {
                    nestedPath = startPath + "/" + node;
                }
                if (pathFilter.ShowChilds(nestedPath))
                {
                    builder.Append(GetSpaces(level - 1) + "'-" + "+" + node + "\n");
                    AddChildrenTostringBuilder(zkClient, pathFilter, level + 1, builder, nestedPath);
                }
                else
                {
                    builder.Append(GetSpaces(level - 1) + "'-" + "-" + node + " (contents hidden)\n");
                }
            }
        }

        private static string GetSpaces(int level)
        {
            string s = "";
            for (int i = 0; i < level; i++)
            {
                s += "  ";
            }
            return s;
        }

        public interface IPathFilter
        {
            bool ShowChilds(string path);
        }

        public class PathFilter : IPathFilter
        {
            public bool ShowChilds(string path)
            {
                return true;
            }
        }

    }
}
