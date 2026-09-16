using System;
using System.Threading.Tasks;
using AnimationLib;
using AnimationLib.Commands;
using AnimationLibWidgets;
using AnimationTool.Screens;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using WidgetLib;
using Image = AnimationLib.Image;

namespace AnimationTool.Tabs
{
    public class EditImagesScreen : ListScreen<Image>, ITab
    {
        #region Properties

        Bone Bone { get; set; }
        ImageEditorScreen _editor;
        AnimationManager AnimationManager { get; set; }
        ToolsScreen _toolsScreen;

        #endregion //Properties

        #region Methods

        public EditImagesScreen(ToolsScreen toolsScreen, AnimationManager animationManager, Bone bone, ImageEditorScreen editor) : base(bone.Images, bone.Name)
        {
            Bone = bone;
            _toolsScreen = toolsScreen;
            _editor = editor;
            AnimationManager = animationManager;
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            CloseButton.OnClick += async (obj, e) =>
            {
                var modelScreen = new ModelScreen(AnimationManager);
                var toolsScreen = new ToolsScreen(AnimationManager);
                var skelTab = new SkeletonTab(toolsScreen, AnimationManager);

                ScreenManager.ClearScreens();
                await LoadingScreen.Load(ScreenManager, new IScreen[] { modelScreen, toolsScreen, skelTab });
            };
        }

        public override void NavigateToItemScreen(Image item)
        {
            ScreenManager.AddScreen(new EditImageScreen(_toolsScreen, AnimationManager, item, _editor));
        }

        public override async void AddItem(object obj, ClickEventArgs e)
        {
            //get a name for the bone
            var msgBox = new ImageNameMessageBox(Bone, null);
            msgBox.OnSelect += (obj2, e2) =>
            {
                //add the bone to the skeleton
                var cmd = new AddImage(AnimationManager.SelectedAnimationContainer, Bone.Name, msgBox.ImageName.Text);
                AnimationManager.UndoStack.Add(cmd);

                //add a button control for it
                var image = Bone.GetImage(msgBox.ImageName.Text);
                CreateItemControl(image, true);
            };

            await ScreenManager.AddScreen(msgBox);
        }

        public override void RemoveItem(Image image)
        {
            //TODO: there is no RemoveImage command.

            //base.RemoveItem(image);
        }

        public override void Draw(GameTime gameTime)
        {
            try
            {
                base.Draw(gameTime);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Save()
        {
        }

        public void Copy()
        {
        }

        public void Paste()
        {
        }

        public void PasteSpecial()
        {
        }

        public void Mirror()
        {
        }

        public void UnKey()
        {
        }

        #endregion //Methods
    }
}
