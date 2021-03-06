﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace Cinteros.Xrm.FetchXmlBuilder.AppCode
{
    class TreeNodeHelper
    {
        /// <summary>
        /// Adds a new TreeNode to the parent object from the XmlNode information
        /// </summary>
        /// <param name="parentObject">Object (TreeNode or TreeView) where to add a new TreeNode</param>
        /// <param name="xmlNode">Xml node from the sitemap</param>
        /// <param name="form">Current application form</param>
        /// <param name="isDisabled"> </param>
        public static TreeNode AddTreeViewNode(object parentObject, XmlNode xmlNode, FetchXmlBuilder form, int index = -1)
        {
            TreeNode node = null;
            if (xmlNode is XmlElement || xmlNode is XmlComment)
            {
                node = new TreeNode(xmlNode.Name);
                node.Name = xmlNode.Name;
                Dictionary<string, string> attributes = new Dictionary<string, string>();

                if (xmlNode.NodeType == XmlNodeType.Comment)
                {
                    attributes.Add("#comment", xmlNode.Value);
                    node.ForeColor = System.Drawing.Color.Gray;
                }
                else if (xmlNode.Attributes != null)
                {
                    foreach (XmlAttribute attr in xmlNode.Attributes)
                    {
                        attributes.Add(attr.Name, attr.Value);
                    }
                }
                if (parentObject is TreeView)
                {
                    ((TreeView)parentObject).Nodes.Add(node);
                }
                else if (parentObject is TreeNode)
                {
                    if (index == -1)
                    {
                        ((TreeNode)parentObject).Nodes.Add(node);
                    }
                    else
                    {
                        ((TreeNode)parentObject).Nodes.Insert(index, node);
                    }
                }
                else
                {
                    throw new Exception("AddTreeViewNode: Unsupported control type");
                }
                node.Tag = attributes;
                AddContextMenu(node, form);
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                {
                    AddTreeViewNode(node, childNode, form);
                }
                SetNodeText(node, FetchXmlBuilder.friendlyNames);
            }
            else if (xmlNode is XmlText && parentObject is TreeNode)
            {
                var treeNode = (TreeNode)parentObject;
                if (treeNode.Tag is Dictionary<string, string>)
                {
                    var attributes = (Dictionary<string, string>)treeNode.Tag;
                    attributes.Add("#text", ((XmlText)xmlNode).Value);
                }
            }
            return node;
        }

        public static void SetNodeText(TreeNode node, bool friendly)
        {
            if (node == null)
            {
                return;
            }
            var text = node.Name;
            Dictionary<string, string> attributes =
                node.Tag is Dictionary<string, string> ?
                    (Dictionary<string, string>)node.Tag :
                    new Dictionary<string, string>();
            var agg = GetAttributeFromNode(node, "aggregate");
            var name = GetAttributeFromNode(node, "name");
            var alias = GetAttributeFromNode(node, "alias");
            switch (node.Name)
            {
                case "fetch":
                    if (attributes.ContainsKey("count"))
                    {
                        text += " count: " + attributes["count"];
                    }
                    if (attributes.ContainsKey("page"))
                    {
                        text += " page: " + attributes["page"];
                    }
                    if (attributes.ContainsKey("returntotalrecordcount") && attributes["returntotalrecordcount"] == "true")
                    {
                        text += " RTRC";
                    }
                    if (attributes.ContainsKey("aggregate") && attributes["aggregate"] == "true")
                    {
                        text += " aggregate";
                    }
                    if (attributes.ContainsKey("distinct") && attributes["distinct"] == "true")
                    {
                        text += " distinct";
                    }
                    break;
                case "entity":
                case "link-entity":
                    text += " " + FetchXmlBuilder.GetEntityDisplayName(name);
                    if (!string.IsNullOrEmpty(alias))
                    {
                        text += " (" + alias + ")";
                    }
                    if (GetAttributeFromNode(node, "intersect") == "true")
                    {
                        text += " M:M";
                    }
                    break;
                case "attribute":
                    if (!string.IsNullOrEmpty(name))
                    {
                        text += " ";
                        if (node.Parent != null)
                        {
                            var parent = GetAttributeFromNode(node.Parent, "name");
                            name = FetchXmlBuilder.GetAttributeDisplayName(parent, name);
                        }
                        if (!string.IsNullOrEmpty(agg) && !string.IsNullOrEmpty(name))
                        {
                            if (!string.IsNullOrEmpty(alias))
                            {
                                text += alias + "=";
                            }
                            text += agg + "(" + name + ")";
                        }
                        else if (!string.IsNullOrEmpty(alias))
                        {
                            text += alias + " (" + name + ")";
                        }
                        else
                        {
                            text += name;
                        }
                        var grp = GetAttributeFromNode(node, "groupby");
                        if (grp == "true")
                        {
                            text += " GRP";
                        }
                    }
                    break;
                case "filter":
                    var type = GetAttributeFromNode(node, "type");
                    if (!string.IsNullOrEmpty(type))
                    {
                        text += " (" + type + ")";
                    }
                    break;
                case "condition":
                    {
                        var ent = GetAttributeFromNode(node, "entityname");
                        var attr = GetAttributeFromNode(node, "attribute");
                        var oper = GetAttributeFromNode(node, "operator");
                        var val = GetAttributeFromNode(node, "value");
                        if (node.Parent != null && node.Parent.Parent != null)
                        {
                            var parent = GetAttributeFromNode(node.Parent.Parent, "name");
                            attr = FetchXmlBuilder.GetAttributeDisplayName(parent, attr);
                        }
                        if (!string.IsNullOrEmpty(ent))
                        {
                            attr = ent + "." + attr;
                        }
                        if (oper.Contains("-x-"))
                        {
                            oper = oper.Replace("-x-", " " + val + " ");
                            val = "";
                        }
                        text += (" " + attr + " " + oper + " " + val).TrimEnd();
                    }
                    break;
                case "value":
                    var value = GetAttributeFromNode(node, "#text");
                    text += " " + value;
                    break;
                case "order":
                    {
                        var attr = GetAttributeFromNode(node, "attribute");
                        var desc = GetAttributeFromNode(node, "descending");
                        if (!string.IsNullOrEmpty(alias))
                        {
                            text += " " + alias;
                        }
                        else if (!string.IsNullOrEmpty(attr))
                        {
                            if (!string.IsNullOrEmpty(attr) && node.Parent != null)
                            {
                                var parent = GetAttributeFromNode(node.Parent, "name");
                                attr = FetchXmlBuilder.GetAttributeDisplayName(parent, attr);
                            }
                            {
                                text += " " + attr;
                            }
                        }
                        if (desc == "true")
                        {
                            text += " Desc";
                        }
                    }
                    break;
                case "#comment":
                    text = GetAttributeFromNode(node, "#comment").Trim().Replace("\r\n", "  ");
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        text = " - comment - ";
                    }
                    break;
            }
            if (friendly && !string.IsNullOrEmpty(text))
            {
                text = text.Substring(0, 1).ToUpper() + text.Substring(1);
            }
            node.Text = text;
            SetNodeTooltip(node);
        }

        internal static void SetNodeTooltip(TreeNode node)
        {
            if (node != null)
            {
                var doc = new XmlDocument();
                XmlNode rootNode = doc.CreateElement("root");
                doc.AppendChild(rootNode);
                TreeNodeHelper.AddXmlNode(node, rootNode);
                var tooltip = "";
                try
                {
                    XDocument xdoc = XDocument.Parse(rootNode.InnerXml);
                    tooltip = xdoc.ToString();
                }
                catch
                {
                    tooltip = rootNode.InnerXml;
                }
                node.ToolTipText = tooltip;
                if (node.Parent != null)
                {
                    SetNodeTooltip(node.Parent);
                }
            }
        }

        /// <summary>Adds a context menu to a TreeNode control</summary>
        /// <param name="node">TreeNode where to add the context menu</param>
        /// <param name="form">Current application form</param>
        public static void AddContextMenu(TreeNode node, FetchXmlBuilder form)
        {
            form.addMenu.Items.Clear();
            form.menuControl.Items.Clear();
            if (node == null && form.tvFetch.Nodes.Count > 0)
            {
                node = form.tvFetch.Nodes[0];
            }
            if (node != null)
            {
                var nodecapabilities = new FetchNodeCapabilities(node);

                foreach (var childcapability in nodecapabilities.ChildTypes)
                {
                    if (childcapability.Name == "-")
                    {
                        form.addMenu.Items.Add(new ToolStripSeparator());
                        //form.menuControl.Items.Add(new ToolStripSeparator());
                    }
                    else if (childcapability.Multiple || !node.Nodes.ContainsKey(childcapability.Name))
                    {
                        AddMenuFromCapability(form.addMenu, childcapability.Name);
                        AddMenuFromCapability(form.menuControl, childcapability.Name, childcapability.Name == "#comment", "Add ");
                    }
                }
                if (form.addMenu.Items.Count == 0)
                {
                    var dummy = form.addMenu.Items.Add("nothing to add");
                    dummy.Enabled = false;
                }

                form.selectAttributesToolStripMenuItem.Visible = nodecapabilities.Attributes;
                form.deleteToolStripMenuItem.Enabled = nodecapabilities.Delete;
                form.commentToolStripMenuItem.Enabled = nodecapabilities.Comment;
                form.uncommentToolStripMenuItem.Enabled = nodecapabilities.Uncomment;

                if (nodecapabilities.Attributes && form.selectAttributesToolStripMenuItem.Enabled)
                {
                    var selattr = new ToolStripMenuItem("Select Attributes");
                    selattr.Tag = "SelectAttributes";
                    form.menuControl.Items.Insert(0, selattr);
                }

                node.ContextMenuStrip = form.nodeMenu;
            }
        }

        private static void AddMenuFromCapability(ToolStrip owner, string name, bool alignright = false, string prefix = "")
        {
            var additem = owner.Items.Add(prefix + name);
            additem.Tag = name;
            if (alignright)
            {
                additem.Alignment = ToolStripItemAlignment.Right;
            }
        }

        /// <summary>
        /// Hides all items from a context menu
        /// </summary>
        /// <param name="cm">Context menu to clean</param>
        public static void HideAllContextMenuItems(ContextMenuStrip cm)
        {
            foreach (ToolStripItem o in cm.Items)
            {
                if (o.Text == "Cut" || o.Text == "Copy" || o.Text == "Paste")
                {
                    o.Enabled = false;
                }
                else if (o.Name == "toolStripSeparatorBeginOfEdition" || o is ToolStripSeparator)
                {
                    o.Visible = true;
                }
                else
                {
                    o.Visible = false;
                }
            }
        }

        /// <summary>Creates xml from given treenode and adds it as child to given xml node</summary>
        /// <param name="currentNode">Tree node from which to build xml</param>
        /// <param name="parentXmlNode">Parent xml node</param>
        internal static void AddXmlNode(TreeNode currentNode, XmlNode parentXmlNode)
        {
            var collec = (Dictionary<string, string>)currentNode.Tag;
            XmlNode newNode;
            if (currentNode.Name == "#comment")
            {
                newNode = parentXmlNode.OwnerDocument.CreateComment(collec.ContainsKey("#comment") ? collec["#comment"] : "");
            }
            else
            {
                newNode = parentXmlNode.OwnerDocument.CreateElement(currentNode.Name);
                foreach (string key in collec.Keys)
                {
                    if (key == "#text")
                    {
                        XmlText newText = parentXmlNode.OwnerDocument.CreateTextNode(collec[key]);
                        newNode.AppendChild(newText);
                    }
                    else
                    {
                        XmlAttribute attr = parentXmlNode.OwnerDocument.CreateAttribute(key);
                        attr.Value = collec[key];
                        newNode.Attributes.Append(attr);
                    }
                }

                var others = new List<TreeNode>();

                foreach (TreeNode childNode in currentNode.Nodes)
                {
                    others.Add(childNode);
                }

                foreach (TreeNode otherNode in others)
                {
                    AddXmlNode(otherNode, newNode);
                }
            }

            parentXmlNode.AppendChild(newNode);
        }

        internal static TreeNode AddChildNode(TreeNode parentNode, string name)
        {
            var childNode = new TreeNode(name);
            childNode.Tag = new Dictionary<string, string>();
            childNode.Name = childNode.Text.Replace(" ", "");
            if (name == "#comment")
            {
                childNode.ForeColor = System.Drawing.Color.Gray;
            }
            if (parentNode != null)
            {
                var parentCap = new FetchNodeCapabilities(parentNode);
                var nodeIndex = parentCap.IndexOfChild(name);
                var pos = 0;
                while (pos < parentNode.Nodes.Count && nodeIndex >= parentCap.IndexOfChild(parentNode.Nodes[pos].Name))
                {
                    pos++;
                }
                if (pos == parentNode.Nodes.Count)
                {
                    parentNode.Nodes.Add(childNode);
                }
                else
                {
                    parentNode.Nodes.Insert(pos, childNode);
                }
            }
            return childNode;
        }

        internal static string GetAttributeFromNode(TreeNode treeNode, string attribute)
        {
            var result = "";
            if (treeNode != null && treeNode.Tag != null && treeNode.Tag is Dictionary<string, string>)
            {
                var collection = (Dictionary<string, string>)treeNode.Tag;
                if (collection.ContainsKey(attribute))
                {
                    result = collection[attribute];
                }
            }
            return result;
        }
    }
}
