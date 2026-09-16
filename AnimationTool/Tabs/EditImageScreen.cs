using AnimationTool.Tabs;
using MenuBuddy;
using System;
using System.Threading.Tasks;
using FilenameBuddy;
using System.Text;

namespace AnimationTool.Screens
{
    public class EditImageScreen : AnimationToolTab
    {
        AnimationLib.Image Image { get; set; }

        ImageEditorScreen _editor;

        public EditImageScreen(ToolsScreen toolsScreen, AnimationManager animationManager, AnimationLib.Image image, ImageEditorScreen editor) : base(toolsScreen, animationManager, "EditImageScreen")
        {
            Image = image;
            _editor = editor;
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            AddTitle(Image.Name, true, ToolStack);

            //Add a shim
            AddShim(ToolStack);

            //Add the Background image controls
            AddResources();

            AddItem(ToolStack);

            //set the image in the editor window
            _editor.SetImage(Image);
        }

        protected override void OnTitleTextEdited(string titleText)
        {
            Image.Name = titleText;
        }

        protected void AddResources()
        {
            var folder = !string.IsNullOrEmpty(Image.ImageFile.File) ? Image.ImageFile.GetRelPath() : AnimationManager.ModelFile.GetRelPath();
            try
            {
                var imageDropdown = AddContentFileDropdown("Image File", folder, ".png", Image.ImageFile, ToolStack);
                imageDropdown.OnSelectedItemChange += (obj, e) =>
                {
                    Image.ImageFile = imageDropdown.SelectedItem;
                    Image.LoadImage(AnimationManager.Renderer);
                };

                var normalDropdown = AddContentFileDropdown("Normal File", folder, ".png", Image.NormalMapFile, ToolStack);
                normalDropdown.OnSelectedItemChange += (obj, e) =>
                {
                    Image.NormalMapFile = normalDropdown.SelectedItem;
                    Image.LoadImage(AnimationManager.Renderer);
                };

                var maskDropdown = AddContentFileDropdown("Mask File", folder, ".png", Image.ColorMaskFile, ToolStack);
                maskDropdown.OnSelectedItemChange += (obj, e) =>
                {
                    Image.ColorMaskFile = maskDropdown.SelectedItem;
                    Image.LoadImage(AnimationManager.Renderer);
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override void Copy()
        {
            throw new NotImplementedException();
        }

        public override void Mirror()
        {
            throw new NotImplementedException();
        }

        public override void Paste()
        {
            throw new NotImplementedException();
        }

        public override void PasteSpecial()
        {
            throw new NotImplementedException();
        }

        public override void UnKey()
        {
            throw new NotImplementedException();
        }
    }
}
