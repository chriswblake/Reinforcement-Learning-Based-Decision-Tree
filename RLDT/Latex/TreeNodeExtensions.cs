using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT.DecisionTree.Latex
{
    public static class TreeNodeExtensions
    {
        /// <summary>
        /// Converts this tree node to a simple text tabbed list.
        /// </summary>
        /// <returns></returns>
        public static string ToLatexForest(this TreeNode treeNode)
        {
            string s = "";
            s += @"\begin{figure}[H]" + "\n";
            s += @"    \centering" + "\n";
            s += @"    \small" + "\n";
            s += @"    \begin{forest}" + "\n";
            s += @"        leaf/.style={fill={leaffill},draw={leafborder,thick},align=center,base=top}" + "\n";
            s += ToLatexForest(treeNode, "    " + "    ");
            s += @"    \end{forest}" + "\n";
            s += @"    \caption{CAPTION}" + "\n";
            s += @"    \label{fig:FIGURE_NAME}" + "\n";
            s += @"\end{figure}";
            return s;
        }

        /// <summary>
        /// The recursive process of cycling through nodes, to convert to a simple text tabbed list.
        /// </summary>
        /// <param name="groupNode"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static string ToLatexForest(TreeNode groupNode, string offset)
        {
            //Check for root node. Skip if if not needed.
            if (groupNode.Type == TreeNodeType.Root && groupNode.SubNodes.Count == 1 && groupNode.Leaves.Count == 0)
                return ToLatexForest(groupNode.SubNodes[0], offset);

            string s = "";

            s += offset + string.Format("[{0}, ", groupNode.Name);
            if (groupNode.SubNodes.Count + groupNode.Leaves.Count > 1)
                s += "\n";

            //Show subnodes
            if (groupNode.SubNodes.Count > 0)
            {
                foreach (var subnode in groupNode.SubNodes)
                    s += ToLatexForest(subnode, offset + "    ");
            }

            //Show leaves
            if (groupNode.Leaves.Count > 0)
            {
                //Combine leaves into one node for displaying in tree
                string leafText = "";
                foreach (var leafItem in groupNode.Leaves)
                {
                    //Add latex new-line after first entry
                    if (leafText.Length > 0)
                        leafText += @"\\";

                    if (groupNode.Leaves.Count == 1)
                    {
                        //Only show the label, since there is only one leaf
                        leafText += leafItem.LabelValue;
                    }
                    else
                    { 
                        //Since multiple leaves, show the label and expected reward
                        leafText += string.Format("{0}:{1:N3}", leafItem.LabelValue, leafItem.ExpectedReward);
                    }
                }

                //Create the leaf node.
                if (groupNode.Leaves.Count == 1)
                { 
                    //If only one sub-item, close on the same line.
                    s += string.Format("[{0}, leaf]", leafText);
                }
                else
                { 
                    //If multiple sub-items under this node, close and move to next line
                    s += offset + "   " + string.Format("[{0}, leaf]\n", leafText);
                }
            }

            //Close group on this line or next line.
            string endOffset = "";
            if (groupNode.SubNodes.Count + groupNode.Leaves.Count > 1)
                endOffset = offset;
            s += endOffset + "]\n";

            return s;
        }

    }
}
