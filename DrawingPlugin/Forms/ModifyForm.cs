using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace DrawingPlugin.PluginCommands
{
    public class ModifyEntityForm : Form
    {
        private DatabaseBlock _block;
        private PictureBox _thumbnailBox;
        private TextBox _scaleTextBox;
        private TextBox _rotationTextBox;
        private Button _okButton;
        private Button _cancelButton;
        private Button _insertButton;
        private Label _errorLabel;
        private InsertCommands _previewCommand;

        // Properties to store modification parameters
        public double Scale { get; private set; } = 1.0;
        public double RotationAngle { get; private set; } = 0.0;

        public ModifyEntityForm(DatabaseBlock block, InsertCommands previewCommand)
        {
            _block = block;
            _previewCommand = previewCommand;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Set form properties
            this.Text = "Modify Entity";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));  // Thumbnail
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));  // Parameters
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); // Error label
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Buttons

            this.Controls.Add(mainLayout);

            // Title label
            Label titleLabel = new Label
            {
                Text = $"Modify: {_block.Name}",
                Font = new System.Drawing.Font(this.Font.FontFamily, 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            mainLayout.Controls.Add(titleLabel, 0, 0);

            // Thumbnail panel
            Panel thumbnailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainLayout.Controls.Add(thumbnailPanel, 0, 1);

            // Thumbnail
            _thumbnailBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = _block.Thumbnail ?? new Bitmap(150, 150),
                BackColor = Color.White,
                Dock = DockStyle.Fill
            };
            thumbnailPanel.Controls.Add(_thumbnailBox);

            // Parameters panel
            TableLayoutPanel parametersPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                Padding = new Padding(5)
            };
            parametersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            parametersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            parametersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            parametersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.Controls.Add(parametersPanel, 0, 2);

            // Scale parameter
            Label scaleLabel = new Label
            {
                Text = "Scale (1.0 or greater):",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            parametersPanel.Controls.Add(scaleLabel, 0, 0);

            _scaleTextBox = new TextBox
            {
                Text = "1.0",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            parametersPanel.Controls.Add(_scaleTextBox, 1, 0);

            // Rotation parameter
            Label rotationLabel = new Label
            {
                Text = "Rotation angle (-360 to 360):",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            parametersPanel.Controls.Add(rotationLabel, 0, 1);

            _rotationTextBox = new TextBox
            {
                Text = "0.0",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            parametersPanel.Controls.Add(_rotationTextBox, 1, 1);

            // Error label
            _errorLabel = new Label
            {
                Text = "",
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            mainLayout.Controls.Add(_errorLabel, 0, 3);

            // Buttons panel
            Panel buttonsPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            mainLayout.Controls.Add(buttonsPanel, 0, 4);

            // OK button
            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 30),
                Location = new Point(buttonsPanel.Width / 2 - 130, 10),
                Anchor = AnchorStyles.None
            };
            _okButton.Click += OkButton_Click;
            buttonsPanel.Controls.Add(_okButton);

            // Insert button
            _insertButton = new Button
            {
                Text = "Insert",
                Size = new Size(80, 30),
                Location = new Point(buttonsPanel.Width / 2 - 40, 10),
                Anchor = AnchorStyles.None
            };
            _insertButton.Click += InsertButton_Click;
            buttonsPanel.Controls.Add(_insertButton);

            // Cancel button
            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 30),
                Location = new Point(buttonsPanel.Width / 2 + 50, 10),
                Anchor = AnchorStyles.None
            };
            buttonsPanel.Controls.Add(_cancelButton);

            // Set accept and cancel buttons
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;

            // Handle resize to reposition buttons
            buttonsPanel.Resize += (s, e) =>
            {
                _okButton.Location = new Point(buttonsPanel.Width / 2 - 130, 10);
                _insertButton.Location = new Point(buttonsPanel.Width / 2 - 40, 10);
                _cancelButton.Location = new Point(buttonsPanel.Width / 2 + 50, 10);
            };
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (ValidateInputs())
            {
                // Store the values
                Scale = double.Parse(_scaleTextBox.Text);
                RotationAngle = double.Parse(_rotationTextBox.Text);

                // Close the form with OK result
                this.DialogResult = DialogResult.OK;
            }
        }

        private void InsertButton_Click(object sender, EventArgs e)
        {
            if (ValidateInputs())
            {
                // Store the values
                Scale = double.Parse(_scaleTextBox.Text);
                RotationAngle = double.Parse(_rotationTextBox.Text);

                // Hide the form but don't close it yet
                this.Hide();

                // Start the insertion process
                InsertSelectedBlock();
            }
        }

        private bool ValidateInputs()
        {
            // Validate scale
            if (!double.TryParse(_scaleTextBox.Text, out double scale) || scale <= 0)
            {
                _errorLabel.Text = "Scale must be a positive number.";
                return false;
            }

            // Validate rotation angle
            if (!double.TryParse(_rotationTextBox.Text, out double angle) || angle < -360 || angle > 360)
            {
                _errorLabel.Text = "Rotation angle must be between -360 and 360.";
                return false;
            }

            _errorLabel.Text = "";
            return true;
        }

        private void InsertSelectedBlock()
        {
            // Get the active document and editor
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // Prompt the user to specify an insertion point
                PromptPointOptions pPtOpts = new PromptPointOptions("\nSpecify insertion point: ");
                pPtOpts.AllowNone = false;

                // Show the form again if the user cancels
                PromptPointResult pPtRes = ed.GetPoint(pPtOpts);
                if (pPtRes.Status != PromptStatus.OK)
                {
                    this.Show();
                    return;
                }

                // Get the insertion point
                Point3d insertionPoint = pPtRes.Value;

                // Insert the block at the specified point with modifications
                bool success = _previewCommand.InsertBlockAtPoint(_block, insertionPoint, Scale, RotationAngle);

                if (success)
                {
                    ed.WriteMessage($"\nBlock '{_block.Name}' inserted successfully.");
                    // Close the form after successful insertion
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ed.WriteMessage($"\nFailed to insert block '{_block.Name}'.");
                    this.Show();
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError during insertion: {ex.Message}");
                this.Show();
            }
        }
    }
}
