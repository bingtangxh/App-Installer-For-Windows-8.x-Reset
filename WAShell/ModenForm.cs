
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DataUtils;

namespace WAShell
{
	public partial class ModernForm: Form, IMetroIconSupport
	{
		// ==================== P/Invoke ====================
		private const int WM_NCCALCSIZE = 0x0083;
		private const int WM_NCHITTEST = 0x0084;
		private const int WM_NCPAINT = 0x0085;
		private const int WM_NCACTIVATE = 0x0086;
		private const int WM_MOUSEMOVE = 0x0200;
		private const int WM_LBUTTONDOWN = 0x0201;
		private const int WM_LBUTTONUP = 0x0202;
		private const int WM_NCLBUTTONDOWN = 0x00A1;
		private const int WM_NCLBUTTONUP = 0x00A2;
		private const int WM_PAINT = 0x000F;
		private const int WM_SETTEXT = 0x000C;
		private const int WM_SIZE = 0x0005;
		private const int WM_MOUSELEAVE = 0x02A3;
		private const int WM_GETMINMAXINFO = 0x0024;
		private const int WM_DISPLAYCHANGE = 0x007E;

		private const int HTCLIENT = 1;
		private const int HTCAPTION = 2;
		private const int HTCLOSE = 20;
		private const int HTMAXBUTTON = 9;
		private const int HTMINBUTTON = 8;
		private const int HTLEFT = 10;
		private const int HTRIGHT = 11;
		private const int HTTOP = 12;
		private const int HTTOPLEFT = 13;
		private const int HTTOPRIGHT = 14;
		private const int HTBOTTOM = 15;
		private const int HTBOTTOMLEFT = 16;
		private const int HTBOTTOMRIGHT = 17;

		private const int CS_DROPSHADOW = 0x00020000;

		[StructLayout (LayoutKind.Sequential)]
		private struct MINMAXINFO
		{
			public Point ptReserved;
			public Point ptMaxSize;
			public Point ptMaxPosition;
			public Point ptMinTrackSize;
			public Point ptMaxTrackSize;
		}

		[StructLayout (LayoutKind.Sequential)]
		private struct NCCALCSIZE_PARAMS
		{
			[MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
			public RECT [] rgrc;
			public IntPtr lppos;
		}

		[StructLayout (LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout (LayoutKind.Sequential)]
		private struct MARGINS
		{
			public int cxLeftWidth;
			public int cxRightWidth;
			public int cyTopHeight;
			public int cyBottomHeight;
		}

		[DllImport ("user32.dll", SetLastError = true)]
		private static extern IntPtr GetWindowDC (IntPtr hWnd);

		[DllImport ("user32.dll")]
		private static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);

		[DllImport ("user32.dll")]
		private static extern bool GetWindowRect (IntPtr hWnd, out RECT lpRect);

		[DllImport ("user32.dll")]
		private static extern bool GetClientRect (IntPtr hWnd, out RECT lpRect);

		[DllImport ("user32.dll")]
		private static extern bool ReleaseCapture ();

		[DllImport ("user32.dll")]
		private static extern int SendMessage (IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		[DllImport ("user32.dll")]
		private static extern bool TrackMouseEvent (ref TRACKMOUSEEVENT tme);

		[DllImport ("user32.dll")]
		private static extern IntPtr MonitorFromWindow (IntPtr hwnd, int dwFlags);

		[DllImport ("user32.dll")]
		private static extern bool GetMonitorInfo (IntPtr hMonitor, ref MONITORINFO lpmi);

		[DllImport ("dwmapi.dll", PreserveSig = false)]
		private static extern void DwmExtendFrameIntoClientArea (IntPtr hWnd, ref MARGINS pMarInset);

		[DllImport ("dwmapi.dll")]
		private static extern int DwmIsCompositionEnabled (out bool pfEnabled);

		[StructLayout (LayoutKind.Sequential)]
		private struct MONITORINFO
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public int dwFlags;
		}

		[StructLayout (LayoutKind.Sequential)]
		private struct TRACKMOUSEEVENT
		{
			public int cbSize;
			public int dwFlags;
			public IntPtr hwndTrack;
			public int dwHoverTime;
		}

