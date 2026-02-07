using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WAShell
{
	public partial class NormalForm: Form, IMetroIconSupport
	{
		public NormalForm ()
		{
			InitializeComponent ();
		}
		private Icon _iconForMetro = null;
		public virtual Icon WindowIcon
		{
			get { return _iconForMetro; }
			set { _iconForMetro = value; }
		}
	}
}
