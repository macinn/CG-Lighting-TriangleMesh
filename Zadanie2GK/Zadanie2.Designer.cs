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
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.kdBar = new System.Windows.Forms.TrackBar();
            this.Canvas = new Zadanie2GK.Canvas();
            this.MBox = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.kdBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MBox)).BeginInit();
            this.SuspendLayout();
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(610, 13);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(95, 20);
            this.checkBox1.TabIndex = 1;
            this.checkBox1.Text = "checkBox1";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // kdBar
            // 
            this.kdBar.Location = new System.Drawing.Point(610, 40);
            this.kdBar.Name = "kdBar";
            this.kdBar.Size = new System.Drawing.Size(160, 56);
            this.kdBar.TabIndex = 2;
            this.kdBar.Tag = "";
            this.kdBar.ValueChanged += new System.EventHandler(this.ksBar_ValueChanged);
            // 
            // Canvas
            // 
            this.Canvas.Location = new System.Drawing.Point(3, 3);
            this.Canvas.Name = "Canvas";
            this.Canvas.Size = new System.Drawing.Size(600, 600);
            this.Canvas.TabIndex = 0;
            this.Canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.Canvas_Paint);
            // 
            // MBox
            // 
            this.MBox.Location = new System.Drawing.Point(620, 103);
            this.MBox.Name = "MBox";
            this.MBox.Size = new System.Drawing.Size(120, 22);
            this.MBox.TabIndex = 4;
            this.MBox.ValueChanged += new System.EventHandler(this.MBox_ValueChanged);
            // 
            // Zadanie2
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(782, 653);
            this.Controls.Add(this.MBox);
            this.Controls.Add(this.kdBar);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.Canvas);
            this.Name = "Zadanie2";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.kdBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Canvas Canvas;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TrackBar kdBar;
        private System.Windows.Forms.NumericUpDown MBox;
    }
}