		private const int TME_LEAVE = 0x00000002;
		private const int MONITOR_DEFAULTTONULL = 0;

		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_MOVE = 0xF010;
		private const int SC_SIZE = 0xF000;

		// ==================== 字段和属性 ====================
		private int _titleBarHeight;
		private int _borderWidth;
		private int _resizeAreaWidth = 10;

		// DPI 适配的尺寸
		private int _iconSize;
		private int _iconOffsetLeft;
		private int _iconOffsetTop;
		private int _buttonDisplaySize;

		private Color _titleBarColorActive = Color.FromArgb (17, 17, 17);
		private bool _isFormActive = true;

		private Rectangle _minButtonRect;
		private Rectangle _maxButtonRect;
		private Rectangle _closeButtonRect;
		private Rectangle _iconRect;

		private bool _isMouseOverMinButton = false;
		private bool _isMouseOverMaxButton = false;
		private bool _isMouseOverCloseButton = false;

		// 按钮按压状态追踪
		private bool _isMinButtonPressed = false;
		private bool _isMaxButtonPressed = false;
		private bool _isCloseButtonPressed = false;

		// 资源管理器
		private System.ComponentModel.ComponentResourceManager _res;

		// 按钮资源缓存（原始尺寸）
		private Bitmap _minNormal, _minLight, _minPress;
		private Bitmap _maxNormal, _maxLight, _maxPress;
		private Bitmap _restoreNormal, _restoreLight, _restorePress;
		private Bitmap _cancelNormal, _cancelLight, _cancelPress;

		// 缩放后的按钮资源缓存
		private Bitmap _minNormalScaled, _minLightScaled, _minPressScaled;
		private Bitmap _maxNormalScaled, _maxLightScaled, _maxPressScaled;
		private Bitmap _restoreNormalScaled, _restoreLightScaled, _restorePressScaled;
		private Bitmap _cancelNormalScaled, _cancelLightScaled, _cancelPressScaled;

		// 窗口图标
		private Icon _windowIcon;

		private enum ButtonState
		{
			Normal,
			Hover,
			Pressed
		}

		private ButtonState _minState = ButtonState.Normal;
		private ButtonState _maxState = ButtonState.Normal;
		private ButtonState _closeState = ButtonState.Normal;

		private bool _trackingMouse = false;
		private bool _buttonRectsCalculated = false;

		// 防止重复的 NCPAINT 请求
		private bool _isInNCPaint = false;

		public ModernForm ()
		{
			InitializeComponent ();
			SetStyle (ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);

			this.FormBorderStyle = FormBorderStyle.None;
			this.DoubleBuffered = false;

			_res = new System.ComponentModel.ComponentResourceManager (typeof (ModernForm));

			UpdateDPIValues ();
			LoadResourceImages ();
			ScaleButtonImages ();
			EnableDWM ();
		}

		// ==================== DPI 处理 ====================
		private void UpdateDPIValues ()
		{
			double dpiScale = UITheme.DPIDouble;
			_titleBarHeight = (int)Math.Round (30 * dpiScale);
			_borderWidth = (int)Math.Round (1 * dpiScale);
			_iconSize = (int)Math.Round (24 * dpiScale);
			_iconOffsetLeft = (int)Math.Round (3 * dpiScale);
			_iconOffsetTop = (int)Math.Round (3 * dpiScale);
			_iconSize = _titleBarHeight - _iconOffsetTop * 2;

			// 按钮显示大小与标题栏等高
			_buttonDisplaySize = _titleBarHeight;

			this.Padding = new Padding (_borderWidth, _titleBarHeight, _borderWidth, _borderWidth);
		}

