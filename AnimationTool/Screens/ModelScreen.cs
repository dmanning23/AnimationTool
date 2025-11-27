using DrawListBuddy;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AnimationTool.Screens
{
	/// <summary>
	/// This is the screen that displays the model
	/// </summary>
	public class ModelScreen : Screen
	{
		#region Properties
		
		AnimationManager Animations { get; set; }

		DrawList Drawlist { get; set; }

		#endregion //Properties

		#region Methods

		public ModelScreen(AnimationManager animations) : base("ModelScreen")
		{
			Animations = animations;
			Drawlist = new DrawList();
			CoveredByOtherScreens = false;
			CoverOtherScreens = false;
		}

		public override void Draw(GameTime gameTime)
		{
			//set up the drawlist
			Drawlist.Set(0.0f, Color.White, 1.0f);
			Drawlist.Flush();

			//draw the character
			Animations.SelectedAnimationContainer.Render(Drawlist);
			Animations.Renderer.SpriteBatchBegin(BlendState.NonPremultiplied, Matrix.Identity);
			Drawlist.Render(Animations.Renderer);
			Animations.Renderer.SpriteBatchEnd();

			base.Draw(gameTime);
		}

		#endregion //Methods
	}
}
