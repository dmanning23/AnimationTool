using AnimationTool.Screens;
using FilenameBuddy;
using MenuBuddy;
using Microsoft.Xna.Framework;
using ResolutionBuddy;

namespace AnimationTool
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    //public class Game1 : ControllerGame

#if __IOS__ || ANDROID || WINDOWS_UAP
	public class Game1 : TouchGame
#else
    public class Game1 : MouseGame
#endif
    {
        #region Properties

        AnimationManager AnimationManager { get; set; }

        #endregion //Properties

        #region Methods

        public Game1()
        {
            //FullScreen = true;
            this.IsMouseVisible = true;
            AnimationManager = new AnimationManager(this);

            VirtualResolution = new Point(1440, 850);
            ScreenResolution = new Point(1440, 850);

            //var debug = new DebugInputComponent(this, ResolutionBuddy.Resolution.TransformationMatrix);
            //debug.DrawOrder = 1000;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void Initialize()
        {
            AnimationManager.LoadContent();
            base.Initialize();
        }

        protected override void InitStyles()
        {
            StyleSheet.SmallFontResource = @"Fonts\ArialBlack10";
            StyleSheet.ClickedSoundResource = string.Empty;
            StyleSheet.HighlightedSoundResource = string.Empty;
            StyleSheet.CancelButtonSoundResource = string.Empty;
            base.InitStyles();

            //DefaultStyles.Instance().MainStyle.HasOutline = true;
            //DefaultStyles.Instance().MenuEntryStyle.HasOutline = true;
            //DefaultStyles.Instance().MenuTitleStyle.HasOutline = true;
            //DefaultStyles.Instance().MessageBoxStyle.HasOutline = true;
        }

        /// <summary>
        /// Get the set of screens needed for the main menu
        /// </summary>
        /// <returns>The gameplay screen stack.</returns>
        public override IScreen[] GetMainMenuScreenStack()
        {
            return new IScreen[] { new ModelScreen(AnimationManager), new ToolsScreen(AnimationManager) };
        }

        #endregion //Methods
    }
}
