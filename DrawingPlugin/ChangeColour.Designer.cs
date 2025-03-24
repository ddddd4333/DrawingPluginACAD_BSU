using System;
using System.ComponentModel;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangeColor));
            this.redButton = new System.Windows.Forms.Button();
            this.yellowButton = new System.Windows.Forms.Button();
            this.greenButton = new System.Windows.Forms.Button();
            this.blueButton = new System.Windows.Forms.Button();
            this.cyanButton = new System.Windows.Forms.Button();
            this.magentaButton = new System.Windows.Forms.Button();
            this.whiteButton = new System.Windows.Forms.Button();
            //
            //redButton
            //
            this.redButton.Location = new System.Drawing.Point(0, 0);
            this.redButton.Name = "redButton";
            this.redButton.Size = new System.Drawing.Size(159, 85);
            this.redButton.TabIndex = 0;
            this.redButton.Text = "Red";
            this.redButton.UseVisualStyleBackColor = true;
            this.redButton.Click += new EventHandler(this.Red_Change);
            //
            //yellowButton
            //
            this.yellowButton.Location = new System.Drawing.Point(0, 85);
            this.yellowButton.Name = "yellowButton";
            this.yellowButton.Size = new System.Drawing.Size(159, 85);
            this.yellowButton.TabIndex = 1;
            this.yellowButton.Text = "Yellow";
            this.yellowButton.UseVisualStyleBackColor = true;
            this.yellowButton.Click += new EventHandler(this.Yellow_Change);
            //
            //greenButton
            //
            this.greenButton.Location = new System.Drawing.Point(0, 115);
            this.greenButton.Name = "greenButton";
            this.greenButton.Size = new System.Drawing.Size(159, 85);
            this.greenButton.TabIndex = 2;
            this.greenButton.Text = "Green";
            this.greenButton.UseVisualStyleBackColor = true;
            this.greenButton.Click += new EventHandler(this.Green_Change);
            //
            //blueButton
            //
            this.blueButton.Location = new System.Drawing.Point(0, 150);
            this.blueButton.Name = "blueButton";
            this.blueButton.Size = new System.Drawing.Size(159, 85);
            this.blueButton.TabIndex = 3;
            this.blueButton.Text = "Blue";
            this.blueButton.UseVisualStyleBackColor = true;
            this.blueButton.Click += new EventHandler(this.Blue_Change);
            //
            //cyanButton
            //
            this.cyanButton.Location = new System.Drawing.Point(0, 180);
            this.cyanButton.Name = "cyanButton";
            this.cyanButton.Size = new System.Drawing.Size(159, 85);
            this.cyanButton.TabIndex = 4;
            this.cyanButton.Text = "Cyan";
            this.cyanButton.UseVisualStyleBackColor = true;
            this.cyanButton.Click += new EventHandler(this.Cyan_Change);
            //
            //magentaButton
            //
            this.magentaButton.Location = new System.Drawing.Point(0, 190);
            this.magentaButton.Name = "magentaButton";
            this.magentaButton.Size = new System.Drawing.Size(159, 85);
            this.magentaButton.TabIndex = 5;
            this.magentaButton.Text = "Magenta";
            this.magentaButton.UseVisualStyleBackColor = true;
            this.magentaButton.Click += new EventHandler(this.Magenta_Change);
            //
            //whiteButton
            //
            this.whiteButton.Location = new System.Drawing.Point(0, 170);
            this.whiteButton.Name = "whiteButton";
            this.whiteButton.Size = new System.Drawing.Size(159, 85);
            this.whiteButton.TabIndex = 6;
            this.whiteButton.Text = "White";
            this.whiteButton.UseVisualStyleBackColor = true;
            this.whiteButton.Click += new EventHandler(this.White_Change);
            //
            //Form
            //
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "ChangeColour";
            this.Controls.Add(redButton);
            this.Controls.Add(yellowButton);
            this.Controls.Add(greenButton);
            this.Controls.Add(blueButton);
            this.Controls.Add(cyanButton);
            this.Controls.Add(magentaButton);
            this.Controls.Add(whiteButton);
        }

        #endregion
    }
}