		// ==================== DWM 适配 ====================
		private void EnableDWM ()
		{
			try
			{
				// 检查 DWM 是否启用
				bool isDwmEnabled = false;
				if (DwmIsCompositionEnabled (out isDwmEnabled) == 0 && isDwmEnabled)
				{
					// 仅禁用标题栏 DWM 绘制，保留 Aero 特性（Aero Snap、Aero Shake 等）
					MARGINS margins = new MARGINS
					{
						cxLeftWidth = 0,
						cxRightWidth = 0,
						cyTopHeight = 0,
						cyBottomHeight = 0
					};

					DwmExtendFrameIntoClientArea (this.Handle, ref margins);
				}
			}
			catch
			{
				// DWM 不可用，继续正常运行
			}
		}

		// ==================== 资源加载 ====================
		private void LoadResourceImages ()
		{
			try
			{
				// 最小化按钮
				_minNormal = _res.GetObject ("min") as Bitmap;
				_minLight = _res.GetObject ("min_light") as Bitmap;
				_minPress = _res.GetObject ("min_press") as Bitmap;

				// 最大化按钮
				_maxNormal = _res.GetObject ("max") as Bitmap;
				_maxLight = _res.GetObject ("max_light") as Bitmap;
				_maxPress = _res.GetObject ("max_press") as Bitmap;

				// 还原按钮
				_restoreNormal = _res.GetObject ("restore") as Bitmap;
				_restoreLight = _res.GetObject ("restore_light") as Bitmap;
				_restorePress = _res.GetObject ("restore_press") as Bitmap;

				// 关闭按钮
				_cancelNormal = _res.GetObject ("cancel") as Bitmap;
				_cancelLight = _res.GetObject ("cancel_light") as Bitmap;
				_cancelPress = _res.GetObject ("cancel_press") as Bitmap;
			}
			catch (Exception ex)
			{
				MessageBox.Show ("加载资源失败: " + ex.Message);
			}
		}

		// ==================== 按钮图片 DPI 缩放 ====================
		private void ScaleButtonImages ()
		{
			double dpiScale = UITheme.DPIDouble;
			int scaledSize = _buttonDisplaySize;

			try
			{
				// 最小化按钮
				_minNormalScaled = ScaleBitmap (_minNormal, scaledSize);
				_minLightScaled = ScaleBitmap (_minLight, scaledSize);
				_minPressScaled = ScaleBitmap (_minPress, scaledSize);

				// 最大化按钮
				_maxNormalScaled = ScaleBitmap (_maxNormal, scaledSize);
				_maxLightScaled = ScaleBitmap (_maxLight, scaledSize);
				_maxPressScaled = ScaleBitmap (_maxPress, scaledSize);

				// 还原按钮
				_restoreNormalScaled = ScaleBitmap (_restoreNormal, scaledSize);
				_restoreLightScaled = ScaleBitmap (_restoreLight, scaledSize);
				_restorePressScaled = ScaleBitmap (_restorePress, scaledSize);

				// 关闭按钮
				_cancelNormalScaled = ScaleBitmap (_cancelNormal, scaledSize);
				_cancelLightScaled = ScaleBitmap (_cancelLight, scaledSize);
				_cancelPressScaled = ScaleBitmap (_cancelPress, scaledSize);
			}
			catch (Exception ex)
			{
				MessageBox.Show ("缩放按钮图片失败: " + ex.Message);
			}
		}

		private Bitmap ScaleBitmap (Bitmap original, int targetSize)
		{
			if (original == null)
				return null;

			if (original.Width == targetSize && original.Height == targetSize)
				return original;

			Bitmap scaled = new Bitmap (targetSize, targetSize);
			using (Graphics g = Graphics.FromImage (scaled))
			{
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.DrawImage (original, 0, 0, targetSize, targetSize);
			}
			return scaled;
		}

		// ==================== 属性 ====================
		public Color TitleBarColorActive
		{
			get { return _titleBarColorActive; }
			set { _titleBarColorActive = value; Invalidate (); }
		}

		public int ResizeAreaWidth
		{
			get { return _resizeAreaWidth; }
			set { _resizeAreaWidth = value; }
		}

		public virtual Icon WindowIcon
		{
			get { return _windowIcon; }
			set
			{
				_windowIcon = value;
				if (this.ClientSize.Width > 0)
					SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
			}
		}

