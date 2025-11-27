using AnimationLib;
using AnimationTool.EditScreens;
using AnimationTool.Screens;
using CollisionBuddy;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;
using UndoRedoBuddy;
using Image = AnimationLib.Image;

namespace AnimationTool.Tabs
{
	public class PhysicsTab : AnimationToolTab
	{
		#region Fields

		Bone _selectedBone;

		#endregion //Fields

		#region Methods

		public PhysicsTab(ToolsScreen toolsScreen, AnimationManager animationManager) : base(toolsScreen, animationManager, "PhysicsTab")
		{
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			AddBoneTree(true, AnimationManager.SelectedAnimationContainer, AnimationManager.UndoStack, ToolStack, 256f, AnimationManager.ShowAnchorBonesInAnimationTab);
			_bones.OnSelectedItemChange += OnBoneSelected;

			AddImageDropdown(ToolStack);
			_images.OnSelectedItemChange += OnImageSelected;

			AddPhysicsDropdown(ToolStack);
			_physics.OnSelectedItemChange += OnPhysicsSelected;

			AddItem(ToolStack);

			FindSelectedBone(AnimationManager.HackBone);
		}

		private void OnBoneSelected(object obj, SelectionChangeEventArgs<Bone> e)
		{
			_selectedBone = e.SelectedItem;
			_images.AddData(e.SelectedItem);
			_physics.AddData(e.SelectedItem);
			_boneLabel.Text = e.SelectedItem.Name;

			AnimationManager.HackBone = _selectedBone;

			AddPhysicsEditScreen();
		}

		private void OnImageSelected(object obj, SelectionChangeEventArgs<Image> e)
		{
			AnimationManager.HackImageIndex = _selectedBone.GetImageIndex(e.SelectedItem);

			AddPhysicsEditScreen();
		}

		private void OnPhysicsSelected(object obj, SelectionChangeEventArgs<ICollidable> e)
		{
			AddPhysicsEditScreen();
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			AnimationManager.Renderer.SpriteBatchBegin(BlendState.AlphaBlend, Matrix.Identity);

			//draw all the physics data
			AnimationManager.SelectedAnimationContainer.Skeleton.DrawPhysics(AnimationManager.Renderer, Color.White);

			AnimationManager.Renderer.SpriteBatchEnd();
		}

		private void AddPhysicsEditScreen()
		{
			ToolsScreen.ClearEditScreens();

			var circle = _physics.SelectedDropdownItem?.Item as PhysicsCircle;
			if (null != circle)
			{
				AddCircleEditScreen(circle);
			}
			else
			{
				var line = _physics.SelectedDropdownItem?.Item as PhysicsLine;
				if (null != line)
				{
					AddLineEditScreen(line);
				}
			}
		}

		private void AddCircleEditScreen(PhysicsCircle circle)
		{
			ScreenManager.AddScreen(new EditCircleScreen(AnimationManager, _selectedBone, circle));
		}

		private void AddLineEditScreen(PhysicsLine circle)
		{
			throw new NotImplementedException();
		}

		public override void Copy()
		{
		}

		public override void Paste()
		{
		}

		public override void PasteSpecial()
		{
		}

		public override void Mirror()
		{
			if (null != _selectedBone)
			{
				var myAction = new CommandStack();
				AnimationManager.SelectedAnimationContainer.Skeleton.Mirror(myAction);
				AnimationManager.UndoStack.Add(myAction);
			}
		}

		public override void UnKey()
		{
		}

		#endregion //Methods
	}
}
