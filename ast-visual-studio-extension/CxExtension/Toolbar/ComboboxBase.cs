using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal abstract class ComboboxBase
    {
        protected CxToolbar cxToolbar;
        protected ComboBox comboBox;
        protected string previousText = string.Empty;
        protected bool isFiltering = false;
        protected List<ComboBoxItem> allItems;
        public ComboboxBase(CxToolbar cxToolbar, ComboBox comboBox)
        {
            this.cxToolbar = cxToolbar;
            this.comboBox = comboBox;
            allItems = new List<ComboBoxItem>();
        }
         
        public void OnTextChanged(object sender, EventArgs e)
        {
            if (comboBox == null) return;

            string newText = comboBox.Text;
            if (newText == previousText) return;

            Mouse.OverrideCursor = Cursors.Wait;
            int savedSelectionStart = 0;
            var textBox = (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);

            if (textBox != null)
            {
                savedSelectionStart = textBox.SelectionStart;
                previousText = newText;

                ResetCombosAndResults();

                comboBox.SelectedItem = null;

                if (string.IsNullOrEmpty(newText))
                {
                    UpdateCombobox(allItems);
                }
                else
                {
                    var filteredItems = allItems.Where(item => item.Content.ToString().IndexOf(newText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                    UpdateCombobox(filteredItems);
                    isFiltering = true;
                }
            }
            Mouse.OverrideCursor = null;
            comboBox.IsDropDownOpen = true;
            RestoreTextBoxState(textBox, savedSelectionStart, newText);
        }

        private void RestoreTextBoxState(TextBox textBox, int selectionStart, string text)
        {
            textBox.Text = text;
            textBox.SelectionStart = Math.Min(selectionStart, text.Length);
            textBox.SelectionLength = 0;
        }

        protected void UpdateCombobox(List<ComboBoxItem> items)
        {
            comboBox.Items.Clear();
            foreach (var item in items)
            {
                comboBox.Items.Add(item);
            }
        }

        protected abstract void ResetCombosAndResults();
    }
}