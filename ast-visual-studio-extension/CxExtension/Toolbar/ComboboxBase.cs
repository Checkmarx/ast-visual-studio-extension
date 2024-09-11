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
        protected bool isFiltered = false;
        protected List<ComboBoxItem> allItems;
        public ComboboxBase(CxToolbar cxToolbar, ComboBox comboBox)
        {
            this.cxToolbar = cxToolbar;
            this.comboBox = comboBox;
            allItems = new List<ComboBoxItem>();
        }
        protected void ResetFilteringState(ComboBoxItem selectedItem)
        {
            previousText = selectedItem.Content.ToString();
            if (isFiltered)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                isFiltered = false;
                UpdateCombobox(allItems);
                comboBox.SelectedItem = selectedItem;
                Mouse.OverrideCursor = null;
            }
        }
        public void OnComboBoxTextChanged(object sender, EventArgs e)
        {
            
            if (comboBox == null || IsTextUnchanged()) return;

            Mouse.OverrideCursor = Cursors.Wait;

            TextBox textBox = GetTextBoxFromComboBox();

            if (textBox != null)
            {
                string newText = textBox.Text;
                int savedSelectionStart = textBox.SelectionStart;

                ResetOthersComboBoxesAndResults();

                UpdateComboBoxWithFilteredItems(newText);

                comboBox.IsDropDownOpen = true;
                RestoreTextBoxState(textBox, savedSelectionStart, newText);
            }
            Mouse.OverrideCursor = null;
        }
        private TextBox GetTextBoxFromComboBox()
        {
            return (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
        }
        private bool IsTextUnchanged()
        {
            
            if (comboBox.Text == previousText) return true;

            previousText = comboBox.Text;
            return false;
        }
        private void UpdateComboBoxWithFilteredItems(string newText)
        {
            comboBox.SelectedItem = null;

            if (string.IsNullOrEmpty(newText))
            {
                UpdateCombobox(allItems);
            }
            else
            {
                var filteredItems = allItems.Where(item => item.Content.ToString()
                .IndexOf(newText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
                UpdateCombobox(filteredItems);
                isFiltered = true;
            }
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

        protected abstract void ResetOthersComboBoxesAndResults();
    }
}