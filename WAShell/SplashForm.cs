using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DataUtils;
namespace WAShell
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public partial class SplashForm: Form
	{
		public enum FadeType
		{
			Gradually,
			Immediately
		};
		private Image splashImage = null;
		private Color background = Color.Transparent;
		private FadeType fadeMode = FadeType.Gradually;
		double opastep = 0.05;
		private Control _host = null;
		public Control Host
		{
			get { return _host; }
			set
			{
				if (ReferenceEquals (_host, value)) return;
				DetachHostEvents (_host);
				_host = value;
				this.Owner = _host as Form;
				AttachHostEvents (_host);
				if (this.Visible)
				{
					ResizeSplashScreen ();
				}
			}
		}
		private void AttachHostEvents (Control host)
		{
			if (host == null) return;
			host.Resize += Host_Changed;
			host.LocationChanged += Host_Changed;
			host.Disposed += Host_Disposed;
		}
		private void DetachHostEvents (Control host)
		{
			if (host == null) return;
			host.Resize -= Host_Changed;
			host.LocationChanged -= Host_Changed;
			host.Disposed -= Host_Disposed;
		}
		private void Host_Changed (object sender, EventArgs e)
		{
			if (this.IsDisposed || !this.IsHandleCreated) return;
			if (this.Owner == null || this.Owner.IsDisposed) return;
			ResizeSplashScreen ();
		}
		private void Host_Disposed (object sender, EventArgs e)
		{
			DetachHostEvents (_host);
			_host = null;
			if (!this.IsDisposed) this.Hide ();   // 或 Hide()
		}
		public SplashForm ()
		{
			InitializeComponent ();
			Init ();
		}
		private void Init ()
		{
			this.AllowTransparency = true;
			picbox.Size = new Size (
				(int)(620 * UITheme.DPIDouble),
				(int)(300 * UITheme.DPIDouble) 
			);
			try { picbox.Image = splashImage; } catch (Exception) { }
			try { this.BackColor = background; } catch (Exception) { }
		}
		public Image SplashImage
		{
			get { return splashImage; }
			set
			{
				splashImage = value;
				if (picbox != null)
				{
					try { picbox.Image = splashImage; } catch { }
				}
			}
		}
		public Color SplashBackgroundColor
		{
			get { try { return background = this.BackColor; } catch (Exception) { return background; } }
			set
			{
				background = value;
				try { this.BackColor = background; }
				catch (Exception) { background = this.BackColor; }
			}
		}
		private void SplashForm_Load (object sender, EventArgs e)
		{
			ResizeSplashScreen ();
		}
		public void ResizeSplashScreen ()
		{
			if (this.IsDisposed || !this.IsHandleCreated) return;
			Control owner = this.Owner ?? this.Parent;
			if (owner == null || owner.IsDisposed || !owner.IsHandleCreated) return;
			var pt = owner.PointToScreen (Point.Empty);
			this.Location = pt;
			this.Size = owner.ClientSize;
			ResizeSplashImage ();
		}
		private void ResizeSplashImage ()
		{
			if (picbox != null && picbox.IsHandleCreated)
			{
				var sz = this.ClientSize;
				picbox.Location = new Point (
					(int)((sz.Width - picbox.Width) * 0.5),
					(int)((sz.Height - picbox.Height) * 0.5)
				);
			}
		}
		private void SplashForm_Resize (object sender, EventArgs e)
		{
			ResizeSplashImage ();
		}
		private bool fadeAwayArmed = false;   // 用于 FadeAway 的“第一次 / 第二次”
		private bool fading = false;          // 防止重复启动 Timer
		protected virtual void OnFormFade ()
		{
			switch (fadeMode)
			{
				case FadeType.Gradually:
					if (this.Opacity > 0)
					{
						this.Opacity -= opastep;
						if (this.Opacity < 0) this.Opacity = 0;
					}
					else
					{
						timer.Stop ();
						this.Visible = false;
						fading = false;
					}
					break;

				case FadeType.Immediately:
					if (fadeAwayArmed)
					{
						this.Opacity = 0;
						timer.Stop ();
						this.Visible = false;
						fading = false;
					}
					else
					{
						fadeAwayArmed = true;
					}
					break;
			}
		}
		private void timer_Tick (object sender, EventArgs e)
		{
			OnFormFade ();
		}
		public void OwnerWnd_Resize (object sender, EventArgs e)
		{
			if (this != null && this.IsHandleCreated && this.picbox != null && this.picbox.IsHandleCreated)
			{
				ResizeSplashScreen ();
			}
		}
		// 渐变消失
		public void FadeOut ()
		{
			if (fading) return;
			fadeMode = FadeType.Gradually;
			fadeAwayArmed = false;
			fading = true;
			this.Opacity = Math.Min (this.Opacity, 1.0);
			timer.Interval = 15;
			timer.Start ();
		}
		// 立即消失
		public void FadeAway ()
		{
			if (fading) return;
			fadeMode = FadeType.Immediately;
			fadeAwayArmed = false;
			fading = true;
			timer.Interval = 15;
			timer.Start ();
		}
		public void ResetSplash ()
		{
			this.Opacity = 1.0;
			this.Visible = false;
			this.Enabled = true;
			fadeMode = FadeType.Gradually;
			fadeAwayArmed = false;
			fading = false;
			if (timer != null) timer.Stop ();
			if (picbox != null)
			{
				picbox.Image = splashImage;
				picbox.BackColor = background;
			}
			ResizeSplashScreen ();
		}
		private void SplashForm_FormClosed (object sender, FormClosedEventArgs e)
		{
			try
			{
				DetachHostEvents (_host);
				_host = null;
				// base.OnFormClosed (e);
			}
			catch (Exception) { }
		}
		private void SplashForm_Shown (object sender, EventArgs e)
		{
			//base.OnShown (e);
			this.Opacity = 1.0;
			this.Visible = true;
			this.Enabled = true;
			ResizeSplashScreen ();
		}
	}
}
