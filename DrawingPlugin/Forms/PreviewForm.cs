using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DrawingPlugin.PluginCommands
{
    public class PreviewForm : Form
    {
        private List<DatabaseBlock> _blocks;
        private FlowLayoutPanel _blocksPanel;
        private Button _closeButton;
        private Button _insertButton;
        private Button _modifyButton;
        private string _dbPath;
        private DatabaseBlock _selectedBlock = null;
        private Panel _selectedPanel = null;
        private InsertCommands _previewCommand;
        
        // Properties for modification
        private double _scale = 1.0;
        private double _rotationAngle = 0.0;

        public PreviewForm(List<DatabaseBlock> blocks, string dbPath)
        {
            _blocks = blocks;
            _dbPath = dbPath;
            _previewCommand = new InsertCommands();
            InitializeComponent();
            PopulateBlocksList();
        }

        private void InitializeComponent()
        {
            this.Text = "Database Blocks Preview";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));

            this.Controls.Add(mainLayout);

            // Create blocks panel
            _blocksPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(10)
            };

            mainLayout.Controls.Add(_blocksPanel, 0, 0);

            // Create buttons panel
            Panel buttonsPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            mainLayout.Controls.Add(buttonsPanel, 0, 1);

            // Add database path label
            Label dbPathLabel = new Label
            {
                Text = $"Database: {_dbPath}",
                AutoSize = true,
                Location = new Point(10, 15),
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };

            buttonsPanel.Controls.Add(dbPathLabel);

            // Add blocks count label
            Label blocksCountLabel = new Label
            {
                Text = $"Blocks: {_blocks.Count}",
                AutoSize = true,
                Location = new Point(10, 35),
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };

            buttonsPanel.Controls.Add(blocksCountLabel);

            // Add buttons
            _closeButton = new Button
            {
                Text = "Close",
                Size = new Size(80, 30),
                Location = new Point(buttonsPanel.Width - 90, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            _closeButton.Click += (s, e) => this.Close();
            buttonsPanel.Controls.Add(_closeButton);

            // Add Insert button - now enabled but will be disabled until a block is selected
            _insertButton = new Button
            {
                Text = "Insert",
                Size = new Size(80, 30),
                Location = new Point(buttonsPanel.Width - 180, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Enabled = false // Initially disabled until a block is selected
            };

            _insertButton.Click += InsertButton_Click;
            buttonsPanel.Controls.Add(_insertButton);
            
            // Add Modify button
            _modifyButton = new Button
            {
                Text = "Modify",
                Size = new Size(80, 30),
                Location = new Point(buttonsPanel.Width - 270, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Enabled = false // Initially disabled until a block is selected
            };

            _modifyButton.Click += ModifyButton_Click;
            buttonsPanel.Controls.Add(_modifyButton);
        }

        private void PopulateBlocksList()
        {
            foreach (DatabaseBlock block in _blocks)
            {
                // Create a panel for each block
                Panel blockPanel = new Panel
                {
                    Width = 180,
                    Height = 200,
                    Margin = new Padding(5),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = block // Store the block in the Tag property for easy access
                };

                // Add thumbnail - centered in the panel
                PictureBox thumbnailBox = new PictureBox
                {
                    Width = 150,
                    Height = 150,
                    Location = new Point((blockPanel.Width - 150) / 2, 10), // Center horizontally
                    SizeMode = PictureBoxSizeMode.CenterImage, // Changed to CenterImage
                    Image = block.Thumbnail ?? new Bitmap(150, 150),
                    BackColor = Color.White,
                    Tag = block // Store the block in the Tag property for easy access
                };

                blockPanel.Controls.Add(thumbnailBox);

                // Add name label
                Label nameLabel = new Label
                {
                    Text = block.Name,
                    Location = new Point(5, 165),
                    Width = 170,
                    Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleCenter // Center text
                };

                blockPanel.Controls.Add(nameLabel);

                // Add date label
                Label dateLabel = new Label
                {
                    Text = block.CreatedAt,
                    Location = new Point(5, 180),
                    Width = 170,
                    Font = new System.Drawing.Font(this.Font.FontFamily, 8),
                    ForeColor = Color.Gray,
                    TextAlign = ContentAlignment.MiddleCenter // Center text
                };

                blockPanel.Controls.Add(dateLabel);

                // Add click handler for selection functionality
                blockPanel.Click += (s, e) => SelectBlock(blockPanel);
                thumbnailBox.Click += (s, e) => SelectBlock(blockPanel);

                // Add to flow layout
                _blocksPanel.Controls.Add(blockPanel);
            }
        }

        private void SelectBlock(Panel blockPanel)
        {
            // Deselect previously selected panel if any
            if (_selectedPanel != null)
            {
                _selectedPanel.BackColor = SystemColors.Control;
                _selectedPanel.BorderStyle = BorderStyle.FixedSingle;
            }

            // Select the new panel
            _selectedPanel = blockPanel;
            _selectedBlock = blockPanel.Tag as DatabaseBlock;
        
            // Highlight the selected panel
            _selectedPanel.BackColor = Color.LightBlue;
            _selectedPanel.BorderStyle = BorderStyle.Fixed3D;
        
            // Enable the Insert and Modify buttons
            _insertButton.Enabled = true;
            _modifyButton.Enabled = true;
        
            // Show selection info
            this.Text = $"Database Blocks Preview - Selected: {_selectedBlock.Name}";
            
            // Reset modification parameters
            _scale = 1.0;
            _rotationAngle = 0.0;
        }

        private void ModifyButton_Click(object sender, EventArgs e)
        {
            if (_selectedBlock != null)
            {
                // Open the modify form
                using (ModifyEntityForm modifyForm = new ModifyEntityForm(_selectedBlock, _previewCommand))
                {
                    if (modifyForm.ShowDialog() == DialogResult.OK)
                    {
                        // Get the modification parameters
                        _scale = modifyForm.Scale;
                        _rotationAngle = modifyForm.RotationAngle;
                        
                        // Update the form title to show that modifications will be applied
                        this.Text = $"Database Blocks Preview - Selected: {_selectedBlock.Name} (Scale: {_scale}, Rotation: {_rotationAngle}°)";
                    }
                }
            }
        }

        private void InsertButton_Click(object sender, EventArgs e)
        {
            if (_selectedBlock != null)
            {
                // Hide the form but don't close it yet
                this.Hide();
            
                // Start the insertion process
                InsertSelectedBlock();
            }
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
                bool success = _previewCommand.InsertBlockAtPoint(_selectedBlock, insertionPoint, _scale, _rotationAngle);
            
                if (success)
                {
                    ed.WriteMessage($"\nBlock '{_selectedBlock.Name}' inserted successfully.");
                    // Close the form after successful insertion
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ed.WriteMessage($"\nFailed to insert block '{_selectedBlock.Name}'.");
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