		// ==================== 坐标计算 ====================
		private int GetTitleBarWidth ()
		{
			RECT rcWindow;
			GetWindowRect (this.Handle, out rcWindow);
			return rcWindow.right - rcWindow.left - _borderWidth * 2;
		}

		private void RecalculateButtonRects ()
		{
			int titleBarWidth = GetTitleBarWidth ();
			if (titleBarWidth <= 0) return;

			// 按钮与标题栏等高
			int buttonY = 0; // 顶部对齐
			int buttonHeight = _titleBarHeight;

			// 从右到左排列按钮
			int closeButtonX = _borderWidth + titleBarWidth - _buttonDisplaySize;
			int maxButtonX = closeButtonX - _buttonDisplaySize;
			int minButtonX = maxButtonX - _buttonDisplaySize;

			_closeButtonRect = new Rectangle (closeButtonX, buttonY, _buttonDisplaySize, buttonHeight);
			_maxButtonRect = new Rectangle (maxButtonX, buttonY, _buttonDisplaySize, buttonHeight);
			_minButtonRect = new Rectangle (minButtonX, buttonY, _buttonDisplaySize, buttonHeight);

			// 图标位置
			_iconRect = new Rectangle (
				_borderWidth + _iconOffsetLeft,
				_borderWidth + (_titleBarHeight - _iconSize) / 2,
				_iconSize,
				_iconSize);

			_buttonRectsCalculated = true;
		}

		// ==================== 坐标工具方法 ====================
		private Point GetWindowCursorPosition ()
		{
			RECT rcWindow;
			GetWindowRect (this.Handle, out rcWindow);

			int screenX = Control.MousePosition.X;
			int screenY = Control.MousePosition.Y;

			int windowX = screenX - rcWindow.left;
			int windowY = screenY - rcWindow.top;

			return new Point (windowX, windowY);
		}

		private void UpdateButtonHoverState (Point windowPt)
		{
			if (!_buttonRectsCalculated)
				RecalculateButtonRects ();

			// 保存原来的状态
			ButtonState oldMin = _minState;
			ButtonState oldMax = _maxState;
			ButtonState oldClose = _closeState;

			// 更新 hover 状态
			_isMouseOverMinButton = _minButtonRect.Contains (windowPt);
			_isMouseOverMaxButton = _maxButtonRect.Contains (windowPt);
			_isMouseOverCloseButton = _closeButtonRect.Contains (windowPt);

			// 更新 ButtonState
			_minState = _isMinButtonPressed ? ButtonState.Pressed : (_isMouseOverMinButton ? ButtonState.Hover : ButtonState.Normal);
			_maxState = _isMaxButtonPressed ? ButtonState.Pressed : (_isMouseOverMaxButton ? ButtonState.Hover : ButtonState.Normal);
			_closeState = _isCloseButtonPressed ? ButtonState.Pressed : (_isMouseOverCloseButton ? ButtonState.Hover : ButtonState.Normal);

			// 如果状态变化，触发重绘
			if (!_isInNCPaint &&
				(oldMin != _minState || oldMax != _maxState || oldClose != _closeState))
			{
				SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
			}
		}



		// ==================== WndProc 处理 ====================
		protected override void WndProc (ref Message m)
		{
			switch (m.Msg)
			{
				case WM_NCCALCSIZE:
					HandleNCCalcSize (ref m);
					return;

				case WM_GETMINMAXINFO:
					HandleGetMinMaxInfo (ref m);
					return;

				case WM_NCPAINT:
					// 防止重复处理
					_isInNCPaint = true;
					try
					{
						PaintNonClientArea ();
						PaintTitleBar ();
						m.Result = IntPtr.Zero;
					}
					finally
					{
						_isInNCPaint = false;
					}
					return;

				case WM_NCACTIVATE:
					_isFormActive = m.WParam.ToInt32 () != 0;
					base.WndProc (ref m);
					// 延迟重绘以避免闪烁
					if (!_isInNCPaint)
						SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
					return;

				case WM_PAINT:
					base.WndProc (ref m);
					return;

				case WM_SIZE:
					base.WndProc (ref m);
					_buttonRectsCalculated = false;
					Invalidate ();
					if (!_isInNCPaint)
						SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
					return;

				case WM_NCHITTEST:
					HandleNCHitTest (ref m);
					return;

				case WM_MOUSEMOVE:
					HandleMouseMove (m);
					base.WndProc (ref m);
					return;

				case WM_NCLBUTTONDOWN:
					HandleNCMouseDown (m);
					return;

				case WM_NCLBUTTONUP:
					HandleNCMouseUp (m);
					return;

				case WM_MOUSELEAVE:
					HandleMouseLeave (m);
					base.WndProc (ref m);
					return;

				default:
					base.WndProc (ref m);
					return;
			}
		}

