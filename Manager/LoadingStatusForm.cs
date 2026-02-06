using System.Windows.Forms;
using Bridge;

namespace Manager
{
	public partial class LoadingStatusForm: Form
	{
		public LoadingStatusForm ()
		{
			InitializeComponent ();
			label1.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_LOADING");
		}
		public string TipText
		{
			get { return label1.Text; }
			set { label1.Text = value; }
		}
	}
}
