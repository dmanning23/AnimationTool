using AnimationLib;
using AnimationLib.Commands;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace AnimationTool.EditScreens
{
	public class EditCircleScreen : BaseEditScreen, IEditScreen
	{
		#region Fields

		private PhysicsCircle _collidable;

		private DragDropButton _centerButton;

		private DragDropButton _edgeButton;

		#endregion //Fields

		#region Methods

		public EditCircleScreen(AnimationManager animationManager, Bone bone, PhysicsCircle collidable) : base(animationManager, bone, "EditCircleScreen")
		{
			if (null == collidable)
			{
				throw new ArgumentNullException("collidable");
			}

			_collidable = collidable;
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			//Add the drag drop button for the anchor position
			_centerButton = new DragDropButton()
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Position = _collidable.Pos.ToPoint()
			};
			_centerButton.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>(@"SelectedJoint"))
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				FillRect = true,
				FillColor = Color.LimeGreen
			});
			_centerButton.OnDrag += OnCenterMove;
			_centerButton.OnDrop += OnDropButton;
			AddItem(_centerButton);

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

		private void OnCenterMove(object obj, DragEventArgs e)
		{
			//move the circle center
			var myAction = new SetCirclePosition(_collidable, _bone.ConvertCoord(e.Current, AnimationManager.Renderer.Camera.Scale));
			AnimationManager.UndoStack.Add(myAction);
		}

		private void OnEdgeMove(object obj, DragEventArgs e)
		{
			//set teh circle radius
			var myAction = new SetCircleRadius(_collidable, _collidable.DistanceToPoint(e.Current));
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

			//draw the bone outline in white
			_bone.DrawOutline(AnimationManager.Renderer, AnimationManager.Renderer.Camera.Scale);

			//draw the selected circle in red
			_collidable.Render(AnimationManager.Renderer, Color.Red);

			AnimationManager.Renderer.SpriteBatchEnd();
		}

		private Point EdgePosition()
		{
			return new Point((int)(_collidable.Pos.X + _collidable.Radius), (int)_collidable.Pos.Y);
		}

		#endregion //Methods
	}
}