		private void HandleNCCalcSize (ref Message m)
		{
			if (m.WParam != IntPtr.Zero)
			{
				NCCALCSIZE_PARAMS nccs = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure (m.LParam, typeof (NCCALCSIZE_PARAMS));

				// 调整客户区以留出边框空间
				nccs.rgrc [0].left += _borderWidth;
				nccs.rgrc [0].right -= _borderWidth;
				nccs.rgrc [0].bottom -= _borderWidth;
				// 关键：不修改顶部，让非客户区承载标题栏
				// nccs.rgrc [0].top 保持不变

				Marshal.StructureToPtr (nccs, m.LParam, false);
			}
			m.Result = IntPtr.Zero;
		}

		private void HandleGetMinMaxInfo (ref Message m)
		{
			MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure (m.LParam, typeof (MINMAXINFO));

			IntPtr hMonitor = MonitorFromWindow (this.Handle, MONITOR_DEFAULTTONULL);
			if (hMonitor != IntPtr.Zero)
			{
				MONITORINFO monitorInfo = new MONITORINFO ();
				monitorInfo.cbSize = Marshal.SizeOf (monitorInfo);
				if (GetMonitorInfo (hMonitor, ref monitorInfo))
				{
					int workWidth = monitorInfo.rcWork.right - monitorInfo.rcWork.left;
					int workHeight = monitorInfo.rcWork.bottom - monitorInfo.rcWork.top;

					mmi.ptMaxSize.X = workWidth + _borderWidth * 2;
					mmi.ptMaxSize.Y = workHeight + _borderWidth;

					mmi.ptMaxPosition.X = monitorInfo.rcWork.left - _borderWidth;
					mmi.ptMaxPosition.Y = monitorInfo.rcWork.top;
				}
			}

			Marshal.StructureToPtr (mmi, m.LParam, false);
		}

		private void HandleNCHitTest (ref Message m)
		{
			if (!_buttonRectsCalculated)
				RecalculateButtonRects ();

			Point windowPt = GetWindowCursorPosition ();

			RECT rcWindow;
			GetWindowRect (this.Handle, out rcWindow);
			int windowWidth = rcWindow.right - rcWindow.left;
			int windowHeight = rcWindow.bottom - rcWindow.top;

			int corner = _resizeAreaWidth;

			// 四角
			if (windowPt.X < corner && windowPt.Y < corner) { m.Result = (IntPtr)HTTOPLEFT; return; }
			if (windowPt.X > windowWidth - corner && windowPt.Y < corner) { m.Result = (IntPtr)HTTOPRIGHT; return; }
			if (windowPt.X < corner && windowPt.Y > windowHeight - corner) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
			if (windowPt.X > windowWidth - corner && windowPt.Y > windowHeight - corner) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }

			// 边界
			if (windowPt.X < corner) { m.Result = (IntPtr)HTLEFT; return; }
			if (windowPt.X > windowWidth - corner) { m.Result = (IntPtr)HTRIGHT; return; }
			if (windowPt.Y > windowHeight - corner) { m.Result = (IntPtr)HTBOTTOM; return; }

			// 标题栏区域
			if (windowPt.Y < _borderWidth + _titleBarHeight)
			{
				UpdateButtonHoverState (windowPt);
				m.Result = (IntPtr)HTCAPTION;
				return;
			}

			m.Result = (IntPtr)HTCLIENT;
		}

