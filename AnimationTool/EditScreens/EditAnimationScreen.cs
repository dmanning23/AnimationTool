using AnimationLib;
using AnimationLib.Commands;
using InputHelper;
using MatrixExtensions;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;
using Image = MenuBuddy.Image;

namespace AnimationTool.EditScreens
{
	public class EditAnimationScreen : BaseEditScreen, IEditScreen
	{
		#region Fields

		int _time;

		private DragDropButton _translationButton;

		#endregion //Fields

		#region Methods

		public EditAnimationScreen(AnimationManager animationManager, Bone bone, int time) : base(animationManager, bone, "EditAnimationScreen")
		{
			_time = time;
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			//Add the drag drop button for the anchor position
			_translationButton = new DragDropButton()
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Position = _bone.AnchorPosition.ToPoint(),
			};

			_translationButton.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>(@"Anchor"))
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				FillRect = true,
				FillColor = Color.Red
			});
			_translationButton.OnDrag += OnTranslationMove;
			_translationButton.OnDrop += OnTranslationDrop;

			AddItem(_translationButton);
		}

		protected override void AddRotationButton()
		{
			_rotationHack = new DragDropButton()
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Position = RotationHandlePosition()
			};
			_rotationHack.AddItem(new Image(Content.Load<Texture2D>(@"SelectedJoint"))
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				FillRect = true,
				FillColor = Color.LimeGreen
			});
			_rotationHack.OnDrag += OnRotationMove;
			_rotationHack.OnDrop += OnRotationDrop;
			AddItem(_rotationHack);
		}

		private Point RotationHandlePosition()
		{
			var rotation = _bone.AnchorJoint.CurrentKeyElement.Rotation;
			var matrix = MatrixExt.Orientation(rotation);
			return _bone.AnchorPosition.ToPoint() + matrix.Multiply(new Vector2(72f, 0f)).ToPoint();
		}

		protected override void OnRotationMove(object obj, DragEventArgs e)
		{
			SetRotation(e.Current);
		}

		protected override void OnRotationDrop(object obj, DropEventArgs e)
		{
			SetRotation(e.Drop);
		}

		private void OnTranslationMove(object obj, DragEventArgs e)
		{
			//move the bone anchor
			AnimationManager.UndoStack.Add(new SetKeyElementTranslation(
				GetCurrentAnimation(_bone),
				_bone,
				_time,
				_bone.ConvertTranslation(e.Current)));

			UpdateControls();
		}

		private void OnTranslationDrop(object obj, DropEventArgs e)
		{
			UpdateControls();
		}

		protected void SetRotation(Vector2 pos)
		{
			var key = _bone.AnchorJoint.CurrentKeyElement;

			//get teh vector to the thing
			var limit = _bone.GetAngleToScreenPosition(pos);

			if (key.Rotation >= MathHelper.Pi)
			{
				limit += MathHelper.TwoPi;
			}
			else if (key.Rotation <= -MathHelper.Pi)
			{
				limit -= MathHelper.TwoPi;
			}

			AnimationManager.UndoStack.Add(new SetKeyElementRotation(GetCurrentAnimation(_bone), _bone, _time, limit));

			UpdateControls();
		}

		private Animation GetCurrentAnimation(Bone bone)
		{
			Animation animation = null;
			if (bone.IsPartOfGarment)
			{
				if (bone is GarmentBone garmentBone)
				{
					animation = garmentBone.GarmentAnimationContainer.CurrentAnimation;
				}
				else
				{
					//run this method on the parent bone until we get to the garmentBone
					animation = GetCurrentAnimation(bone.ParentBone);
				}
			}
			else
			{
				animation = AnimationManager.SelectedAnimationContainer.CurrentAnimation;
			}

			return animation;
		}

		public void UpdateControls()
		{
			//move the anchor button back to the correct spot
			_translationButton.Position = _bone.AnchorPosition.ToPoint();

			//move the button back to the correct spot
			_rotationHack.Position = RotationHandlePosition();
		}

		protected override void DrawRotationBone()
		{
			AnimationManager.Renderer.SpriteBatchBegin(BlendState.AlphaBlend, Matrix.Identity);

			//draw the line from anchor to first joint
			AnimationManager.Renderer.Primitive.Line(_translationButton.Position.ToVector2(),
				_rotationHack.Position.ToVector2(),
				Color.White);

			AnimationManager.Renderer.SpriteBatchEnd();
		}

		#endregion //Methods
	}
}
