using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace PriFileFormat
{
	/// <summary>
	/// 树节点接口
	/// </summary>
	public interface ITreeNode: IDisposable, IEnumerable<ITreeNode>
	{
		string Name { get; set; }
		string Value { get; set; }
		ITreeNode Parent { get; set; }
		IList<ITreeNode> Children { get; }
		ITreeNode AddChild (string name, string value);
		IEnumerable<ITreeNode> DescendantsAndSelf ();
		ITreeNode Find (string name);
		IEnumerable<ITreeNode> FindAll (string name);
	}
	/// <summary>
	/// 树节点实现
	/// </summary>
	[Serializable]
	public class TreeNode: ITreeNode
	{
		// 节点基本属性
		public string Name { get; set; }
		public string Value { get; set; }
		// 父节点引用
		[XmlIgnore]
		public TreeNode Parent { get; set; }
		ITreeNode ITreeNode.Parent
		{
			get { return Parent; }
			set { Parent = (TreeNode)value; }
		}
		// 子节点列表
		[XmlArray ("Children")]
		[XmlArrayItem ("Node")]
		public IList<TreeNode> Children { get; set; }
		IList<ITreeNode> ITreeNode.Children
		{
			get
			{
				List<ITreeNode> list = new List<ITreeNode> ();
				foreach (TreeNode node in Children)
				{
					list.Add (node);
				}
				return list;
			}
		}
		public TreeNode ()
		{
			Name = "";
			Value = "";
			Children = new List<TreeNode> ();
			Parent = null;
		}
		public TreeNode (string name, string value)
		{
			Name = name;
			Value = value;
			Children = new List<TreeNode> ();
			Parent = null;
		}
		/// <summary>
		/// 添加子节点
		/// </summary>
		public TreeNode AddChild (string name, string value)
		{
			TreeNode child = new TreeNode (name, value);
			child.Parent = this;
			Children.Add (child);
			return child;
		}
		ITreeNode ITreeNode.AddChild (string name, string value)
		{
			return AddChild (name, value);
		}
		/// <summary>
		/// 深度优先遍历节点，包括自身
		/// </summary>
		public IEnumerable<TreeNode> DescendantsAndSelf ()
		{
			yield return this;
			foreach (TreeNode child in Children)
			{
				foreach (TreeNode desc in child.DescendantsAndSelf ())
				{
					yield return desc;
				}
			}
		}
		IEnumerable<ITreeNode> ITreeNode.DescendantsAndSelf ()
		{
			foreach (TreeNode n in DescendantsAndSelf ())
			{
				yield return n;
			}
		}
		/// <summary>
		/// 查找第一个匹配节点
		/// </summary>
		public TreeNode Find (string name)
		{
			foreach (TreeNode n in DescendantsAndSelf ())
			{
				if (n.Name == name)
					return n;
			}
			return null;
		}
		ITreeNode ITreeNode.Find (string name)
		{
			return Find (name);
		}
		/// <summary>
		/// 查找所有匹配节点
		/// </summary>
		public IEnumerable<TreeNode> FindAll (string name)
		{
			foreach (TreeNode n in DescendantsAndSelf ())
			{
				if (n.Name == name)
					yield return n;
			}
		}
		IEnumerable<ITreeNode> ITreeNode.FindAll (string name)
		{
			foreach (TreeNode n in FindAll (name))
			{
				yield return n;
			}
		}
		#region IEnumerable<TreeNode>
		public IEnumerator<TreeNode> GetEnumerator ()
		{
			return Children.GetEnumerator ();
		}
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		#endregion
		#region IEnumerable<ITreeNode> 显示实现
		IEnumerator<ITreeNode> IEnumerable<ITreeNode>.GetEnumerator ()
		{
			foreach (TreeNode child in Children)
			{
				yield return child;
			}
		}
		#endregion
		#region XML 序列化
		public void SaveToXml (string filePath)
		{
			XmlSerializer serializer = new XmlSerializer (typeof (TreeNode));
			using (StreamWriter writer = new StreamWriter (filePath, false, System.Text.Encoding.UTF8))
			{
				serializer.Serialize (writer, this);
			}
		}

		public static TreeNode LoadFromXml (string filePath)
		{
			XmlSerializer serializer = new XmlSerializer (typeof (TreeNode));
			using (StreamReader reader = new StreamReader (filePath, System.Text.Encoding.UTF8))
			{
				TreeNode root = (TreeNode)serializer.Deserialize (reader);
				SetParentRecursive (root, null);
				return root;
			}
		}

		private static void SetParentRecursive (TreeNode node, TreeNode parent)
		{
			node.Parent = parent;
			foreach (TreeNode child in node.Children)
			{
				SetParentRecursive (child, node);
			}
		}
		#endregion
		#region IDisposable
		public virtual void Dispose ()
		{
			foreach (TreeNode child in Children)
			{
				child.Dispose ();
			}
			Children.Clear ();
			Parent = null;
			Children = null;
		}
		#endregion
	}
	/// <summary>
	/// 树根节点
	/// </summary>
	[Serializable]
	public class TreeRoot: TreeNode
	{
		public TreeRoot ()
			: base ("Root", "")
		{
		}
	}
}