		private void HandleMouseMove (Message m)
		{
			if (!_buttonRectsCalculated)
				RecalculateButtonRects ();

			Point windowPt = GetWindowCursorPosition ();

			// 启用 WM_MOUSELEAVE
			if (!_trackingMouse)
			{
				TRACKMOUSEEVENT tme = new TRACKMOUSEEVENT ();
				tme.cbSize = Marshal.SizeOf (tme);
				tme.dwFlags = TME_LEAVE;
				tme.hwndTrack = this.Handle;
				TrackMouseEvent (ref tme);
				_trackingMouse = true;
			}

			bool oldOverMin = _isMouseOverMinButton;
			bool oldOverMax = _isMouseOverMaxButton;
			bool oldOverClose = _isMouseOverCloseButton;

			_isMouseOverMinButton = _minButtonRect.Contains (windowPt);
			_isMouseOverMaxButton = _maxButtonRect.Contains (windowPt);
			_isMouseOverCloseButton = _closeButtonRect.Contains (windowPt);

			// 只要状态变化就重绘标题栏
			if (!_isInNCPaint &&
				(oldOverMin != _isMouseOverMinButton ||
				 oldOverMax != _isMouseOverMaxButton ||
				 oldOverClose != _isMouseOverCloseButton))
			{
				SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
			}

			base.WndProc (ref m); // 保留鼠标移动默认处理
		}


		private void HandleNCMouseDown (Message m)
		{
			if (!_buttonRectsCalculated)
				RecalculateButtonRects ();

			Point windowPt = GetWindowCursorPosition ();

			if (_minButtonRect.Contains (windowPt))
			{
				_isMinButtonPressed = true;
				_minState = ButtonState.Pressed;
				if (!_isInNCPaint)
					SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
			}
			else if (_maxButtonRect.Contains (windowPt))
			{
				_isMaxButtonPressed = true;
				_maxState = ButtonState.Pressed;
				if (!_isInNCPaint)
					SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
			}
			else if (_closeButtonRect.Contains (windowPt))
			{
				_isCloseButtonPressed = true;
				_closeState = ButtonState.Pressed;
				if (!_isInNCPaint)
					SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
			}
			else
			{
				base.WndProc (ref m);
			}
		}

		private void HandleNCMouseUp (Message m)
		{
			if (!_buttonRectsCalculated)
				RecalculateButtonRects ();

			Point windowPt = GetWindowCursorPosition ();

			if (_minButtonRect.Contains (windowPt) && _isMinButtonPressed)
			{
				this.WindowState = FormWindowState.Minimized;
			}
			else if (_maxButtonRect.Contains (windowPt) && _isMaxButtonPressed)
			{
				this.WindowState = this.WindowState == FormWindowState.Maximized
					? FormWindowState.Normal
					: FormWindowState.Maximized;
			}
			else if (_closeButtonRect.Contains (windowPt) && _isCloseButtonPressed)
			{
				_isMinButtonPressed = false;
				_isMaxButtonPressed = false;
				_isCloseButtonPressed = false;

				_minState = ButtonState.Normal;
				_maxState = ButtonState.Normal;
				_closeState = ButtonState.Normal;

				if (!_isInNCPaint)
					SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
				this.Close ();
				return;
			}

			_isMinButtonPressed = false;
			_isMaxButtonPressed = false;
			_isCloseButtonPressed = false;

			_minState = ButtonState.Normal;
			_maxState = ButtonState.Normal;
			_closeState = ButtonState.Normal;

			if (!_isInNCPaint)
				SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
		}

		private void HandleMouseLeave (Message m)
		{
			bool needRedraw = _isMouseOverMinButton || _isMouseOverMaxButton || _isMouseOverCloseButton;

			_isMouseOverMinButton = false;
			_isMouseOverMaxButton = false;
			_isMouseOverCloseButton = false;

			_minState = _isMinButtonPressed ? ButtonState.Pressed : ButtonState.Normal;
			_maxState = _isMaxButtonPressed ? ButtonState.Pressed : ButtonState.Normal;
			_closeState = _isCloseButtonPressed ? ButtonState.Pressed : ButtonState.Normal;

			if (needRedraw && !_isInNCPaint)
			{
				SendMessage (this.Handle, WM_NCPAINT, IntPtr.Zero, IntPtr.Zero);
			}

			_trackingMouse = false;
		}

