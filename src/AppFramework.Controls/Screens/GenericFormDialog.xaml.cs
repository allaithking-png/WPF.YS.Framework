using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AppFramework.Controls.Screens;

/// <summary>
/// نافذة حوار عامة لتحرير/إضافة أي كيان.
/// تُبنى الحقول ديناميكيًا من خصائص DTO.
/// </summary>
public partial class GenericFormDialog : Window
{
    private readonly Type _dtoType;
    private readonly object _instance;
    private readonly List<FormField> _fields = new();

    /// <summary>هل ضغط المستخدم حفظ؟</summary>
    public bool Saved { get; private set; }

    /// <summary>الكيان بعد التعديل.</summary>
    public object Result => _instance;

    public GenericFormDialog(object dto, string title)
    {
        InitializeComponent();

        _instance = dto ?? throw new ArgumentNullException(nameof(dto));
        _dtoType = dto.GetType();

        PART_Title.Text = title;

        BuildFields();
    }

    // ==========================================================
    //  Field Builder
    // ==========================================================

    private void BuildFields()
    {
        foreach (var prop in _dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            // تجاهل الخصائص المحددة كـ ReadOnly من الـ DTO (اختياري)
            // نُظهر الكل افتراضيًا

            var field = FormFieldBuilder.CreateField(prop, _instance);
            if (field is null) continue;

            _fields.Add(field);
            PART_FieldsHost.Children.Add(field.Container);
        }
    }

    // ==========================================================
    //  Events
    // ==========================================================

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        // تحقق من الصحة
        var errors = new List<string>();
        foreach (var field in _fields)
        {
            if (!field.Validate(out var error))
                errors.Add($"• {field.Label}: {error}");
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(
                "الرجاء تصحيح الأخطاء التالية:\n\n" + string.Join("\n", errors),
                "تحقق من البيانات",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // طبّق القيم
        foreach (var field in _fields)
            field.Apply();

        Saved = true;
        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        Saved = false;
        DialogResult = false;
        Close();
    }
}