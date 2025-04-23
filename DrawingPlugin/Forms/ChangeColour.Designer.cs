using System;
using System.ComponentModel;
using System.Drawing;

namespace DrawingPlugin
{
    partial class ChangeColor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.Windows.Forms.Button redButton;
        private System.Windows.Forms.Button yellowButton;
        private System.Windows.Forms.Button greenButton;
        private System.Windows.Forms.Button blueButton;
        private System.Windows.Forms.Button cyanButton;
        private System.Windows.Forms.Button magentaButton;
        private System.Windows.Forms.Button whiteButton;
        private System.ComponentModel.Container components = null;

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
            this.redButton = new System.Windows.Forms.Button();
            this.yellowButton = new System.Windows.Forms.Button();
            this.greenButton = new System.Windows.Forms.Button();
            this.blueButton = new System.Windows.Forms.Button();
            this.cyanButton = new System.Windows.Forms.Button();
            this.magentaButton = new System.Windows.Forms.Button();
            this.whiteButton = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // redButton
            // 
            this.redButton.Location = new System.Drawing.Point(276, 283);
            this.redButton.Name = "redButton";
            this.redButton.Size = new System.Drawing.Size(159, 85);
            this.redButton.TabIndex = 0;
            this.redButton.Text = "Red";
            this.redButton.UseVisualStyleBackColor = true;
            this.redButton.Click += new System.EventHandler(this.Red_Change);
            // 
            // yellowButton
            // 
            this.yellowButton.Location = new System.Drawing.Point(276, 170);
            this.yellowButton.Name = "yellowButton";
            this.yellowButton.Size = new System.Drawing.Size(159, 85);
            this.yellowButton.TabIndex = 1;
            this.yellowButton.Text = "Yellow";
            this.yellowButton.UseVisualStyleBackColor = true;
            this.yellowButton.Click += new System.EventHandler(this.Yellow_Change);
            // 
            // greenButton
            // 
            this.greenButton.Location = new System.Drawing.Point(465, 283);
            this.greenButton.Name = "greenButton";
            this.greenButton.Size = new System.Drawing.Size(159, 85);
            this.greenButton.TabIndex = 2;
            this.greenButton.Text = "Green";
            this.greenButton.UseVisualStyleBackColor = true;
            this.greenButton.Click += new System.EventHandler(this.Green_Change);
            // 
            // blueButton
            // 
            this.blueButton.Location = new System.Drawing.Point(465, 170);
            this.blueButton.Name = "blueButton";
            this.blueButton.Size = new System.Drawing.Size(159, 85);
            this.blueButton.TabIndex = 3;
            this.blueButton.Text = "Blue";
            this.blueButton.UseVisualStyleBackColor = true;
            this.blueButton.Click += new System.EventHandler(this.Blue_Change);
            // 
            // cyanButton
            // 
            this.cyanButton.Location = new System.Drawing.Point(81, 58);
            this.cyanButton.Name = "cyanButton";
            this.cyanButton.Size = new System.Drawing.Size(159, 85);
            this.cyanButton.TabIndex = 4;
            this.cyanButton.Text = "Cyan";
            this.cyanButton.UseVisualStyleBackColor = true;
            this.cyanButton.Click += new System.EventHandler(this.Cyan_Change);
            // 
            // magentaButton
            // 
            this.magentaButton.Location = new System.Drawing.Point(81, 283);
            this.magentaButton.Name = "magentaButton";
            this.magentaButton.Size = new System.Drawing.Size(159, 85);
            this.magentaButton.TabIndex = 5;
            this.magentaButton.Text = "Magenta";
            this.magentaButton.UseVisualStyleBackColor = true;
            this.magentaButton.Click += new System.EventHandler(this.Magenta_Change);
            // 
            // whiteButton
            // 
            this.whiteButton.Location = new System.Drawing.Point(81, 170);
            this.whiteButton.Name = "whiteButton";
            this.whiteButton.Size = new System.Drawing.Size(159, 85);
            this.whiteButton.TabIndex = 6;
            this.whiteButton.Text = "White";
            this.whiteButton.UseVisualStyleBackColor = true;
            this.whiteButton.Click += new System.EventHandler(this.White_Change);
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Onyx", 28.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(359, 79);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(218, 61);
            this.textBox1.TabIndex = 7;
            this.textBox1.Text = "ColorPad";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // ChangeColor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(647, 450);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.redButton);
            this.Controls.Add(this.yellowButton);
            this.Controls.Add(this.greenButton);
            this.Controls.Add(this.blueButton);
            this.Controls.Add(this.cyanButton);
            this.Controls.Add(this.magentaButton);
            this.Controls.Add(this.whiteButton);
            this.Name = "ChangeColor";
            this.Text = "ChangeColour";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
    }
}