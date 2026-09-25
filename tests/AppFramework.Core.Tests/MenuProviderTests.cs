using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FluentAssertions;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Controls.Menus;
using Xunit;

namespace AppFramework.Core.Tests;

public class MenuProviderTests
{
    // =============== Registry Tests ===============

    [Fact]
    public void MenuProviderRegistry_Register_AddsProvider()
    {
       StaTestHelper.RunSta(() =>
    { 
         var registry = new MenuProviderRegistry();
        registry.Register(new MenuBarProvider());

        registry.Supports(MenuHost.MenuBar).Should().BeTrue();
        registry.Get(MenuHost.MenuBar).Should().BeOfType<MenuBarProvider>();  
        });
    }

    [Fact]
    public void MenuProviderRegistry_Get_UnknownHost_ReturnsNull()
    {
       StaTestHelper.RunSta(() =>
    {
       
        var registry = new MenuProviderRegistry();
        registry.Get(MenuHost.Ribbon).Should().BeNull();
         });
    }

    [Fact]
    public void MenuProviderRegistry_CreateDefault_HasAllProviders()
    {
      StaTestHelper.RunSta(() =>
    {
        
    
        var registry = MenuProviderRegistry.CreateDefault();

        registry.Supports(MenuHost.MenuBar).Should().BeTrue();
        registry.Supports(MenuHost.Toolbar).Should().BeTrue();
        registry.Supports(MenuHost.Ribbon).Should().BeTrue();
         });
    }

    // =============== MenuBarProvider Tests ===============

    [Fact]
    public void MenuBarProvider_Build_ReturnsMenu()
    {
       StaTestHelper.RunSta(() =>
    {
        
     var provider = new MenuBarProvider();
        var def = new MenuDefinition
        {
            Host = MenuHost.MenuBar,
            Items =
            {
                new MenuItemDescriptor { Title = "File", Order = 1 }
            }
        };

        var result = provider.Build(def);

        result.Should().BeOfType<Menu>();
        var menu = (Menu)result;
        menu.Items.Count.Should().Be(1);
         });
    }

    [Fact]
    public void MenuBarProvider_GroupsItemsByGroupName()
    {
      StaTestHelper.RunSta(() =>
    {
        

        var provider = new MenuBarProvider();
        var def = new MenuDefinition
        {
            Items =
            {
                new MenuItemDescriptor { Title = "New", GroupName = "File", Order = 1 },
                new MenuItemDescriptor { Title = "Open", GroupName = "File", Order = 2 },
                new MenuItemDescriptor { Title = "Exit", GroupName = "App", Order = 1 }
            }
        };

        var menu = (Menu)provider.Build(def);

        menu.Items.Count.Should().Be(2);   // File + App
    });
    }

    [Fact]
    public void MenuBarProvider_HidesInvisibleItems()
    {
       StaTestHelper.RunSta(() =>
    {
        
    
        var provider = new MenuBarProvider();
        var def = new MenuDefinition
        {
            Items =
            {
                new MenuItemDescriptor { Title = "Visible", IsVisible = true, GroupName = "A" },
                new MenuItemDescriptor { Title = "Hidden", IsVisible = false, GroupName = "A" }
            }
        };

        var menu = (Menu)provider.Build(def);

        // مجموعتان بـ "A" -> يصبحان قائمة واحدة، فقط Visible
        var topItem = menu.Items[0] as MenuItem;
        topItem.Should().NotBeNull();
        topItem!.Items.Count.Should().Be(1);
         });
    }

    // =============== ToolbarProvider Tests ===============

    [Fact]
    public void ToolbarProvider_Build_ReturnsToolBarTray()
    {
        StaTestHelper.RunSta(() =>
    {
        
  
        var provider = new ToolbarProvider();
        var def = new MenuDefinition
        {
            Items =
            {
                new MenuItemDescriptor { Title = "Save", GroupName = "File", Order = 1 }
            }
        };

        var result = provider.Build(def);

        result.Should().BeOfType<ToolBarTray>();
         });
    }

    [Fact]
    public void ToolbarProvider_InsertsSeparatorBetweenGroups()
    {
     StaTestHelper.RunSta(() =>
    {
        
   
       var provider = new ToolbarProvider();
        var def = new MenuDefinition
        {
            Items =
            {
                new MenuItemDescriptor { Title = "Save", GroupName = "File", Order = 1 },
                new MenuItemDescriptor { Title = "Print", GroupName = "Print", Order = 1 }
            }
        };

        var tray = (ToolBarTray)provider.Build(def);
        var toolbar = tray.ToolBars[0];

        // عنصران + فاصل واحد = 3
        toolbar.Items.Count.Should().Be(3);
         });
    }

    // =============== RibbonProvider Tests ===============

    [Fact]
    public void RibbonProvider_Build_ReturnsFallback()
    {
       StaTestHelper.RunSta(() =>
    {
        
      var provider = new RibbonProvider();
        var def = new MenuDefinition();

        // حاليًا يعود بـ ToolBarTray (fallback)
        var result = provider.Build(def);
        result.Should().NotBeNull();
         });
    }

    [Fact]
    public void RibbonProvider_Host_IsRibbon()
    {
       StaTestHelper.RunSta(() =>
    {
        
   
      var provider = new RibbonProvider();
        provider.Host.Should().Be(MenuHost.Ribbon);
     });
     }
}