		// ==================== 绘制方法 ====================
		private void PaintNonClientArea ()
		{
			IntPtr hdc = GetWindowDC (this.Handle);
			if (hdc == IntPtr.Zero) return;

			try
			{
				using (Graphics g = Graphics.FromHdc (hdc))
				{
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

					RECT rcWindow;
					GetWindowRect (this.Handle, out rcWindow);

					int windowWidth = rcWindow.right - rcWindow.left;
					int windowHeight = rcWindow.bottom - rcWindow.top;

					// 边框颜色
					Color borderColor = _isFormActive ? UITheme.GetDwmThemeColor () : Color.FromArgb (177, 177, 177);
					borderColor = UITheme.GetDwmThemeColor ();
					int borderW = Math.Max (1, _borderWidth);

					// 绘制边框（左、右、底）
					using (Pen pen = new Pen (borderColor, borderW))
					{
						float halfBorder = borderW / 2.0f;

						// 左边框
						g.DrawLine (pen, halfBorder, 0, halfBorder, windowHeight);

						// 右边框
						g.DrawLine (pen, windowWidth - halfBorder, 0, windowWidth - halfBorder, windowHeight);

						// 底部边框
						g.DrawLine (pen, 0, windowHeight - halfBorder, windowWidth, windowHeight - halfBorder);
					}
				}
			}
			finally
			{
				ReleaseDC (this.Handle, hdc);
			}
		}

		private void PaintTitleBar ()
		{
			if (this.ClientSize.Width <= 0 || _titleBarHeight <= 0)
				return;

			if (!_buttonRectsCalculated)
			{
				RecalculateButtonRects ();
			}

			IntPtr hdc = GetWindowDC (this.Handle);
			if (hdc == IntPtr.Zero) return;

			try
			{
				using (Graphics g = Graphics.FromHdc (hdc))
				{
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

					RECT rcWindow;
					GetWindowRect (this.Handle, out rcWindow);
					int windowWidth = rcWindow.right - rcWindow.left;

					// 标题栏绘制区域
					int titleBarX = _borderWidth;
					int titleBarY = 0;
					int titleBarWidth = windowWidth - _borderWidth * 2;
					int titleBarHeight = _titleBarHeight;
					Color titleBarColor = _isFormActive ? _titleBarColorActive : Color.FromArgb (177, 177, 177);
					// 绘制标题栏背景（完整填充，不留缝隙）
					using (Brush brush = new SolidBrush (titleBarColor))
					{
						g.FillRectangle (brush, titleBarX, titleBarY, titleBarWidth, titleBarHeight);
					}

					// 绘制图标
					Icon iconToDraw = _windowIcon ?? this.Icon;
					if (iconToDraw != null && _iconRect.Width > 0 && _iconRect.Height > 0)
					{
						try
						{
							Bitmap iconBitmap = iconToDraw.ToBitmap ();
							Rectangle adjustedIconRect = new Rectangle (
								_iconRect.X,
								_iconRect.Y,
								_iconSize,
								_iconSize);
							g.DrawImage (iconBitmap, adjustedIconRect);
							iconBitmap.Dispose ();
						}
						catch { }
					}

					// 绘制标题文本
					if (!string.IsNullOrEmpty (this.Text))
					{
						using (Font font = GetTitleFont ())
						{
							SizeF textSize = g.MeasureString (this.Text, font);
							float textX = titleBarX + (titleBarWidth - textSize.Width) / 2.0f;
							float textY = titleBarY + (titleBarHeight - textSize.Height) / 2.0f;

							using (Brush textBrush = new SolidBrush (Color.White))
							{
								g.DrawString (this.Text, font, textBrush, textX, textY);
							}
						}
					}

					// 绘制按钮
					DrawTitleBarButtons (g);
				}
			}
			finally
			{
				ReleaseDC (this.Handle, hdc);
			}
		}

