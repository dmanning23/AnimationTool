using AnimationLib;
using AnimationLib.Commands;
using InputHelper;
using MatrixExtensions;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;

namespace AnimationTool.EditScreens
{
	public class EditLimitRagdollScreen : EditRagdollScreen, IEditScreen
	{
		#region Fields

		private DragDropButton _limit1Button;

		private DragDropButton _limit2Button;

		#endregion //Fields

		#region Methods

		public EditLimitRagdollScreen(AnimationManager animationManager, Bone bone) : base(animationManager, bone, "EditLimitRagdollScreen")
		{
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			//Add the drag drop button for the anchor position
			_limit1Button = new DragDropButton()
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Position = Limit1EdgePosition()
			};
			_limit1Button.AddItem(new Label("1", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				TextColor = Color.LimeGreen,
				ShadowColor = Color.Transparent
			});
			_limit1Button.OnDrag += OnLimit1Move;
			_limit1Button.OnDrop += OnLimit1DropButton;
			AddItem(_limit1Button);

			//Add the drag drop button for the anchor position
			_limit2Button = new DragDropButton()
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Position = Limit2EdgePosition()
			};
			_limit2Button.AddItem(new Label("2", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				TextColor = Color.LimeGreen,
				ShadowColor = Color.Transparent
			});
			_limit2Button.OnDrag += OnLimit2Move;
			_limit2Button.OnDrop += OnLimit2DropButton;
			AddItem(_limit2Button);
		}

		private void OnLimit1Move(object obj, DragEventArgs e)
		{
			MoveLimit(e, true);
		}

		private void OnLimit1DropButton(object obj, DropEventArgs e)
		{
			//move the button back to the correct spot
			_limit1Button.Position = Limit1EdgePosition();
		}

		private void OnLimit2Move(object obj, DragEventArgs e)
		{
			MoveLimit(e, false);
		}

		private void OnLimit2DropButton(object obj, DropEventArgs e)
		{
			//move the button back to the correct spot
			_limit2Button.Position = Limit2EdgePosition();
		}

		public override void Draw(GameTime gameTime)
		{
			AnimationManager.Renderer.SpriteBatchBegin(BlendState.AlphaBlend, Matrix.Identity);

			float limit1, limit2;
			_bone.GetLimitRotations(out limit1, out limit2);

			//draw relative to the bone angle
			float startAngle = _bone.GetParentAngle();
			startAngle += limit1;
			startAngle -= _bone.GetBoneAngle();

			//get the sweep
			float sweep = limit2 - limit1;

			AnimationManager.Renderer.Primitive.Pie(
				_bone.AnchorPosition,
				32,
				startAngle, sweep,
				Color.Red);

			AnimationManager.Renderer.SpriteBatchEnd();

			base.Draw(gameTime);
		}

		private void MoveLimit(DragEventArgs e, bool limit1)
		{
			//get teh vector to the thing
			var limit = _bone.GetAngle(e.Current);

			//get teh current joint data
			var myAction = new SetRagdollLimit(_bone.AnchorJoint.Data, limit, limit1);
			AnimationManager.UndoStack.Add(myAction);
		}

		private Point Limit1EdgePosition()
		{
			return EdgePosition(true);
		}

		private Point Limit2EdgePosition()
		{
			return EdgePosition(false);
		}

		private Point EdgePosition(bool isLimit1)
		{
			float limit1, limit2;
			_bone.GetLimitRotations(out limit1, out limit2);

			//draw relative to the bone angle
			limit1 -= _bone.GetBoneAngle();
			limit2 -= _bone.GetBoneAngle();

			var rotation = MatrixExt.Orientation(isLimit1 ? limit1 : limit2);
			var limitVect = rotation.Multiply(new Vector2(32f, 0f));

			return (_bone.AnchorJoint.Position + limitVect).ToPoint();
		}

		#endregion //Methods
	}
}
