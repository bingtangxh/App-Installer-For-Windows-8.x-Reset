using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bridge;
namespace Manager
{
	public partial class ShortcutCreateForm: Form
	{
		public ShortcutCreateForm ()
		{
			InitializeComponent ();
			Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_TITLE");
			label1.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_TITLE");
			label2.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_DESC");
			label3.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_ICONTYPE");
			iconSetGen.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_ICONTYPE_GEN");
			iconSetFromFile.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_ICONTYPE_SEL");
			label4.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_ICONTYPE_SEL_ICONPATH");
			iconFileBrowser.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_BROWSE");
			label7.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_PREVIEW");
			label5.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_BACKGROUNDCOLOR");
			colorPickerButton.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SELECT");
			label6.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_RATIO");
			ratioCustom.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_RATIO_CUSTOM");
			label10.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_RATIO_CUSTOMRATIO");
			label12.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_DISPLAYNAME");
			label8.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_IMAGELIST");
			label9.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_PREVIEW");
			buttonGen.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_GENERATE");
			buttonCancel.Text = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_CANCEL");
			Column1.HeaderText = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_SIZE");
			Column2.HeaderText = ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_IMAGE");
			_imageItems.ListChanged += ImageItems_ListChanged;
			RefreshCustomRatioStatus ();
			RefreshIconSourceMode ();
		}
		private void ImageItems_ListChanged (object sender, ListChangedEventArgs e) { RefreshImagePreview (); }
		void RefreshImagePreview ()
		{
			imagesPreview.SuspendLayout ();
			imagesPreview.Controls.Clear ();
			foreach (var item in _imageItems.OrderBy (i => i.Size))
			{
				var display = new ImageDisplay
				{
					IconSize = item.Size,
					ForegroundImage = item.Image,
					BackgroundColor = GetCurrentBackgroundColor (),
					Ratio = GetCurrentRatio () 
				};
				imagesPreview.Controls.Add (display);
			}
			imagesPreview.ResumeLayout ();
		}
		void UpdateRatio (decimal ratio)
		{
			foreach (ImageDisplay d in imagesPreview.Controls) d.Ratio = ratio;
		}
		void UpdateBackgroundColor (Color color)
		{
			foreach (ImageDisplay d in imagesPreview.Controls) d.BackgroundColor = color;
		}
		public Color GetCurrentBackgroundColor () => DataUtils.UITheme.StringToColor (colorInputAndPreview.Text);
		public decimal GetCurrentRatio ()
		{
			if (ratio8_7.Checked) return (decimal)(8m / 7m);
			if (ratio3_2.Checked) return 1.5m;
			if (ratio1_1.Checked) return 1m;
			try
			{
				if (ratioCustom.Checked)
				{
					decimal l = 0m, r = 0m;
					decimal.TryParse (ratioCustomBack.Text, out l);
					decimal.TryParse (ratioCustomFore.Text, out r);
					return l / r;
				}
			} catch { }
			return 0m;
		}
		class ImageItem
		{
			public int Size { get; set; }
			public Image Image { get; set; }
		}
		private BindingList<ImageItem> _imageItems = new BindingList<ImageItem>();
		Dictionary<int, string> ExtractBest (Dictionary<AppxPackage.PriResourceKey, string> source, int baseSize)
		{
			if (source == null || source.Count == 0) return null;
			var result = new Dictionary<int, string> ();
			foreach (var kv in source)
			{
				if (kv.Key.IsTargetSize &&
					kv.Key.Contrast == AppxPackage.PriResourceKey.PriContrast.None)
				{
					result [kv.Key.Value] = kv.Value;
				}
			}
			if (result.Count > 0) return result;
			foreach (var kv in source)
			{
				if (kv.Key.IsTargetSize)
				{
					result [kv.Key.Value] = kv.Value;
				}
			}
			if (result.Count > 0) return result;
			foreach (var kv in source)
			{
				if (kv.Key.IsScale &&
					kv.Key.Contrast == AppxPackage.PriResourceKey.PriContrast.None)
				{
					int size = (int)(kv.Key.Value * 0.01 * baseSize);
					result [size] = kv.Value;
				}
			}
			if (result.Count > 0)
				return result;
			foreach (var kv in source)
			{
				if (kv.Key.IsScale)
				{
					int size = (int)(kv.Key.Value * 0.01 * baseSize);
					result [size] = kv.Value;
				}
			}
			return result.Count > 0 ? result : null;
		}
		void InitImageList (Dictionary<int, Image> images)
		{
			_imageItems = new BindingList<ImageItem> (
				images.Select (kv => new ImageItem
				{
					Size = kv.Key,
					Image = kv.Value
				}).ToList ()
			);
			imageSizeList.AutoGenerateColumns = false;
			Column1.DataPropertyName = nameof (ImageItem.Size);
			Column2.DataPropertyName = nameof (ImageItem.Image);
			imageSizeList.DataSource = _imageItems;

		}
		private string installLocation = "";
		private string genAppUserId = "";
		private Dictionary<int, Image> _initList = new Dictionary<int, Image> ();
		private void InitInfos ()
		{
			try
			{
				_initList?.Clear ();
				_initList = null;
				using (var m = new AppxPackage.ManifestReader (Path.Combine (installLocation, "AppxManifest.xml")))
				{
					m.EnablePri = false;
					m.UsePri = true;
					AppxPackage.MRApplication app = null;
					string logo_30 = "",
						smallLogo = "",
						logo = "",
						logo_44 = "";
					foreach (var i in m.Applications)
					{
						if (i.UserModelID?.Trim ()?.ToLowerInvariant () == genAppUserId?.Trim ()?.ToLowerInvariant ())
						{
							app = i;
							logo_44 = app ["Square44x44Logo"];
							logo_30 = app ["Square30x30Logo"];
							logo = app ["Logo"];
							smallLogo = app ["SmallLogo"];
							break;
						}
					}
					m.EnablePri = true;
					foreach (var i in m.Applications)
					{
						if (i.UserModelID?.Trim ()?.ToLowerInvariant () == genAppUserId?.Trim ()?.ToLowerInvariant ())
						{
							app = i;
							break;
						}
					}
					this.Invoke ((Action)(() =>
					{
						colorInputAndPreview.Text = app ["BackgroundColor"];
						shortcutNameInput.Text = app ["DisplayName"];
						if (string.IsNullOrWhiteSpace (shortcutNameInput.Text))
							shortcutNameInput.Text = app ["SmallLogo"];
					}));
					Dictionary<AppxPackage.PriResourceKey, string> logo_30list = m.PriFile.ResourceAllValues (logo_30),
						logo_smalllist = m.PriFile.ResourceAllValues (smallLogo),
						logo_list = m.PriFile.ResourceAllValues (logo),
						logo_44list = m.PriFile.ResourceAllValues (logo_44);
					Dictionary<int, string> filteredlist = null;
					filteredlist =
						ExtractBest (logo_44list, 44)
						?? ExtractBest (logo_30list, 30)
						?? ExtractBest (logo_smalllist, 30)
						?? ExtractBest (logo_list, 150);
					Dictionary<int, Image> imageList = new Dictionary<int, Image> ();
					foreach (var kv in filteredlist)
					{
						try
						{
							var imgPath = Path.Combine (installLocation, kv.Value);
							var img = Image.FromFile (imgPath);
							if (img == null) continue;
							imageList [kv.Key] = img;
						}
						catch { }
					}
					_initList = imageList;
					this.Invoke ((Action)(() =>
					{
						InitImageList (imageList);
						RefreshImagePreview ();
						RefreshCustomRatioStatus ();
						RefreshIconSourceMode ();
					}));
				}
			}
			catch (Exception e)
			{
				this.Invoke ((Action)(() => _imageItems.Clear ()));
			}
		}
		private void InitAsync ()
		{
			var loading = new LoadingStatusForm ();
			{
				loading.Show (this);
				loading.Refresh ();
				this.Invoke ((Action)(() => { Enabled = false; }));

				Task.Factory.StartNew (() =>
				{
					try
					{
						InitInfos ();
					}
					catch (Exception ex)
					{
						Invoke ((Action)(() =>
						{
							MessageBox.Show ($"Initialization failed: {ex.Message}");
						}));
					}
					finally
					{
						this.Invoke ((Action)(() =>
						{
							loading.Close ();
							Enabled = true;
						}));
					}
				});
			}
		}
		public string InstallLocation
		{
			get { return installLocation; }
			set { installLocation = value; InitAsync (); }
		}
		public string AppUserModelID
		{
			get { return genAppUserId; }
			set { genAppUserId = value; InitAsync (); }
		}
		public void InitCreater (string inslocation, string appUserId)
		{
			installLocation = inslocation;
			genAppUserId = appUserId;
			InitAsync ();
		}
		private void ShortcutCreateForm_Load (object sender, EventArgs e)
		{

		}
		private void ratio8_7_CheckedChanged (object sender, EventArgs e) { RefreshCustomRatioStatus (); }
		private void ratio3_2_CheckedChanged (object sender, EventArgs e) { RefreshCustomRatioStatus (); }
		private void ratio1_1_CheckedChanged (object sender, EventArgs e) { RefreshCustomRatioStatus (); }
		private void ratioCustom_CheckedChanged (object sender, EventArgs e) { RefreshCustomRatioStatus (); }
		private void RefreshCustomRatioStatus ()
		{
			ratioCustomBack.Enabled = ratioCustomFore.Enabled = ratioCustom.Checked;
			UpdateRatio (GetCurrentRatio ());
		}
		private void iconSetGen_CheckedChanged (object sender, EventArgs e) { RefreshIconSourceMode (); }
		private void iconSetFromFile_CheckedChanged (object sender, EventArgs e) { RefreshIconSourceMode (); }
		private void RefreshIconSourceMode ()
		{
			iconFileInput.Enabled = iconFileBrowser.Enabled = iconSetFromFile.Checked;
			colorInputAndPreview.Enabled = colorPickerButton.Enabled =
				ratio8_7.Enabled = ratio3_2.Enabled = ratio1_1.Enabled =
				ratioCustom.Enabled = ratioCustomBack.Enabled = ratioCustomFore.Enabled =
				iconSetGen.Checked;
			if (iconSetGen.Checked)
			{
				RefreshCustomRatioStatus ();
			}
			if (iconSetFromFile.Checked)
			{
				try
				{
					customIconDisplay.Image = new Icon (iconFileInput.Text)?.ToBitmap ();
				}
				catch
				{
					customIconDisplay.Image = null;
				}
			}
			else customIconDisplay.Image = null;
		}
		private void colorInputAndPreview_TextChanged (object sender, EventArgs e)
		{
			try
			{
				Color nowColor = DataUtils.UITheme.StringToColor (colorInputAndPreview.Text);
				double luminance = nowColor.R * 0.299 + nowColor.G * 0.587 + nowColor.B * 0.114;
				Color foreground = luminance < 128 ? Color.White : Color.Black;
				colorInputAndPreview.BackColor = nowColor;
				colorInputAndPreview.ForeColor = foreground;
				UpdateBackgroundColor (nowColor);
			}
			catch { }
		}
		private Bitmap GenerateIconBitmap (int size, Image foreground, Color background, decimal ratio)
		{
			var bmp = new Bitmap (size, size);
			using (var g = Graphics.FromImage (bmp))
			{
				g.Clear (background);
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
				float foreSize = (float)(size / ratio);
				float x = (size - foreSize) / 2f;
				float y = (size - foreSize) / 2f;
				var destRect = new RectangleF (x, y, foreSize, foreSize);
				g.DrawImage (foreground, destRect);
			}
			return bmp;
		}
		Dictionary<int, Image> GenerateAllIconImages ()
		{
			var result = new Dictionary<int, Image> ();
			Color bg = GetCurrentBackgroundColor ();
			decimal ratio = GetCurrentRatio ();
			foreach (var item in _imageItems)
			{
				var bmp = GenerateIconBitmap (item.Size, item.Image, bg, ratio);
				result [item.Size] = bmp;
			}
			return result;
		}
		public static void SaveAsIcon (Dictionary<int, Image> images, string filePath)
		{
			using (var fs = new FileStream (filePath, FileMode.Create))
			using (var bw = new BinaryWriter (fs))
			{
				bw.Write ((short)0);   // reserved
				bw.Write ((short)1);   // type = icon
				bw.Write ((short)images.Count);
				int offset = 6 + (16 * images.Count);
				var imageData = new List<byte []> ();
				foreach (var kv in images.OrderBy (i => i.Key))
				{
					using (var ms = new MemoryStream ())
					{
						kv.Value.Save (ms, System.Drawing.Imaging.ImageFormat.Png);
						byte [] data = ms.ToArray ();
						imageData.Add (data);
						bw.Write ((byte)(kv.Key >= 256 ? 0 : kv.Key)); // width
						bw.Write ((byte)(kv.Key >= 256 ? 0 : kv.Key)); // height
						bw.Write ((byte)0); // color count
						bw.Write ((byte)0); // reserved
						bw.Write ((short)1); // planes
						bw.Write ((short)32); // bit count
						bw.Write (data.Length);
						bw.Write (offset);
						offset += data.Length;
					}
				}
				foreach (var data in imageData) bw.Write (data);
			}
		}
		private void iconFileInput_TextChanged (object sender, EventArgs e)
		{
			try
			{
				customIconDisplay.Image = new Icon (iconFileInput.Text)?.ToBitmap ();
			}
			catch
			{
				customIconDisplay.Image = null;
			}
		}
		private void imageSizeList_CellDoubleClick (object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;
			if (imageSizeList.Columns [e.ColumnIndex] != Column2) return;
			var item = _imageItems [e.RowIndex];
			var isf = new ImageSetForm ();
			isf.CurrentSize = item.Size;
			isf.DefaultImages = _initList;
			isf.FinalImage = item.Image;
			isf.ShowDialog (this);
			var newimg = isf.FinalImage;
			if (newimg != null)
			{
				item.Image = newimg;
				RefreshImagePreview ();
			}
		}
		public bool IsSuccess { get; private set; } = false;
		public string Message { get; private set; } = "";
		private void buttonCancel_Click (object sender, EventArgs e)
		{
			IsSuccess = false;
			Message = "User canceled.";
			this.Close ();
		}
		private void ShortcutCreateForm_FormClosed (object sender, FormClosedEventArgs e)
		{
			_imageItems?.Clear ();
			_imageItems = null;
			_initList?.Clear ();
			_initList = null;
		}
		public static bool IsValidFileName (string fileName, bool requireExtension = true)
		{
			if (string.IsNullOrWhiteSpace (fileName)) return false;
			if (fileName.IndexOfAny (Path.GetInvalidFileNameChars ()) >= 0) return false;
			if (fileName.EndsWith (" ") || fileName.EndsWith (".")) return false;
			if (requireExtension)
			{
				if (!Path.HasExtension (fileName)) return false;
				string ext = Path.GetExtension (fileName);
				if (string.IsNullOrWhiteSpace (ext) || ext == ".") return false;
			}
			string nameWithoutExtension = Path.GetFileNameWithoutExtension (fileName);
			string [] reservedNames =
			{
				"CON", "PRN", "AUX", "NUL",
				"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
				"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
			};
			if (reservedNames.Contains (nameWithoutExtension.ToUpper ())) return false;
			if (fileName.Length > 255) return false;
			return true;
		}
		private void buttonGen_Click (object sender, EventArgs e)
		{
			try
			{
				if (!IsValidFileName (shortcutNameInput.Text, false))
				{
					MessageBox.Show (this, "Invalid shortcut name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				var iconfilename = "";
				var iconfilepath = "";
				if (iconSetGen.Checked)
				{
					iconfilename = genAppUserId.Replace ('!', '-') + ".ico";
					iconfilepath = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "icons", iconfilename);
					if (File.Exists (iconfilepath))
					{
						#region gen twice;
						var dlgres = MessageBox.Show (
							this,
							ResXmlStore.StringRes.Get ("MANAGER_APP_SHORTCUTCREATE_ASK_ICONEXISTS"),
							"Ask",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question
						);
						if (dlgres == DialogResult.Yes)
						{
							try
							{
								iconfilename = genAppUserId.Replace ('!', '-') + "-" + DateTime.Now.GetHashCode () + ".ico";
								iconfilepath = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "icons", iconfilename);
								var icons = GenerateAllIconImages ();
								SaveAsIcon (icons, iconfilepath);
							}
							catch (Exception ex)
							{
								MessageBox.Show (this, "Cannot create icon, we will fallback. Message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
								iconfilename = genAppUserId.Replace ('!', '-') + ".ico";
								iconfilepath = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "icons", iconfilename);
							}
						}
						#endregion
					}
					else
					{
						var icons = GenerateAllIconImages ();
						SaveAsIcon (icons, iconfilepath);
					}
				}
				else
				{
					iconfilepath = iconFileInput.Text;
				}
				var shortcutname = shortcutNameInput.Text + ".lnk";
				var shortcutpath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Desktop), shortcutname);
				ShortcutHelper.CreateShortcut (
					shortcutpath,
					Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "Launch.exe"),
					genAppUserId,
					null,
					iconfilepath,
					shortcutNameInput.Text,
					genAppUserId
				);
				IsSuccess = true;
				Message = "";
				this.Close ();
			}
			catch (Exception ex)
			{
				IsSuccess = false;
				Message = ex.Message;
				this.Close ();
			}
		}
		private void ShortcutCreateForm_FormClosing (object sender, FormClosingEventArgs e)
		{
			if (!IsSuccess && string.IsNullOrWhiteSpace (Message))
			{
				IsSuccess = false;
				Message = "User canceled.";
			}
		}

		private void colorPickerButton_Click (object sender, EventArgs e)
		{
			using (var colorpicker = new ColorDialog ())
			{
				colorpicker.Color = GetCurrentBackgroundColor ();
				var dlgres = colorpicker.ShowDialog (this);
				if (dlgres == DialogResult.OK)
				{
					colorInputAndPreview.Text = DataUtils.UITheme.ColorToHtml (colorpicker.Color);
				}
			}
		}

		private void iconFileBrowser_Click (object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog ())
			{
				dlg.Title = "Please select the icon file:";
				dlg.Filter = "Icon File (*.ico)|*.ico";
				dlg.CheckFileExists = true;
				dlg.CheckPathExists = true;
				dlg.Multiselect = false;
				dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
				if (dlg.ShowDialog () == DialogResult.OK)
				{
					iconFileInput.Text = dlg.FileName;
				}
			}
		}
	}
}
