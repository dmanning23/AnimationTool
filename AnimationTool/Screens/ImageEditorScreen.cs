using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AnimationTool.Screens
{
    public class ImageEditorScreen : WidgetScreen
    {
        DragDropButton upperLeft;
        DragDropButton lowerRight;
        MenuBuddy.Image imageControl;

        public ImageEditorScreen() : base("ImageEditorScreen")
        {
        }

        public void SetImage(AnimationLib.Image image)
        {
            Layout.Items.Clear();
            imageControl = null;
            upperLeft = null;
            lowerRight = null;

            AddImageControl(image);
            AddUpperLeft(image);
            AddLowerRight(image);
        }

        private void AddImageControl(AnimationLib.Image image)
        {
            if (null != image.TextureInfo)
            {
                //Add the menubuddy image control
                imageControl = new MenuBuddy.Image(image.TextureInfo.Texture)
                {
                    Position = new Point(128, 128),
                    Horizontal = HorizontalAlignment.Left,
                    Vertical = VerticalAlignment.Top,
                    HasOutline = true,
                    Highlightable = false
                };
                AddItem(imageControl);
            }
        }

        private void AddUpperLeft(AnimationLib.Image image)
        {
            if (null != imageControl)
            {
                //add dragdrop buttons at the upper left and lower right
                upperLeft = new DragDropButton()
                {
                    Position = new Point(imageControl.Position.X + image.SourceRectangle.Left,
                    imageControl.Position.Y + image.SourceRectangle.Top),
                    Horizontal = HorizontalAlignment.Center,
                    Vertical = VerticalAlignment.Center,
                    Size = new Vector2(16, 16)
                };
                upperLeft.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>(@"Joint"))
                {
                    Horizontal = HorizontalAlignment.Center,
                    Vertical = VerticalAlignment.Center,
                    Size = new Vector2(16, 16)
                });

                upperLeft.OnDrag += (obj, e) =>
                {
                    //constrain the button position
                    upperLeft.Position = new Point((int)Math.Max(imageControl.Position.X, Math.Min(e.Current.X, lowerRight.Position.X)),
                            (int)Math.Max(imageControl.Position.Y, Math.Min(e.Current.Y, lowerRight.Position.Y)));

                    //set the image upper left boundary
                    image.SourceRectangle = new Rectangle(upperLeft.Position.X - imageControl.Position.X,
                            upperLeft.Position.Y - imageControl.Position.Y,
                            lowerRight.Position.X - upperLeft.Position.X,
                            lowerRight.Position.Y - upperLeft.Position.Y);
                };
                upperLeft.OnDrop += (obj, e) =>
                {
                    //constrain the button position
                    upperLeft.Position = new Point((int)Math.Max(imageControl.Position.X, Math.Min(e.Drop.X, lowerRight.Position.X)),
                            (int)Math.Max(imageControl.Position.Y, Math.Min(e.Drop.Y, lowerRight.Position.Y)));

                    //set the image upper left boundary
                    image.SourceRectangle = new Rectangle(upperLeft.Position.X - imageControl.Position.X,
                            upperLeft.Position.Y - imageControl.Position.Y,
                            lowerRight.Position.X - upperLeft.Position.X,
                            lowerRight.Position.Y - upperLeft.Position.Y);
                };

                AddItem(upperLeft);
            }
        }

        private void AddLowerRight(AnimationLib.Image image)
        {
            if (null != imageControl)
            {
                //add dragdrop buttons at the upper left and lower right
                lowerRight = new DragDropButton()
                {
                    Position = new Point(imageControl.Position.X + image.SourceRectangle.Right,
                    imageControl.Position.Y + image.SourceRectangle.Bottom),
                    Horizontal = HorizontalAlignment.Center,
                    Vertical = VerticalAlignment.Center,
                    Size = new Vector2(16, 16)
                };
                lowerRight.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>(@"Joint"))
                {
                    Horizontal = HorizontalAlignment.Center,
                    Vertical = VerticalAlignment.Center,
                    Size = new Vector2(16, 16)
                });

                lowerRight.OnDrag += (obj, e) =>
                {
                    //constrain the button position
                    lowerRight.Position = new Point((int)Math.Max(upperLeft.Position.X, Math.Min(e.Current.X, imageControl.Position.X + image.TextureInfo.Texture.Width)),
                            (int)Math.Max(upperLeft.Position.Y, Math.Min(e.Current.Y, imageControl.Position.Y + image.TextureInfo.Texture.Height)));

                    //set the image upper left boundary
                    image.SourceRectangle = new Rectangle(upperLeft.Position.X - imageControl.Position.X,
                            upperLeft.Position.Y - imageControl.Position.Y,
                            lowerRight.Position.X - upperLeft.Position.X,
                            lowerRight.Position.Y - upperLeft.Position.Y);
                };
                lowerRight.OnDrop += (obj, e) =>
                {
                    //constrain the button position
                    lowerRight.Position = new Point((int)Math.Max(upperLeft.Position.X, Math.Min(e.Drop.X, imageControl.Position.X + image.TextureInfo.Texture.Width)),
                            (int)Math.Max(upperLeft.Position.Y, Math.Min(e.Drop.Y, imageControl.Position.Y + image.TextureInfo.Texture.Height)));

                    //set the image upper left boundary
                    image.SourceRectangle = new Rectangle(upperLeft.Position.X - imageControl.Position.X,
                            upperLeft.Position.Y - imageControl.Position.Y,
                            lowerRight.Position.X - upperLeft.Position.X,
                            lowerRight.Position.Y - upperLeft.Position.Y);
                };

                AddItem(lowerRight);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatchBegin();

            if ((null != upperLeft) && (null != lowerRight))
            {
                ScreenManager.DrawHelper.DrawOutline(Color.Red, new Rectangle(upperLeft.Position.X,
                    upperLeft.Position.Y,
                    lowerRight.Position.X - upperLeft.Position.X,
                    lowerRight.Position.Y - upperLeft.Position.Y),
                    1f);
            }

            ScreenManager.SpriteBatchEnd();

            base.Draw(gameTime);
        }
    }
}
