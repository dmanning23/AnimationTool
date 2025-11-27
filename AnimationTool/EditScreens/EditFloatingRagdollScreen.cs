using AnimationLib;
using AnimationLib.Commands;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;

namespace AnimationTool.EditScreens
{
	public class EditFloatingRagdollScreen : EditRagdollScreen, IEditScreen
	{
		#region Fields

		private DragDropButton _edgeButton;

		#endregion //Fields

		#region Methods

		public EditFloatingRagdollScreen(AnimationManager animationManager, Bone bone) : base(animationManager, bone, "EditFloatingRagdollScreen")
		{
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			//Add the drag drop button for the anchor position
			_edgeButton = new DragDropButton()
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Position = EdgePosition()
			};
			_edgeButton.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>(@"SelectedJoint"))
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				FillRect = true,
				FillColor = Color.Yellow
			});
			_edgeButton.OnDrag += OnEdgeMove;
			_edgeButton.OnDrop += OnDropButton;
			AddItem(_edgeButton);
		}

		private void OnEdgeMove(object obj, DragEventArgs e)
		{
			//get teh vector to the thing
			var vect = _bone.ConvertTranslation(e.Current);

			//get the length of that vector, which is going to be the float radius
			float length = vect.Length();

			//get teh current joint data
			var myAction = new SetRagdollRadius(_bone.AnchorJoint.Data, length);
			AnimationManager.UndoStack.Add(myAction);
		}

		private void OnDropButton(object obj, DropEventArgs e)
		{
			//move the button back to the correct spot
			_edgeButton.Position = EdgePosition();
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			AnimationManager.Renderer.SpriteBatchBegin(BlendState.AlphaBlend, Matrix.Identity);

			//draw the float circle
			AnimationManager.Renderer.Primitive.Circle(
				_bone.AnchorPosition,
				(int)(_bone.AnchorJoint.Data.FloatRadius * AnimationManager.Renderer.Camera.Scale),
				Color.Red);

			//draw a red dot on the anchor location
			AnimationManager.Renderer.Primitive.Point(_bone.AnchorJoint.Position, Color.Red);

			AnimationManager.Renderer.SpriteBatchEnd();
		}

		private Point EdgePosition()
		{
			return new Point((int)(_bone.AnchorJoint.Position.X + (_bone.AnchorJoint.Data.FloatRadius * AnimationManager.Renderer.Camera.Scale)),
				(int)_bone.AnchorJoint.Position.Y);
		}

		#endregion //Methods
	}
}
