using System.Windows.Forms;

namespace DrawingPlugin
{
    partial class RegFiguresMenu : Form
    {
        private System.Windows.Forms.Button triangleButton;
        private System.Windows.Forms.Button squareButton;
        private System.Windows.Forms.Button pentagonButton;
        private System.Windows.Forms.Button hexagonButton;
        private System.Windows.Forms.TextBox text1;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegFiguresMenu));
            this.triangleButton = new System.Windows.Forms.Button();
            this.pentagonButton = new System.Windows.Forms.Button();
            this.hexagonButton = new System.Windows.Forms.Button();
            this.squareButton = new System.Windows.Forms.Button();
            this.text1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // triangleButton
            // 
            this.triangleButton.Location = new System.Drawing.Point(68, 207);
            this.triangleButton.Name = "triangleButton";
            this.triangleButton.Size = new System.Drawing.Size(159, 85);
            this.triangleButton.TabIndex = 0;
            this.triangleButton.Text = "Draw a Triangle";
            this.triangleButton.UseVisualStyleBackColor = true;
            this.triangleButton.Click += new System.EventHandler(this.triangleButton_Click);
            // 
            // pentagonButton
            // 
            this.pentagonButton.Location = new System.Drawing.Point(259, 85);
            this.pentagonButton.Name = "pentagonButton";
            this.pentagonButton.Size = new System.Drawing.Size(159, 85);
            this.pentagonButton.TabIndex = 0;
            this.pentagonButton.Text = "Draw a Pentagon";
            this.pentagonButton.UseVisualStyleBackColor = true;
            this.pentagonButton.Click += new System.EventHandler(this.pentagonButton_Click);
            // 
            // hexagonButton
            // 
            this.hexagonButton.Location = new System.Drawing.Point(68, 85);
            this.hexagonButton.Name = "hexagonButton";
            this.hexagonButton.Size = new System.Drawing.Size(159, 85);
            this.hexagonButton.TabIndex = 0;
            this.hexagonButton.Text = "Draw a Hexagon";
            this.hexagonButton.UseVisualStyleBackColor = true;
            this.hexagonButton.Click += new System.EventHandler(this.hexagonButton_Click);
            // 
            // squareButton
            // 
            this.squareButton.Location = new System.Drawing.Point(259, 207);
            this.squareButton.Name = "squareButton";
            this.squareButton.Size = new System.Drawing.Size(159, 85);
            this.squareButton.TabIndex = 0;
            this.squareButton.Text = "Draw a Square";
            this.squareButton.UseVisualStyleBackColor = true;
            this.squareButton.Click += new System.EventHandler(this.squareButton_Click);
            // 
            // text1
            // 
            this.text1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.text1.BackColor = System.Drawing.Color.Red;
            this.text1.Font = new System.Drawing.Font("Arial", 18F);
            this.text1.Location = new System.Drawing.Point(68, 23);
            this.text1.Multiline = true;
            this.text1.Name = "text1";
            this.text1.Size = new System.Drawing.Size(350, 46);
            this.text1.TabIndex = 1;
            this.text1.Text = "Draw Regular Figures";
            this.text1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.text1.TextChanged += new System.EventHandler(this.text1_TextChanged);
            // 
            // RegFiguresMenu
            // 
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(492, 396);
            this.Controls.Add(this.triangleButton);
            this.Controls.Add(this.squareButton);
            this.Controls.Add(this.pentagonButton);
            this.Controls.Add(this.hexagonButton);
            this.Controls.Add(this.text1);
            this.ForeColor = System.Drawing.Color.DarkGoldenrod;
            this.Name = "RegFiguresMenu";
            this.Text = "Draw a Regular";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}