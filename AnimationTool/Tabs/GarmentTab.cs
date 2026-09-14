using AnimationLib;
using AnimationLibWidgets;
using AnimationTool.Screens;
using GameTimer;
using MenuBuddy;
using Microsoft.Xna.Framework;
using ResolutionBuddy;
using System.Threading.Tasks;

namespace AnimationTool.Tabs
{
    public class GarmentTab : AnimationToolTab
    {
        #region Properties

        GameClock Clock { get; set; }
        Vector2 DudePosition { get; set; }

        private IStackLayout ScrollingStack { get; set; }

        private ScrollLayout Scroller { get; set; }

        #endregion //Properties

        #region Methods

        public GarmentTab(ToolsScreen toolsScreen, AnimationManager animationManager) : base(toolsScreen, animationManager, "Garments")
        {
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            //Create the scroll layout...
            Scroller = new ScrollLayout()
            {
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                Size = new Vector2(360f, Resolution.ScreenArea.Height - ToolStack.Rect.Bottom),
                Position = new Point(ToolStack.Rect.Left, ToolStack.Rect.Bottom)
            };

            //Create the scrolling stack and add to the scroller
            ScrollingStack = new StackLayout()
            {
                Alignment = StackAlignment.Top,
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top
            };

            //add a button for each item in the list
            foreach (var garment in AnimationManager.Garments)
            {
                CreateItemControl(garment);
            }

            Scroller.AddItem(ScrollingStack);

            //add the scroller
            AddItem(Scroller);

            AddItem(ToolStack);

            DudePosition = new Vector2(Resolution.TitleSafeArea.Center.X * .7f, Resolution.TitleSafeArea.Center.Y);

            Clock = new GameClock();
            Clock.Start();

            if (null != AnimationManager.SelectedAnimationContainer.CurrentAnimation)
            {
                var currentAnimation = AnimationManager.SelectedAnimationContainer.CurrentAnimation.Name;
                AnimationManager.SelectedAnimationContainer.SetAnimation(currentAnimation, EPlayback.Loop);
            }
        }

        protected virtual void CreateItemControl(Garment garment)
        {
            var button = new GarmentCheckbox(garment, Content)
            {
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
            };

            ScrollingStack.AddItem(button);

            Scroller.UpdateMinMaxScroll();
            Scroller.UpdateScrollBars();
        }

        protected override void UpdateAnimations(GameTime gameTime)
        {
            Clock.Update(gameTime);
            AnimationManager.Update(Clock, DudePosition);
        }

        public override void Copy()
        {
        }

        public override void Mirror()
        {
        }

        public override void Paste()
        {
        }

        public override void PasteSpecial()
        {
        }

        public override void UnKey()
        {
        }

        #endregion //Methods
    }
}
