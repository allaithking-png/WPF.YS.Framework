using System.Windows.Controls;

namespace AppFramework.Core.ViewTemplates.GenericViews;

/// <summary>
/// عرض بطاقات عام — يعمل مع أي ViewModel له خاصية Items.
/// </summary>
public partial class GenericCardView : UserControl
{
    public GenericCardView()
    {
        InitializeComponent();
    }
}