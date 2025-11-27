using AnimationLib;
using AnimationTool.Screens;
using GameTimer;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using ResolutionBuddy;
using System.Linq;
using System.Threading.Tasks;

namespace AnimationTool.Tabs
{
	public class TestTab : AnimationToolTab, IDraggable
	{
		GameClock Clock { get; set; }
		Vector2 DudePosition { get; set; }
		EPlayback Playback { get; set; }
		string Animation { get; set; }

		public TestTab(ToolsScreen toolsScreen, AnimationManager animationManager) : base(toolsScreen, animationManager, "Test")
		{
			Animation = animationManager.Animations.Animations.First().Key;
			Playback = EPlayback.Forwards;
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			//Get the center of the thing
			DudePosition = new Vector2(Resolution.TitleSafeArea.Center.X * .7f, Resolution.TitleSafeArea.Center.Y);

			//add the animation dropdown
			AddAnimationDropdown(AnimationManager.SelectedAnimationContainer, AnimationManager.UndoStack, ToolStack);
			_animations.OnSelectedItemChange += OnAnimationSelected;

			var animationType = AddPlaybackTypeDropdown(Playback, ToolStack);
			animationType.OnSelectedItemChange += OnPlaybackSelected;

			AddItem(ToolStack);

			Clock = new GameClock();
			Clock.Start();

			FindSelectedBone(AnimationManager.HackBone);
		}

		private void OnAnimationSelected(object obj, SelectionChangeEventArgs<Animation> e)
		{
			Clock.Start();
			Animation = e.SelectedItem.Name;
			AnimationManager.SelectedAnimationContainer.SetAnimation(Animation, Playback);
		}

		private void OnPlaybackSelected(object obj, SelectionChangeEventArgs<EPlayback> e)
		{
			Clock.Start();
			Playback = e.SelectedItem;
			AnimationManager.SelectedAnimationContainer.SetAnimation(Animation, Playback);
		}

		protected override void UpdateAnimations(GameTime gameTime)
		{
			Clock.Update(gameTime);
			AnimationManager.Update(Clock, DudePosition);
		}

		public override bool CheckDrag(DragEventArgs drag)
		{
			if (!base.CheckDrag(drag))
			{
				DudePosition = DudePosition + drag.Delta;
			}
			return true;
		}

		public override void Copy()
		{
		}

		public override void Mirror()
		{
		}

		public override void Paste()
		{
		}

		public override void PasteSpecial()
		{
		}

		public override void UnKey()
		{
		}
	}
}
