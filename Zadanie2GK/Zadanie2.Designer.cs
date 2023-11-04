namespace Zadanie2GK
{
    partial class Zadanie2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.kdBar = new System.Windows.Forms.TrackBar();
            this.MBox = new System.Windows.Forms.NumericUpDown();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.FPSLabel = new System.Windows.Forms.Label();
            this.Canvas = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.kdBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MBox)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Canvas)).BeginInit();
            this.SuspendLayout();
            // 
            // kdBar
            // 
            this.kdBar.Location = new System.Drawing.Point(3, 19);
            this.kdBar.Name = "kdBar";
            this.kdBar.Size = new System.Drawing.Size(160, 56);
            this.kdBar.TabIndex = 2;
            this.kdBar.Tag = "";
            this.kdBar.ValueChanged += new System.EventHandler(this.ksBar_ValueChanged);
            // 
            // MBox
            // 
            this.MBox.Location = new System.Drawing.Point(3, 81);
            this.MBox.Name = "MBox";
            this.MBox.Size = new System.Drawing.Size(120, 22);
            this.MBox.TabIndex = 4;
            this.MBox.ValueChanged += new System.EventHandler(this.MBox_ValueChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.FPSLabel);
            this.flowLayoutPanel1.Controls.Add(this.kdBar);
            this.flowLayoutPanel1.Controls.Add(this.MBox);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(610, 12);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(160, 349);
            this.flowLayoutPanel1.TabIndex = 5;
            // 
            // FPSLabel
            // 
            this.FPSLabel.AutoSize = true;
            this.FPSLabel.Location = new System.Drawing.Point(3, 0);
            this.FPSLabel.Name = "FPSLabel";
            this.FPSLabel.Size = new System.Drawing.Size(85, 16);
            this.FPSLabel.TabIndex = 5;
            this.FPSLabel.Text = "FPS: 0 (0 ms)";
            // 
            // Canvas
            // 
            this.Canvas.Location = new System.Drawing.Point(4, 3);
            this.Canvas.Name = "Canvas";
            this.Canvas.Size = new System.Drawing.Size(600, 600);
            this.Canvas.TabIndex = 6;
            this.Canvas.TabStop = false;
            // 
            // Zadanie2
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(782, 653);
            this.Controls.Add(this.Canvas);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "Zadanie2";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.kdBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MBox)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Canvas)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TrackBar kdBar;
        private System.Windows.Forms.NumericUpDown MBox;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label FPSLabel;
        private System.Windows.Forms.PictureBox Canvas;
    }
}

