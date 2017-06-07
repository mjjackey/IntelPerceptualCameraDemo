namespace IntelPerceptualCameraDemo
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.MainPanel = new System.Windows.Forms.PictureBox();
            this.PIPPanel = new System.Windows.Forms.PictureBox();
            this.Start = new System.Windows.Forms.Button();
            this.Stop = new System.Windows.Forms.Button();
            this.Status2 = new System.Windows.Forms.StatusStrip();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.DepthRaw = new System.Windows.Forms.RadioButton();
            this.Depth = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.MainPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PIPPanel)).BeginInit();
            this.Status2.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainPanel
            // 
            this.MainPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MainPanel.ErrorImage = null;
            this.MainPanel.InitialImage = null;
            this.MainPanel.Location = new System.Drawing.Point(12, 12);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(360, 199);
            this.MainPanel.TabIndex = 28;
            this.MainPanel.TabStop = false;
            // 
            // PIPPanel
            // 
            this.PIPPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PIPPanel.Location = new System.Drawing.Point(12, 231);
            this.PIPPanel.Name = "PIPPanel";
            this.PIPPanel.Size = new System.Drawing.Size(360, 202);
            this.PIPPanel.TabIndex = 36;
            this.PIPPanel.TabStop = false;
            // 
            // Start
            // 
            this.Start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Start.Location = new System.Drawing.Point(379, 338);
            this.Start.Name = "Start";
            this.Start.Size = new System.Drawing.Size(73, 21);
            this.Start.TabIndex = 37;
            this.Start.Text = "Start";
            this.Start.UseVisualStyleBackColor = true;
            this.Start.Click += new System.EventHandler(this.Start_Click);
            // 
            // Stop
            // 
            this.Stop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Stop.Enabled = false;
            this.Stop.Location = new System.Drawing.Point(379, 388);
            this.Stop.Name = "Stop";
            this.Stop.Size = new System.Drawing.Size(73, 21);
            this.Stop.TabIndex = 38;
            this.Stop.Text = "Stop";
            this.Stop.UseVisualStyleBackColor = true;
            this.Stop.Click += new System.EventHandler(this.Stop_Click);
            // 
            // Status2
            // 
            this.Status2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabel});
            this.Status2.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.Status2.Location = new System.Drawing.Point(0, 443);
            this.Status2.Name = "Status2";
            this.Status2.Size = new System.Drawing.Size(464, 22);
            this.Status2.TabIndex = 39;
            this.Status2.Text = "Status2";
            // 
            // StatusLabel
            // 
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(26, 17);
            this.StatusLabel.Text = "OK";
            // 
            // DepthRaw
            // 
            this.DepthRaw.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DepthRaw.AutoSize = true;
            this.DepthRaw.Location = new System.Drawing.Point(379, 63);
            this.DepthRaw.Name = "DepthRaw";
            this.DepthRaw.Size = new System.Drawing.Size(89, 16);
            this.DepthRaw.TabIndex = 41;
            this.DepthRaw.Text = "Depth (Raw)";
            this.DepthRaw.UseVisualStyleBackColor = true;
            // 
            // Depth
            // 
            this.Depth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Depth.AutoSize = true;
            this.Depth.Location = new System.Drawing.Point(379, 30);
            this.Depth.Name = "Depth";
            this.Depth.Size = new System.Drawing.Size(53, 16);
            this.Depth.TabIndex = 40;
            this.Depth.Text = "Depth";
            this.Depth.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 465);
            this.Controls.Add(this.DepthRaw);
            this.Controls.Add(this.Depth);
            this.Controls.Add(this.Status2);
            this.Controls.Add(this.Stop);
            this.Controls.Add(this.Start);
            this.Controls.Add(this.PIPPanel);
            this.Controls.Add(this.MainPanel);
            this.Name = "MainForm";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.MainPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PIPPanel)).EndInit();
            this.Status2.ResumeLayout(false);
            this.Status2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox MainPanel;
        private System.Windows.Forms.PictureBox PIPPanel;
        private System.Windows.Forms.Button Start;
        private System.Windows.Forms.Button Stop;
        private System.Windows.Forms.StatusStrip Status2;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private System.Windows.Forms.RadioButton DepthRaw;
        private System.Windows.Forms.RadioButton Depth;
    }
}

