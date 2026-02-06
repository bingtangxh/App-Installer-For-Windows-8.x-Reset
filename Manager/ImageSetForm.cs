using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Bridge;

namespace Manager
{
	public partial class ImageSetForm: Form
	{
		public ImageSetForm ()
		{
			InitializeComponent ();
			Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SETIMG_TITLE");
			label1.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SETIMG_CURRSIZE");
			radioButton1.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SETIMG_USEDEF");
			radioButton2.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SETIMG_USEFILE");
			label2.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SETIMG_FILEPATH");
			button1.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_BROWSE");
			label3.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_PREVIEW");
			button2.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SETIMG_SET");
			button3.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_CANCEL");
		}
		private Dictionary<int, Image> defimages = new Dictionary<int, Image> ();
		private void RefreshDefaultImagesSettings ()
		{
			try
			{
				initImgsSizeList.Controls.Clear ();
				foreach (var kv in defimages)
				{
					RadioButton rb = new RadioButton ();
					rb.Text = kv.Key.ToString ();
					rb.CheckedChanged += DefaultImgsRadio_CheckedChanged;
					initImgsSizeList.Controls.Add (rb);
				}
			}
			catch { }
		}
		public Dictionary <int, Image> DefaultImages
		{
			get { return defimages; }
			set { defimages = value; RefreshDefaultImagesSettings (); }
		}
		public int CurrentSize { set { textBox1.Text = value.ToString (); } }
		private void RefreshImagesType ()
		{
			initImgsSizeList.Enabled = radioButton1.Checked;
			textBox2.Enabled = button1.Enabled = radioButton2.Checked;
		}
		private Image finalUse = null;
		private void RefreshImagesPreview ()
		{
			pictureBox1.Image = null;
			try
			{
				if (radioButton1.Checked)
				{
					foreach (RadioButton ctrl in initImgsSizeList.Controls)
					{
						if (ctrl.Checked)
						{
							int value = int.Parse (ctrl.Text);
							pictureBox1.Image = defimages [value];
						}
					}
				}
				else
				{
					try
					{
						pictureBox1.Image = Image.FromFile (textBox2.Text);
					}
					catch { }
				}
			}
			catch { pictureBox1.Image = null; }
			finally
			{
				try
				{
					label4.Text = $"{pictureBox1.Image.Width} x {pictureBox1.Image.Height}";
				}
				catch { label4.Text = ""; }
			}
		}
		private void DefaultImgsRadio_CheckedChanged (object sender, EventArgs e)
		{
			RefreshImagesPreview ();
		}
		private void ImageSetForm_Load (object sender, EventArgs e)
		{
			RefreshImagesType ();
			//RefreshImagesPreview ();
		}
		private void textBox2_TextChanged (object sender, EventArgs e)
		{
			RefreshImagesPreview ();
		}
		private void radioButton1_CheckedChanged (object sender, EventArgs e)
		{
			RefreshImagesType ();
			RefreshImagesPreview ();
		}
		private void radioButton2_CheckedChanged (object sender, EventArgs e)
		{
			RefreshImagesType ();
			RefreshImagesPreview ();
		}
		public Image FinalImage
		{
			set
			{
				pictureBox1.Image = value;
				try
				{
					label4.Text = $"{pictureBox1.Image.Width} x {pictureBox1.Image.Height}";
				}
				catch { label4.Text = ""; }
			}
			get { return finalUse; }
		}
		private void button3_Click (object sender, EventArgs e)
		{
			this.Close ();
		}
		private void button2_Click (object sender, EventArgs e)
		{
			try
			{
				if (radioButton1.Checked)
				{
					foreach (RadioButton ctrl in initImgsSizeList.Controls)
					{
						if (ctrl.Checked)
						{
							int value = int.Parse (ctrl.Text);
							finalUse = defimages [value];
						}
					}
				}
				else
				{
					finalUse = Image.FromFile (textBox2.Text);
				}
				if (finalUse == null) throw new Exception ("Error: none valid image.");
				this.Close ();
			}
			catch (Exception ex)
			{
				MessageBox.Show (ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void ImageSetForm_FormClosing (object sender, FormClosingEventArgs e)
		{
		}
		private void pictureBox1_LoadCompleted (object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			label4.Text = $"{pictureBox1.Image.Width} x {pictureBox1.Image.Height}";
		}
		private void button1_Click (object sender, EventArgs e)
		{
			using (OpenFileDialog ofd = new OpenFileDialog ())
			{
				ofd.Title = "Please select the image file: ";
				ofd.Filter = "Image Files (*.png;*.bmp;*.jpg;*.jpeg)|*.png;*.bmp;*.jpg;*.jpeg";
				ofd.Multiselect = false;
				ofd.CheckFileExists = true;
				ofd.CheckPathExists = true;
				if (ofd.ShowDialog (this) == DialogResult.OK)
				{
					textBox2.Text = ofd.FileName;
					radioButton2.Checked = true;
				}
			}
		}
	}
}
