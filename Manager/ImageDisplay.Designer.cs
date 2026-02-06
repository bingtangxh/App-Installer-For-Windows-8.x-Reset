namespace Manager
{
	partial class ImageDisplay
	{
		/// <summary> 
		/// 必需的设计器变量。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// 清理所有正在使用的资源。
		/// </summary>
		/// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region 组件设计器生成的代码

		/// <summary> 
		/// 设计器支持所需的方法 - 不要修改
		/// 使用代码编辑器修改此方法的内容。
		/// </summary>
		private void InitializeComponent ()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageDisplay));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.sizeDisplay = new System.Windows.Forms.Label();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.backgroundPanel = new System.Windows.Forms.Panel();
			this.foregroundPicture = new System.Windows.Forms.PictureBox();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.backgroundPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.foregroundPicture)).BeginInit();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.sizeDisplay, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(30, 60);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// sizeDisplay
			// 
			this.sizeDisplay.AutoSize = true;
			this.sizeDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sizeDisplay.Location = new System.Drawing.Point(3, 0);
			this.sizeDisplay.Name = "sizeDisplay";
			this.sizeDisplay.Size = new System.Drawing.Size(24, 15);
			this.sizeDisplay.TabIndex = 0;
			this.sizeDisplay.Text = "16";
			this.sizeDisplay.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("flowLayoutPanel1.BackgroundImage")));
			this.flowLayoutPanel1.Controls.Add(this.backgroundPanel);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 15);
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(30, 45);
			this.flowLayoutPanel1.TabIndex = 1;
			// 
			// backgroundPanel
			// 
			this.backgroundPanel.BackColor = System.Drawing.Color.Transparent;
			this.backgroundPanel.Controls.Add(this.foregroundPicture);
			this.backgroundPanel.Location = new System.Drawing.Point(0, 0);
			this.backgroundPanel.Margin = new System.Windows.Forms.Padding(0);
			this.backgroundPanel.Name = "backgroundPanel";
			this.backgroundPanel.Size = new System.Drawing.Size(24, 24);
			this.backgroundPanel.TabIndex = 0;
			// 
			// foregroundPicture
			// 
			this.foregroundPicture.BackColor = System.Drawing.Color.Transparent;
			this.foregroundPicture.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
			this.foregroundPicture.Location = new System.Drawing.Point(3, 4);
			this.foregroundPicture.Name = "foregroundPicture";
			this.foregroundPicture.Size = new System.Drawing.Size(16, 16);
			this.foregroundPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.foregroundPicture.TabIndex = 0;
			this.foregroundPicture.TabStop = false;
			// 
			// ImageDisplay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel1);
			this.MinimumSize = new System.Drawing.Size(30, 0);
			this.Name = "ImageDisplay";
			this.Size = new System.Drawing.Size(30, 60);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.backgroundPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.foregroundPicture)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label sizeDisplay;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Panel backgroundPanel;
		private System.Windows.Forms.PictureBox foregroundPicture;
	}
}
