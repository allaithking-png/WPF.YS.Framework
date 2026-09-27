namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// نوع التفاعل مع أيقونة في سطح المكتب.
/// </summary>
public enum LauncherAction
{
    Open,
    Edit,
    Delete,
    Pin,
    Unpin,
    Favorite,
    Unfavorite,
    Hide,
    MoveUp,
    MoveDown,
    CreateFolder,
    AddToFolder,
    RemoveFromFolder
}

/// <summary>
/// حدث تفاعل مع أيقونة.
/// </summary>
public sealed class LauncherInteractionEventArgs : System.EventArgs
{
    public NavItem Item { get; }
    public LauncherAction Action { get; }

    public LauncherInteractionEventArgs(NavItem item, LauncherAction action)
    {
        Item = item;
        Action = action;
    }
}