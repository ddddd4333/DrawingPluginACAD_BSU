using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Drawing;
using System.Windows.Forms;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Font = System.Drawing.Font;

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
        private Form _parentForm; // Добавляем ссылку на родительскую форму

        // Добавим функционал выбора элемента блока и изменения его длины
        // В начале класса добавим новые переменные:

        private ComboBox _entitySelector;
        private Label _entityLabel;
        private TextBox _lengthTextBox;
        private Label _lengthLabel;
        private int _selectedEntityIndex = -1;

        // Properties to store modification parameters
        public double Scale { get; private set; } = 1.0;
        public double RotationAngle { get; private set; } = 0.0;

        public ModifyEntityForm(DatabaseBlock block, InsertCommands previewCommand, Form parentForm)
        {
            _block = block;
            _previewCommand = previewCommand;
            _parentForm = parentForm; // Сохраняем ссылку на родительскую форму
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Set form properties - увеличиваем размер формы
            this.Text = "Modify Entity";
            this.Size = new Size(500, 600); // Увеличенный размер
            this.MinimumSize = new Size(450, 550); // Минимальный размер
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable; // Изменяемый размер
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // Create main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                Padding = new Padding(15) // Увеличенные отступы
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Title - увеличенная высота
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));  // Thumbnail
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));  // Parameters - увеличенная доля
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Error label - увеличенная высота
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); // Buttons - увеличенная высота

            this.Controls.Add(mainLayout);

            // Title label
            Label titleLabel = new Label
            {
                Text = $"Modify: {_block.Name}",
                Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoEllipsis = true // Добавляем многоточие для длинных имен
            };
            mainLayout.Controls.Add(titleLabel, 0, 0);

            // Thumbnail panel
            Panel thumbnailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5) // Добавляем отступы
            };
            mainLayout.Controls.Add(thumbnailPanel, 0, 1);

            // Thumbnail
            _thumbnailBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom, // Изменено на Zoom для лучшего масштабирования
                Image = _block.Thumbnail ?? new Bitmap(150, 150),
                BackColor = Color.White,
                Dock = DockStyle.Fill
            };
            thumbnailPanel.Controls.Add(_thumbnailBox);

            // Parameters panel
            TableLayoutPanel parametersPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 2,
                Padding = new Padding(5),
                Margin = new Padding(5) // Добавляем отступы
            };
            parametersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            parametersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            parametersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            parametersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            parametersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            parametersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.Controls.Add(parametersPanel, 0, 2);

            // Scale parameter
            Label scaleLabel = new Label
            {
                Text = "Scale (1.0 or greater):",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Margin = new Padding(3) // Добавляем отступы
            };
            parametersPanel.Controls.Add(scaleLabel, 0, 0);

            _scaleTextBox = new TextBox
            {
                Text = "1.0",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };
            parametersPanel.Controls.Add(_scaleTextBox, 1, 0);

            // Rotation parameter
            Label rotationLabel = new Label
            {
                Text = "Rotation angle (-360 to 360):",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Margin = new Padding(3) // Добавляем отступы
            };
            parametersPanel.Controls.Add(rotationLabel, 0, 1);

            _rotationTextBox = new TextBox
            {
                Text = "0.0",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };
            parametersPanel.Controls.Add(_rotationTextBox, 1, 1);

            // Entity selector
            _entityLabel = new Label
            {
                Text = "Select entity to modify:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Margin = new Padding(3) // Добавляем отступы
            };
            parametersPanel.Controls.Add(_entityLabel, 0, 2);

            _entitySelector = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(5),
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };
            parametersPanel.Controls.Add(_entitySelector, 1, 2);

            // Populate entity selector
            PopulateEntitySelector();

            // Entity length modifier
            _lengthLabel = new Label
            {
                Text = "Length factor (0.1-10):",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Margin = new Padding(3) // Добавляем отступы
            };
            parametersPanel.Controls.Add(_lengthLabel, 0, 3);

            _lengthTextBox = new TextBox
            {
                Text = "1.0",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Enabled = false, // Initially disabled until an entity is selected
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };
            parametersPanel.Controls.Add(_lengthTextBox, 1, 3);

            // Add event handler for entity selection
            _entitySelector.SelectedIndexChanged += EntitySelector_SelectedIndexChanged;

            // Error label
            _errorLabel = new Label
            {
                Text = "",
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 5, 0) // Горизонтальные отступы
            };
            mainLayout.Controls.Add(_errorLabel, 0, 3);

            // Buttons panel - используем TableLayoutPanel для лучшего размещения кнопок
            TableLayoutPanel buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(5)
            };
            
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            
            mainLayout.Controls.Add(buttonsPanel, 0, 4);

            // OK button
            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(100, 35), // Увеличенный размер
                Anchor = AnchorStyles.None,
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };
            _okButton.Click += OkButton_Click;
            buttonsPanel.Controls.Add(_okButton, 0, 0);
            buttonsPanel.SetCellPosition(_okButton, new TableLayoutPanelCellPosition(0, 0));

            // Insert button
            _insertButton = new Button
            {
                Text = "Insert",
                Size = new Size(100, 35), // Увеличенный размер
                Anchor = AnchorStyles.None,
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };
            _insertButton.Click += InsertButton_Click;
            buttonsPanel.Controls.Add(_insertButton, 1, 0);
            buttonsPanel.SetCellPosition(_insertButton, new TableLayoutPanelCellPosition(1, 0));

            // Cancel button
            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 35), // Увеличенный размер
                Anchor = AnchorStyles.None,
                Font = new Font(this.Font.FontFamily, 10) // Увеличенный шрифт
            };
            buttonsPanel.Controls.Add(_cancelButton, 2, 0);
            buttonsPanel.SetCellPosition(_cancelButton, new TableLayoutPanelCellPosition(2, 0));

            // Set accept and cancel buttons
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private void PopulateEntitySelector()
        {
            _entitySelector.Items.Clear();
            _entitySelector.Items.Add("None (modify entire block)");

            for (int i = 0; i < _block.EntitiesData.Count; i++)
            {
                EntityData entity = _block.EntitiesData[i];
                _entitySelector.Items.Add($"{i + 1}: {entity.Type}");
            }

            _entitySelector.SelectedIndex = 0;
        }

        private void EntitySelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedEntityIndex = _entitySelector.SelectedIndex - 1; // -1 because first item is "None"
            _lengthTextBox.Enabled = _selectedEntityIndex >= 0;

            if (_selectedEntityIndex >= 0)
            {
                // Reset length factor to 1.0 when selecting a new entity
                _lengthTextBox.Text = "1.0";
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

            // Validate length factor if an entity is selected
            if (_selectedEntityIndex >= 0)
            {
                if (!double.TryParse(_lengthTextBox.Text, out double length) || length < 0.1 || length > 10)
                {
                    _errorLabel.Text = "Length factor must be between 0.1 and 10.";
                    return false;
                }
            }

            _errorLabel.Text = "";
            return true;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (ValidateInputs())
            {
                // Store the values
                Scale = double.Parse(_scaleTextBox.Text);
                RotationAngle = double.Parse(_rotationTextBox.Text);

                // Apply length modification if an entity is selected
                if (_selectedEntityIndex >= 0)
                {
                    double lengthFactor = double.Parse(_lengthTextBox.Text);
                    ModifyEntityLength(_selectedEntityIndex, lengthFactor);
                }

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

                // Apply length modification if an entity is selected
                if (_selectedEntityIndex >= 0)
                {
                    double lengthFactor = double.Parse(_lengthTextBox.Text);
                    ModifyEntityLength(_selectedEntityIndex, lengthFactor);
                }

                // Hide the form but don't close it yet
                this.Hide();
                
                // Также скрываем родительскую форму
                if (_parentForm != null)
                {
                    _parentForm.Hide();
                }

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
                    if (_parentForm != null)
                    {
                        _parentForm.Show();
                    }
                    return;
                }

                // Get the insertion point
                Point3d insertionPoint = pPtRes.Value;

                // Insert the block at the specified point with modifications
                bool success = _previewCommand.InsertBlockAtPoint(_block, insertionPoint, Scale, RotationAngle);

                if (success)
                {
                    ed.WriteMessage($"\nBlock '{_block.Name}' inserted successfully.");
                    
                    // Закрываем родительскую форму при успешной вставке
                    if (_parentForm != null)
                    {
                        _parentForm.DialogResult = DialogResult.OK;
                        _parentForm.Close();
                    }
                    
                    // Close the form after successful insertion
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ed.WriteMessage($"\nFailed to insert block '{_block.Name}'.");
                    this.Show();
                    if (_parentForm != null)
                    {
                        _parentForm.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nError during insertion: {ex.Message}");
                this.Show();
                if (_parentForm != null)
                {
                    _parentForm.Show();
                }
            }
        }

        private void ModifyEntityLength(int entityIndex, double lengthFactor)
        {
            if (entityIndex < 0 || entityIndex >= _block.EntitiesData.Count)
                return;

            EntityData entity = _block.EntitiesData[entityIndex];

            switch (entity.Type)
            {
                case "Line":
                    ModifyLineLength(entity, lengthFactor);
                    break;
                case "Polyline":
                    ModifyPolylineLength(entity, lengthFactor);
                    break;
                case "Arc":
                    ModifyArcLength(entity, lengthFactor);
                    break;
                case "Ellipse":
                    ModifyEllipseLength(entity, lengthFactor);
                    break;
            }
        }

        private void ModifyLineLength(EntityData line, double lengthFactor)
        {
            if (line.Points == null || line.Points.Count < 2)
                return;

            // Get start and end points
            double[] startPoint = line.Points[0];
            double[] endPoint = line.Points[1];

            // Calculate midpoint
            double midX = (startPoint[0] + endPoint[0]) / 2;
            double midY = (startPoint[1] + endPoint[1]) / 2;

            // Calculate vector from midpoint to endpoints
            double vx1 = startPoint[0] - midX;
            double vy1 = startPoint[1] - midY;
            double vx2 = endPoint[0] - midX;
            double vy2 = endPoint[1] - midY;

            // Scale vectors by length factor
            vx1 *= lengthFactor;
            vy1 *= lengthFactor;
            vx2 *= lengthFactor;
            vy2 *= lengthFactor;

            // Calculate new endpoints
            startPoint[0] = midX + vx1;
            startPoint[1] = midY + vy1;
            endPoint[0] = midX + vx2;
            endPoint[1] = midY + vy2;
        }

        private void ModifyPolylineLength(EntityData polyline, double lengthFactor)
        {
            if (polyline.Points == null || polyline.Points.Count < 2)
                return;

            // Calculate centroid of the polyline
            double centroidX = 0, centroidY = 0;
            foreach (double[] point in polyline.Points)
            {
                centroidX += point[0];
                centroidY += point[1];
            }
            centroidX /= polyline.Points.Count;
            centroidY /= polyline.Points.Count;

            // Scale each point relative to the centroid
            for (int i = 0; i < polyline.Points.Count; i++)
            {
                double[] point = polyline.Points[i];
                double vx = point[0] - centroidX;
                double vy = point[1] - centroidY;

                point[0] = centroidX + vx * lengthFactor;
                point[1] = centroidY + vy * lengthFactor;
            }
        }

        private void ModifyArcLength(EntityData arc, double lengthFactor)
        {
            if (arc.Center == null || arc.Center.Length < 2)
                return;

            // For arcs, we'll modify the radius
            arc.Radius *= lengthFactor;
        }

        private void ModifyEllipseLength(EntityData ellipse, double lengthFactor)
        {
            if (ellipse.Center == null || ellipse.Center.Length < 2 ||
                ellipse.MajorAxis == null || ellipse.MajorAxis.Length < 2)
                return;

            // Scale the major axis
            ellipse.MajorAxis[0] *= lengthFactor;
            ellipse.MajorAxis[1] *= lengthFactor;
        }
    }
}
