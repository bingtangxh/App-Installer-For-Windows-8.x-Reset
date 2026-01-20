using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
namespace Manager
{
	public partial class ManagerShell: WAShell.WebAppForm
	{
		public ManagerShell ()
		{
			InitializeComponent ();
			SplashScreen.SplashBackgroundColor = Color.Honeydew;
		}
		private void ManagerShell_Load (object sender, EventArgs e)
		{
			var root = Path.GetDirectoryName (DataUtils.Utilities.GetCurrentProgramPath ());
			WebUI.Navigate (Path.Combine (root, "html\\manager.html"));
		}
	}
}
