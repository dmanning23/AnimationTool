using AnimationLib;
using AnimationTool.EditScreens;
using AnimationTool.Tabs;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ResolutionBuddy;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Image = MenuBuddy.Image;

namespace AnimationTool.Screens
{
    /// <summary>
    /// This is a widget screen with items like undo, redo, save, the tab buttons, etc.
    /// </summary>
    public class ToolsScreen : WidgetScreen
    {
        #region Properties

        AnimationManager AnimationManager { get; set; }

        #endregion //Properties

        #region Methods

        public ToolsScreen(AnimationManager animationManager) : base("ToolsScreen")
        {
            AnimationManager = animationManager;
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            //Add a toolbar at the top of the screen
            var tabs = new StackLayout(StackAlignment.Left)
            {
                Position = new Point(910, 0),
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
            };

            var modelTabButton = CreateTabButton("Model");
            modelTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new SkeletonTab(this, AnimationManager));
            };
            tabs.AddItem(modelTabButton);

            var animationTabButton = CreateTabButton("Animations");
            animationTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new AnimationTab(this, AnimationManager));
            };
            tabs.AddItem(animationTabButton);

            var physicsTabButton = CreateTabButton("Physics");
            physicsTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new PhysicsTab(this, AnimationManager));
            };
            tabs.AddItem(physicsTabButton);

            var ragdollTabButton = CreateTabButton("Ragdoll");
            ragdollTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new RagdollTab(this, AnimationManager));
            };
            tabs.AddItem(ragdollTabButton);

            var testTabButton = CreateTabButton("Test");
            testTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new TestTab(this, AnimationManager));
            };
            tabs.AddItem(testTabButton);

            var garmentTabButton = CreateTabButton("Garments");
            garmentTabButton.OnClick += (obj, e) =>
            {
                LoadTab(new GarmentTab(this, AnimationManager));
            };
            tabs.AddItem(garmentTabButton);

            var fileMenu = new Hamburger(Content.Load<Texture2D>("menu"), true, ScreenManager);
            fileMenu.AddItem(new Label("File", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Bottom,
            });
            fileMenu.AddItem(Content.Load<Texture2D>("menu"), "New", FileNew);
            fileMenu.AddItem(Content.Load<Texture2D>("menu"), "Open Model", FileOpenModel);
            fileMenu.AddItem(Content.Load<Texture2D>("menu"), "Open Animation", FileOpenAnimation);
            fileMenu.AddItem(Content.Load<Texture2D>(@"icons\save"), "Save", FileSave);
            fileMenu.AddItem(Content.Load<Texture2D>(@"icons\save"), "Save As", FileSaveAs);
            fileMenu.AddItem(Content.Load<Texture2D>(@"icons\save"), "Save As JSON", FileSaveAsJson);
            tabs.AddItem(fileMenu);

            var garmentMenu = new Hamburger(Content.Load<Texture2D>("menu"), true, ScreenManager);
            garmentMenu.AddItem(new Label("Garment", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Bottom,
            });
            garmentMenu.AddItem(Content.Load<Texture2D>("menu"), "New", GarmentNew);
            garmentMenu.AddItem(Content.Load<Texture2D>("menu"), "Open", GarmentOpen);
            tabs.AddItem(garmentMenu);

            AddItem(tabs);
            AddAsMenuItems();
        }

        private void AddAsMenuItems()
        {
            var stack = new StackLayout()
            {
                Alignment = StackAlignment.Top,
                Position = new Point(Resolution.ScreenArea.Left, 0),
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
            };

            var hamburgerItems = new List<ContextMenuItem>();
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\undo"), "Undo", Undo));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\redo"), "Redo", Redo));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\copy"), "Copy", Copy));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\paste"), "Paste", Paste));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\pasteSpecial"), "PasteSpecial", PasteSpecial));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\leftright"), "Mirror", Mirror));
            hamburgerItems.Add(new ContextMenuItem(Content.Load<Texture2D>(@"icons\unkey"), "UnKey", UnKey));

            //add each menu item below this
            foreach (var hamburgerItem in hamburgerItems)
            {
                CreateButton(hamburgerItem, stack);
            }

            AddItem(stack);
        }

        private void CreateButton(ContextMenuItem hamburgerItem, StackLayout stack)
        {
            var button = new StackLayoutButton()
            {
                Vertical = VerticalAlignment.Center,
                Horizontal = HorizontalAlignment.Left,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopLeft),
                Alignment = StackAlignment.Left,
            };
            button.AddItem(new Image(hamburgerItem.Icon)
            {
                Size = new Vector2(32f, 32f),
                Vertical = VerticalAlignment.Center,
                Horizontal = HorizontalAlignment.Left,
                FillRect = true,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopLeft)
            });
            button.AddItem(new Shim()
            {
                Size = new Vector2(16f, 16f)
            });
            button.AddItem(new Label(hamburgerItem.IconText, Content, FontSize.Small)
            {
                Vertical = VerticalAlignment.Top,
                Horizontal = HorizontalAlignment.Left,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopLeft)
            });
            button.OnClick += (obj, e) => hamburgerItem.ClickEvent(obj, e);

            stack.AddItem(button);
            stack.AddItem(new Shim()
            {
                Size = new Vector2(8f, 8f)
            });
        }

        public void ClearEditScreens()
        {
            //remove all the current edit screens
            var editScreens = ScreenManager.FindScreens<IEditScreen>();
            foreach (var editScreen in editScreens)
            {
                editScreen.ExitScreen();
            }
        }

        public void ClearTabsAndScreens()
        {
            //remove all the current tabs
            var tabScreens = ScreenManager.FindScreens<ITab>();
            foreach (var tabScreen in tabScreens)
            {
                tabScreen.ExitScreen();
            }

            ClearEditScreens();
        }

        #endregion //Methods

        #region Event Handlers

        private RelativeLayoutButton CreateTabButton(string tabName)
        {
            var tabButton = new RelativeLayoutButton()
            {
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                Size = new Vector2(75f, 32f),
                HasOutline = true,
            };
            tabButton.AddItem(new Label(tabName, Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Center,
            });

            return tabButton;
        }

        private void LoadTab(ITab tab)
        {
            ClearTabsAndScreens();

            ScreenManager.AddScreen(tab);
        }

        private void OnAnimationContainerChanged(object obj, SelectionChangeEventArgs<AnimationContainer> e)
        {
            AnimationManager.SelectedAnimationContainer = e.SelectedItem;
            ClearTabsAndScreens();
        }

        #endregion //Event Handlers

        #region Hamburger Event Handlers

        private void FileNew(object obj, ClickEventArgs e)
        {
            AnimationManager.NewModel();
            ClearTabsAndScreens();
        }

        private void FileOpenModel(object obj, ClickEventArgs e)
        {
            AnimationManager.OpenModel();
            ClearTabsAndScreens();
        }

        private void FileOpenAnimation(object obj, ClickEventArgs e)
        {
            AnimationManager.OpenAnimation();
            ClearTabsAndScreens();
        }

        private void FileSave(object obj, ClickEventArgs e)
        {
            AnimationManager.Save();
        }

        private void FileSaveAs(object obj, ClickEventArgs e)
        {
            AnimationManager.SaveAs();
        }

        private void FileSaveAsJson(object obj, ClickEventArgs e)
        {
            AnimationManager.SaveAsJson();
        }

        private void GarmentNew(object obj, ClickEventArgs e)
        {
            AnimationManager.NewGarment();
            ClearTabsAndScreens();
        }

        private void GarmentOpen(object obj, ClickEventArgs e)
        {
            AnimationManager.OpenGarment();
            ClearTabsAndScreens();
        }

        private void Undo(object obj, ClickEventArgs e)
        {
            AnimationManager.UndoStack.Undo();
        }

        private void Redo(object obj, ClickEventArgs e)
        {
            AnimationManager.UndoStack.Redo();
        }

        private void Copy(object obj, ClickEventArgs e)
        {
            var screens = ScreenManager.FindScreens<ITab>();
            foreach (var screen in screens)
            {
                var tab = screen as ITab;
                tab.Copy();
            }
        }

        private void Paste(object obj, ClickEventArgs e)
        {
            var screen = ScreenManager.FindScreens<ITab>().First();
            var tab = screen as ITab;
            tab?.Paste();
        }

        private void PasteSpecial(object obj, ClickEventArgs e)
        {
            var screen = ScreenManager.FindScreens<ITab>().First();
            var tab = screen as ITab;
            tab?.PasteSpecial();
        }

        private void Mirror(object obj, ClickEventArgs e)
        {
            var screen = ScreenManager.FindScreens<ITab>().First();
            var tab = screen as ITab;
            tab?.Mirror();
        }

        private void UnKey(object obj, ClickEventArgs e)
        {
            var screen = ScreenManager.FindScreens<ITab>().First();
            var tab = screen as ITab;
            tab?.UnKey();
        }

        #endregion //Hamburger Event Handlers
    }
}
