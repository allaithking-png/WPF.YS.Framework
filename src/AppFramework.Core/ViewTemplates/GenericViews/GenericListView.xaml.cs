using System.Windows.Controls;

namespace AppFramework.Core.ViewTemplates.GenericViews;

/// <summary>
/// عرض قائمة عام — يعمل مع أي ViewModel له خاصية Items.
/// </summary>
public partial class GenericListView : UserControl
{
    public GenericListView()
    {
        InitializeComponent();
    }
}