using System.Windows.Forms;
using DrawingPlugin.PluginCommands;
using System.Collections.Generic;

namespace DrawingPlugin.Forms
{
    public class PreviewForm : Form
    {
        public event System.Action<EntityData> EntitySelected;
        public event System.Action<EntityData> InsertRequested;

        private ListBox _listBox;
        private Button _insertButton;
        private List<EntityData> _entities;

        public PreviewForm(List<EntityData> entities)
        {
            _entities = entities;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Выберите объект для вставки";
            this.Size = new System.Drawing.Size(400, 500);

            _listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DisplayMember = "DisplayInfo"
            };
            _listBox.DataSource = _entities;

            _insertButton = new Button
            {
                Text = "Вставить",
                Dock = DockStyle.Bottom
            };
            _insertButton.Click += (s, e) =>
            {
                if (_listBox.SelectedItem is EntityData data)
                    InsertRequested?.Invoke(data);
            };

            _listBox.SelectedIndexChanged += (s, e) =>
            {
                if (_listBox.SelectedItem is EntityData data)
                    EntitySelected?.Invoke(data);
            };

            this.Controls.Add(_listBox);
            this.Controls.Add(_insertButton);
        }
    }
}