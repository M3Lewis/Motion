using System.Windows;
using Grasshopper.Kernel;
using System.Collections.Generic;

namespace Motion.Animation
{
    public partial class JumpToComponentDialog : Window
    {
        public class ComponentItem
        {
            public IGH_DocumentObject Component { get; set; }
            public string DisplayName { get; set; }
        }

        public IGH_DocumentObject SelectedComponent { get; private set; }

        public JumpToComponentDialog(IEnumerable<IGH_DocumentObject> components)
        {
            InitializeComponent();

            foreach (var component in components)
            {
                ComponentsComboBox.Items.Add(new ComponentItem
                {
                    Component = component,
                    DisplayName = $"{component.Name} ({component.NickName})"
                });
            }

            if (ComponentsComboBox.Items.Count > 0)
                ComponentsComboBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ComponentsComboBox.SelectedItem as ComponentItem;
            if (selectedItem != null)
            {
                SelectedComponent = selectedItem.Component;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 