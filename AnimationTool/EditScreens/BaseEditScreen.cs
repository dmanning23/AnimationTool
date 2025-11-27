using AnimationLib;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;
using Image = MenuBuddy.Image;

namespace AnimationTool.EditScreens
{
	public abstract class BaseEditScreen : WidgetScreen, IEditScreen
	{
		#region Properties

		protected AnimationManager AnimationManager { get; private set; }

		protected DragDropButton _rotationHack;

		protected Bone _bone;

		#endregion //Properties

		#region Methods

		public BaseEditScreen(AnimationManager animationManager, Bone bone, string name) : base(name)
		{
			if (null == bone)
			{
				throw new ArgumentNullException("bone");
			}

			AnimationManager = animationManager;
			Transition.OnTime = 0f;
			Transition.OffTime = 0f;
			_bone = bone;
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();
			AddRotationButton();
		}

		protected virtual void AddRotationButton()
		{
			if (_bone.Joints.Count > 0)
			{
				//add the rotation hack
				_rotationHack = new DragDropButton()
				{
					Size = new Vector2(16, 16),
					Horizontal = HorizontalAlignment.Center,
					Vertical = VerticalAlignment.Center,
					Position = _bone.Joints[0].Position.ToPoint()
				};
				_rotationHack.AddItem(new Image(Content.Load<Texture2D>(@"Joint"))
				{
					Size = new Vector2(16, 16),
					Horizontal = HorizontalAlignment.Center,
					Vertical = VerticalAlignment.Center,
					FillRect = true,
					FillColor = Color.White
				});
				_rotationHack.OnDrag += OnRotationMove;
				_rotationHack.OnDrop += OnRotationDrop;
				AddItem(_rotationHack);
			}
		}

		protected virtual void OnRotationMove(object obj, DragEventArgs e)
		{
			SetRotationHack(e.Current);
		}

		protected virtual void OnRotationDrop(object obj, DropEventArgs e)
		{
			SetRotationHack(e.Drop);

			//move the button back to the correct spot
			_rotationHack.Position = _bone.Joints[0].Position.ToPoint();
			_bone.SolveLimits(_bone.GetParentAngle());
		}

		protected virtual void SetRotationHack(Vector2 pos)
		{
			//get teh vector to the thing
			var limit = _bone.GetAngle(pos);

			//get teh current joint data
			AnimationManager.HackRotation = limit;
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);
			DrawRotationBone();
		}

		protected virtual void DrawRotationBone()
		{
			//draw the rotation bone
			if (_bone.Joints.Count > 0)
			{
				AnimationManager.Renderer.SpriteBatchBegin(BlendState.AlphaBlend, Matrix.Identity);

				//draw the line from anchor to first joint
				AnimationManager.Renderer.Primitive.Line(_bone.AnchorPosition,
					_bone.Joints[0].Position,
					Color.White);

				AnimationManager.Renderer.SpriteBatchEnd();
			}
		}

		#endregion //Methods
	}
}
