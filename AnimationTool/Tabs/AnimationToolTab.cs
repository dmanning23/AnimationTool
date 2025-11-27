using AnimationLib;
using AnimationLibWidgets;
using AnimationTool.Screens;
using Microsoft.Xna.Framework;

namespace AnimationTool.Tabs
{
	public abstract class AnimationToolTab : AnimationLibBaseTab, ITab
	{
		#region Properties

		public AnimationManager AnimationManager { get; private set; }

		public ToolsScreen ToolsScreen { get; set; }
		
		#endregion //Properties

		#region Methods

		public AnimationToolTab(ToolsScreen toolsScreen, AnimationManager animationManager, string tabName) : base(tabName)
		{
			AnimationManager = animationManager;
			ToolsScreen = toolsScreen;
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
			UpdateAnimations(gameTime);
		}

		protected virtual void UpdateAnimations(GameTime gameTime)
		{
			AnimationManager.Update(true, EPlayback.Forwards);
		}

		public abstract void Copy();
		public abstract void Paste();
		public abstract void PasteSpecial();
		public abstract void Mirror();
		public abstract void UnKey();

		#endregion //Methods
	}
}
