using AnimationLib;
using AnimationLib.Commands;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimationTool.EditScreens
{
	public class EditSkeletonScreen : BaseEditScreen
	{
		#region Fields

		private Joint _joint;

		private DragDropButton _anchorButton;

		Vector2? _dragStart;

		private Dictionary<Joint, IWidget> _jointWidgets;

		#endregion //Fields

		#region Methods

		public EditSkeletonScreen(AnimationManager animationManager, Bone bone, Joint joint) : base(animationManager, bone, "EditSkeletonScreen")
		{
			_joint = joint;
			_jointWidgets = new Dictionary<Joint, IWidget>();
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			//Add the drag drop button for the anchor position
			_anchorButton = new DragDropButton()
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Position = _bone.AnchorPosition.ToPoint(),
			};

			_anchorButton.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>(@"Anchor"))
			{
				Size = new Vector2(16, 16),
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				FillRect = true,
				FillColor = Color.Red
			});
			_anchorButton.OnDrag += OnAnchorMove;
			_anchorButton.OnDrop += OnAnchorDrop;

			AddItem(_anchorButton);

			//if this is the first joint, remove the rotation hack from it
			if (0 == _bone.GetJointIndex(_joint))
			{
				RemoveItem(_rotationHack);
			}

			UpdateJointButtons();
		}

		private void OnAnchorMove(object obj, DragEventArgs e)
		{
			if (!_dragStart.HasValue)
			{
				_dragStart = _bone.Position;
			}

			var image = _bone.GetCurrentImage();

			if (null != image)
			{
				var action = new SetAnchorLocation(_bone,
					image,
					_bone.ConvertCoord(e.Current, AnimationManager.Renderer.Camera.Scale, _dragStart));

				//move the bone anchor
				AnimationManager.UndoStack.Add(action);

				UpdateJointButtons();
			}
		}

		private void OnAnchorDrop(object obj, DropEventArgs e)
		{
			//move the anchor button back to the correct spot
			_anchorButton.Position = _bone.AnchorPosition.ToPoint();

			_dragStart = null;
		}

		private void OnJointMove(object obj, DragEventArgs e)
		{
			int iIndex = _bone.GetJointIndex(_joint);

			//get teh current joint data
			var image = _bone.GetCurrentImage();
			if (null != image)
			{
				JointData CurrentData = image.JointCoords[iIndex];
				JointData myNewData = new JointData();

				myNewData.Copy(CurrentData);
				myNewData.Location = _bone.ConvertCoord(e.Current, AnimationManager.Renderer.Camera.Scale);

				AnimationManager.UndoStack.Add(new SetJointCoords(_bone, image, iIndex, myNewData));
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			//draw the outline in white
			if (null != _bone)
			{
				AnimationManager.Renderer.SpriteBatchBegin(BlendState.AlphaBlend, Matrix.Identity);
				_bone.DrawOutline(AnimationManager.Renderer, AnimationManager.Renderer.Camera.Scale);
				AnimationManager.Renderer.SpriteBatchEnd();
			}
		}

		public void UpdateJointButtons()
		{
			foreach (var jointWidget in _jointWidgets)
			{
				//remove the widget from the screen
				RemoveItem(jointWidget.Value);
			}

			_jointWidgets.Clear();

			//add the widgets for all the joints
			foreach (var joint in _bone.Joints)
			{
				//is this the selected joint
				if ((null != _joint) && (joint.Name == _joint.Name))
				{
					//add the drag drop button for the joint location
					var jointButton = new DragDropButton()
					{
						Size = new Vector2(16, 16),
						Horizontal = HorizontalAlignment.Center,
						Vertical = VerticalAlignment.Center,
						Position = joint.Position.ToPoint(),
					};

					jointButton.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>(@"SelectedJoint"))
					{
						Size = new Vector2(16, 16),
						Horizontal = HorizontalAlignment.Center,
						Vertical = VerticalAlignment.Center,
						FillRect = true,
						FillColor = Color.LimeGreen
					});
					jointButton.OnDrag += OnJointMove;

					AddItem(jointButton);
					_jointWidgets.Add(joint, jointButton);
				}
				else
				{
					var jointImage = new MenuBuddy.Image(Content.Load<Texture2D>(@"Joint"))
					{
						Size = new Vector2(16, 16),
						Horizontal = HorizontalAlignment.Center,
						Vertical = VerticalAlignment.Center,
						FillRect = true,
						FillColor = Color.Yellow,
						Position = joint.Position.ToPoint(),
						Highlightable = false
					};
					AddItem(jointImage);
					_jointWidgets.Add(joint, jointImage);
				}
			}
		}

		#endregion //Methods
	}
}