		private Font GetTitleFont ()
		{
			string [] fontNames = { "Microsoft YaHei", "Segoe UI" };

			foreach (string fontName in fontNames)
			{
				try
				{
					using (Font testFont = new Font (fontName, 11, FontStyle.Regular))
					{
						return new Font (fontName, 11, FontStyle.Regular);
					}
				}
				catch
				{
				}
			}

			return new Font (SystemFonts.DefaultFont ?? new Font ("Arial", 11, FontStyle.Regular), FontStyle.Regular);
		}

		private void DrawTitleBarButtons (Graphics g)
		{
			Color backgroundColor = _titleBarColorActive;

			// 绘制最小化按钮
			DrawButton (g, _minButtonRect, _minState, _minNormalScaled, _minLightScaled, _minPressScaled, backgroundColor);

			// 绘制最大化/还原按钮
			if (this.WindowState == FormWindowState.Maximized)
			{
				DrawButton (g, _maxButtonRect, _maxState, _restoreNormalScaled, _restoreLightScaled, _restorePressScaled, backgroundColor);
			}
			else
			{
				DrawButton (g, _maxButtonRect, _maxState, _maxNormalScaled, _maxLightScaled, _maxPressScaled, backgroundColor);
			}

			// 绘制关闭按钮
			DrawButton (g, _closeButtonRect, _closeState, _cancelNormalScaled, _cancelLightScaled, _cancelPressScaled, backgroundColor);
		}

		private void DrawButton (Graphics g, Rectangle buttonRect, ButtonState state,
	Bitmap imgNormal, Bitmap imgLight, Bitmap imgPress, Color backgroundColor)
		{
			if (buttonRect.Width <= 0 || buttonRect.Height <= 0)
				return;

			// 1. 填充背景色
			using (SolidBrush brush = new SolidBrush (backgroundColor))
			{
				g.FillRectangle (brush, buttonRect);
			}

			// 2. 选择状态对应的图片
			Bitmap imgBitmap = null;
			if (state == ButtonState.Pressed)
				imgBitmap = imgPress ?? imgLight ?? imgNormal;
			else if ((buttonRect == _minButtonRect && _isMouseOverMinButton) ||
					 (buttonRect == _maxButtonRect && _isMouseOverMaxButton) ||
					 (buttonRect == _closeButtonRect && _isMouseOverCloseButton))
				imgBitmap = imgLight ?? imgNormal;
			else
				imgBitmap = imgNormal;

			if (imgBitmap != null)
			{
				// 3. 强制转换为标准 Bitmap 并绘制到目标区域
				try
				{
					using (Bitmap bmp = new Bitmap (imgBitmap.Width, imgBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
					{
						using (Graphics tempG = Graphics.FromImage (bmp))
						{
							tempG.DrawImage (imgBitmap, 0, 0, imgBitmap.Width, imgBitmap.Height);
						}

						// 绘制到目标区域并自动缩放
						g.DrawImage (bmp, buttonRect);
					}
				}
				catch { }
			}
		}


		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ClassStyle |= CS_DROPSHADOW;
				return cp;
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				// 原始资源
				_minNormal?.Dispose ();
				_minLight?.Dispose ();
				_minPress?.Dispose ();
				_maxNormal?.Dispose ();
				_maxLight?.Dispose ();
				_maxPress?.Dispose ();
				_restoreNormal?.Dispose ();
				_restoreLight?.Dispose ();
				_restorePress?.Dispose ();
				_cancelNormal?.Dispose ();
				_cancelLight?.Dispose ();
				_cancelPress?.Dispose ();

				// 缩放后的资源
				_minNormalScaled?.Dispose ();
				_minLightScaled?.Dispose ();
				_minPressScaled?.Dispose ();
				_maxNormalScaled?.Dispose ();
				_maxLightScaled?.Dispose ();
				_maxPressScaled?.Dispose ();
				_restoreNormalScaled?.Dispose ();
				_restoreLightScaled?.Dispose ();
				_restorePressScaled?.Dispose ();
				_cancelNormalScaled?.Dispose ();
				_cancelLightScaled?.Dispose ();
				_cancelPressScaled?.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}