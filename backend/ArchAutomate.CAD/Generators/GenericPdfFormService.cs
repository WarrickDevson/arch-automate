using Syncfusion.Pdf.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArchAutomate.CAD.Generators;

public class GenericPdfFormService
{
    private readonly string _dateFormat;

    public GenericPdfFormService(string dateFormat = "yyyy-MM-dd")
    {
        _dateFormat = dateFormat;
    }

    public byte[] FillForm(string templateFilePath, Dictionary<string, object> fieldData)
    {
        byte[] templateBytes = File.ReadAllBytes(templateFilePath);
        return FillForm(templateBytes, fieldData);
    }

    public byte[] FillForm(byte[] templateBytes, Dictionary<string, object> fieldData)
    {
        using (MemoryStream inputStream = new MemoryStream(templateBytes))
        using (PdfLoadedDocument document = new PdfLoadedDocument(inputStream))
        {
            if (document.Form != null && document.Form.Fields.Count > 0)
            {
                ProcessAcroForm(document.Form, fieldData);
                document.Form.Flatten = true;

                using (MemoryStream outputStream = new MemoryStream())
                {
                    document.Save(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
        throw new Exception("PDF does not contain valid interactive fields.");
    }

    private void ProcessAcroForm(PdfLoadedForm form, Dictionary<string, object> fieldData)
    {
        // To optimize, create a dictionary of existing fields by exactly matching names
        var pdfFields = form.Fields.Cast<PdfLoadedField>().ToDictionary(f => f.Name, f => f);

        foreach (var kvp in fieldData)
        {
            string fieldName = kvp.Key;
            object value = kvp.Value;

            if (value == null || !pdfFields.ContainsKey(fieldName))
                continue;

            PdfLoadedField field = pdfFields[fieldName];

            switch (field)
            {
                case PdfLoadedTextBoxField textBox:
                    textBox.Text = FormatValueForText(value);
                    break;
                case PdfLoadedCheckBoxField checkBox:
                    if (value is bool boolVal)
                    {
                        checkBox.Checked = boolVal;
                    }
                    else
                    {
                        string strVal = value.ToString()?.ToLower() ?? "";
                        if (strVal == "true" || strVal == "on" || strVal == "1" || strVal == "yes")
                        {
                            checkBox.Checked = true;
                        }
                    }
                    break;
                case PdfLoadedComboBoxField comboBox:
                    comboBox.SelectedValue = value.ToString();
                    break;
                case PdfLoadedListBoxField listBox:
                    string valStr = value.ToString() ?? "";
                    if (listBox.Values != null && listBox.Values.Cast<PdfLoadedListItem>().Any(i => i.Value == valStr))
                    {
                        listBox.SelectedIndex = new int[] { listBox.Values.Cast<PdfLoadedListItem>().ToList().FindIndex(i => i.Value == valStr) };
                    }
                    break;
                case PdfLoadedRadioButtonListField radio:
                    radio.SelectedValue = value.ToString();
                    break;
            }
        }
    }

    private string FormatValueForText(object value)
    {
        if (value is DateTime dateTime) return dateTime.ToString(_dateFormat);
        return value.ToString() ?? "";
    }
}
