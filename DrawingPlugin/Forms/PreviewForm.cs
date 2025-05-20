using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Font = System.Drawing.Font;

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
        private Button _deleteButton;
        private Label _dbPathLabel;
        private Label _blocksCountLabel;
        
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
            this.Size = new Size(1000, 700); // Увеличенный размер
            this.MinimumSize = new Size(800, 600); // Минимальный размер
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable; // Изменяемый размер
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // Create main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 85F)); // Уменьшаем долю для блоков
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15F)); // Увеличиваем долю для кнопок

            this.Controls.Add(mainLayout);

            // Create blocks panel
            _blocksPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(15) // Увеличенные отступы
            };

            mainLayout.Controls.Add(_blocksPanel, 0, 0);

            // Create buttons panel - используем TableLayoutPanel для лучшего размещения
            TableLayoutPanel buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 5,
                Padding = new Padding(10)
            };

            buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Для меток
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5F)); // Для кнопки Delete
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5F)); // Для кнопки Modify
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5F)); // Для кнопки Insert
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5F)); // Для кнопки Close

            mainLayout.Controls.Add(buttonsPanel, 0, 1);

            // Add database path label
            _dbPathLabel = new Label
            {
                Text = $"Database: {_dbPath}",
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font.FontFamily, 9),
                AutoEllipsis = true // Добавляем многоточие для длинных путей
            };

            buttonsPanel.Controls.Add(_dbPathLabel, 0, 0);
            buttonsPanel.SetColumnSpan(_dbPathLabel, 5); // Растягиваем на все колонки

            // Add blocks count label
            _blocksCountLabel = new Label
            {
                Text = $"Blocks: {_blocks.Count}",
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font.FontFamily, 9)
            };

            buttonsPanel.Controls.Add(_blocksCountLabel, 0, 1);

            // Add Delete button
            _deleteButton = new Button
            {
                Text = "Delete",
                Size = new Size(110, 35), // Увеличенный размер
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Enabled = false, // Initially disabled until a block is selected
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };

            _deleteButton.Click += DeleteButton_Click;
            buttonsPanel.Controls.Add(_deleteButton, 1, 1);

            // Add Modify button
            _modifyButton = new Button
            {
                Text = "Modify",
                Size = new Size(110, 35), // Увеличенный размер
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Enabled = false, // Initially disabled until a block is selected
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };

            _modifyButton.Click += ModifyButton_Click;
            buttonsPanel.Controls.Add(_modifyButton, 2, 1);

            // Add Insert button
            _insertButton = new Button
            {
                Text = "Insert",
                Size = new Size(110, 35), // Увеличенный размер
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Enabled = false, // Initially disabled until a block is selected
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };

            _insertButton.Click += InsertButton_Click;
            buttonsPanel.Controls.Add(_insertButton, 3, 1);

            // Add Close button
            _closeButton = new Button
            {
                Text = "Close",
                Size = new Size(110, 35), // Увеличенный размер
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };

            _closeButton.Click += (s, e) => this.Close();
            buttonsPanel.Controls.Add(_closeButton, 4, 1);

            // Обработчик изменения размера формы для обновления меток
            this.Resize += (s, e) => {
                _dbPathLabel.Text = $"Database: {_dbPath}";
                _blocksCountLabel.Text = $"Blocks: {_blocks.Count}";
            };
        }

        private void PopulateBlocksList()
        {
            foreach (DatabaseBlock block in _blocks)
            {
                // Create a panel for each block - увеличиваем размер
                Panel blockPanel = new Panel
                {
                    Width = 220, // Увеличенная ширина
                    Height = 240, // Увеличенная высота
                    Margin = new Padding(10), // Увеличенные отступы
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = block // Store the block in the Tag property for easy access
                };

                // Add thumbnail - centered in the panel
                PictureBox thumbnailBox = new PictureBox
                {
                    Width = 180, // Увеличенная ширина
                    Height = 180, // Увеличенная высота
                    Location = new Point((blockPanel.Width - 180) / 2, 10), // Center horizontally
                    SizeMode = PictureBoxSizeMode.Zoom, // Изменено на Zoom для лучшего масштабирования
                    Image = block.Thumbnail ?? new Bitmap(180, 180),
                    BackColor = Color.White,
                    Tag = block // Store the block in the Tag property for easy access
                };

                blockPanel.Controls.Add(thumbnailBox);

                // Add name label - увеличиваем размер шрифта
                Label nameLabel = new Label
                {
                    Text = block.Name,
                    Location = new Point(5, 195),
                    Width = 210, // Увеличенная ширина
                    Height = 20, // Фиксированная высота
                    Font = new System.Drawing.Font(this.Font.FontFamily, 10, FontStyle.Bold), // Увеличенный шрифт
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleCenter // Center text
                };

                blockPanel.Controls.Add(nameLabel);

                // Add date label
                Label dateLabel = new Label
                {
                    Text = block.CreatedAt,
                    Location = new Point(5, 215),
                    Width = 210, // Увеличенная ширина
                    Height = 20, // Фиксированная высота
                    Font = new System.Drawing.Font(this.Font.FontFamily, 8),
                    ForeColor = Color.Gray,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleCenter // Center text
                };

                blockPanel.Controls.Add(dateLabel);

                // Add click handler for selection functionality
                blockPanel.Click += (s, e) => SelectBlock(blockPanel);
                thumbnailBox.Click += (s, e) => SelectBlock(blockPanel);
                nameLabel.Click += (s, e) => SelectBlock(blockPanel);
                dateLabel.Click += (s, e) => SelectBlock(blockPanel);

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
            _deleteButton.Enabled = true;
        
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
                // Open the modify form - передаем ссылку на текущую форму
                using (ModifyEntityForm modifyForm = new ModifyEntityForm(_selectedBlock, _previewCommand, this))
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

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (_selectedBlock != null)
            {
                // Confirm deletion
                DialogResult result = MessageBox.Show(
                    $"Are you sure you want to delete the block '{_selectedBlock.Name}'?",
                    "Confirm Deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                    
                if (result == DialogResult.Yes)
                {
                    // Delete the block from the database
                    if (DeleteBlockFromDatabase(_selectedBlock.Id))
                    {
                        // Remove the block from the list
                        _blocks.Remove(_selectedBlock);
                        
                        // Remove the panel from the UI
                        if (_selectedPanel != null)
                        {
                            _blocksPanel.Controls.Remove(_selectedPanel);
                            _selectedPanel = null;
                        }
                        
                        // Reset selection
                        _selectedBlock = null;
                        
                        // Disable buttons
                        _insertButton.Enabled = false;
                        _modifyButton.Enabled = false;
                        _deleteButton.Enabled = false;
                        
                        // Update the form title
                        this.Text = "Database Blocks Preview";
                        
                        // Update blocks count label
                        _blocksCountLabel.Text = $"Blocks: {_blocks.Count}";
                    }
                }
            }
        }

        // Добавить метод для удаления блока из базы данных
        private bool DeleteBlockFromDatabase(int blockId)
        {
            try
            {
                string connectionString = $"Data Source={_dbPath};Version=3;";
                
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    
                    using (SQLiteCommand command = new SQLiteCommand(
                        "DELETE FROM Entities WHERE Id = @Id",
                        connection))
                    {
                        command.Parameters.AddWithValue("@Id", blockId);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage($"\nError deleting block: {ex.Message}");
                MessageBox.Show($"Error deleting block: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
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
            Document doc = Application.DocumentManager.MdiActiveDocument;
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
