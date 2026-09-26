using System.Windows.Controls;

namespace AppFramework.Core.ViewTemplates.GenericViews;

/// <summary>
/// عرض شبكي عام — يعمل مع أي ViewModel له خاصية Items.
/// </summary>
public partial class GenericGridView : UserControl
{
    public GenericGridView()
    {
        InitializeComponent();
    }
}