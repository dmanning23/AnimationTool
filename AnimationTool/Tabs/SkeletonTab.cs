using AnimationLib;
using AnimationLibWidgets;
using AnimationTool.EditScreens;
using AnimationTool.Screens;
using MenuBuddy;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using UndoRedoBuddy;
using Image = AnimationLib.Image;

namespace AnimationTool.Tabs
{
	public class SkeletonTab : AnimationToolTab
	{
		#region Fields

		Bone _selectedBone;

		BoneTypeDropdown _boneTypes;

		#endregion //Fields

		#region Methods

		public SkeletonTab(ToolsScreen toolsScreen, AnimationManager animationManager) : base(toolsScreen, animationManager, "SkeletonTab")
		{
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			var dropdown = AddAnimationContainerDropdown(AnimationManager.Animations, AnimationManager.Garments, AnimationManager.SelectedAnimationContainer, ToolStack);
			dropdown.OnSelectedItemChange += OnAnimationContainerChanged;

			AddBoneTree(true, AnimationManager.SelectedAnimationContainer, AnimationManager.UndoStack, ToolStack, 320f, AnimationManager.ShowAnchorBonesInAnimationTab);
			_bones.OnSelectedItemChange += OnBoneSelected;

			_joints = AddJointDropdown(AnimationManager.SelectedAnimationContainer, AnimationManager.UndoStack, ToolStack);
			_joints.OnSelectedItemChange += OnJointSelected;

			AddImageDropdown(ToolStack);
			_images.OnSelectedItemChange += OnImageSelected;

			AddEditImagesButton();

			_boneTypes = AddBoneTypeDropdown(ToolStack);
			_boneTypes.OnSelectedItemChange += OnBoneTypeSelected;

			AddItem(ToolStack);

			FindSelectedBone(AnimationManager.HackBone);
		}

		private void AddEditImagesButton()
		{
			//add the "Add Quest" button
			var button = CreateButton("Edit Images", ToolStack);
			button.OnClick += async (obj, e) =>
			{
				if (null != AnimationManager.HackBone)
				{
					var editor = new ImageEditorScreen();
					var editImages = new EditImagesScreen(ToolsScreen, AnimationManager, AnimationManager.HackBone, editor);
					ScreenManager.ClearScreens();
					await LoadingScreen.Load(ScreenManager, new IScreen[] { editor, editImages });
				}
			};
		}

		private void OnBoneSelected(object obj, SelectionChangeEventArgs<Bone> e)
		{
			_selectedBone = e.SelectedItem;
			_joints.AddData(e.SelectedItem);
			_images.AddData(e.SelectedItem);
			_boneTypes.AddData(e.SelectedItem);
			_boneLabel.Text = e.SelectedItem.Name;

			AnimationManager.HackBone = _selectedBone;

			AddSkeletonEditScreen();
		}

		private void OnJointSelected(object obj, SelectionChangeEventArgs<Joint> e)
		{
			AddSkeletonEditScreen();
		}

		private void OnBoneTypeSelected(object obj, SelectionChangeEventArgs<EBoneType> e)
		{
			_selectedBone.BoneType = e.SelectedItem;
		}

		private void OnImageSelected(object obj, SelectionChangeEventArgs<Image> e)
		{
			AnimationManager.HackImageIndex = _selectedBone.GetImageIndex(e.SelectedItem);
			AddSkeletonEditScreen();
		}

		private void AddSkeletonEditScreen()
		{
			ToolsScreen.ClearEditScreens();
			ScreenManager.AddScreen(
				new EditSkeletonScreen(AnimationManager, 
					_selectedBone, 
					null != _joints.SelectedDropdownItem ? _joints.SelectedDropdownItem.Item : null));
		}

		private void OnAnimationContainerChanged(object obj, SelectionChangeEventArgs<AnimationContainer> e)
		{
			AnimationManager.SelectedAnimationContainer = e.SelectedItem;
			ToolsScreen.ClearTabsAndScreens();
		}

		protected override void UpdateAnimations(GameTime gameTime)
		{
			AnimationManager.Update(false, EPlayback.Forwards);
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
