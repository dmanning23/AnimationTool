using AnimationLib;
using AnimationLib.Commands;
using AnimationTool.EditScreens;
using AnimationTool.Screens;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;
using UndoRedoBuddy;
using Image = AnimationLib.Image;

namespace AnimationTool.Tabs
{
	public class AnimationTab : AnimationToolTab
	{
		#region Fields

		Bone _selectedBone;

		Checkbox _flipCheckbox;
		Checkbox _ragdollCheckbox;
		Label _relativeLayer;
		Label _absLayer;
		Checkbox _keyframeCheckbox;
		Checkbox _add360Checkbox;
		Checkbox _subtract360Checkbox;
		IntSlider _timeSlider;
		Label _currentTime;

		string _copyBone;
		Animation _copyAnimation;
		int _copyTime;

		#endregion //Fields

		#region Methods

		#region Init

		public AnimationTab(ToolsScreen toolsScreen, AnimationManager animationManager) : base(toolsScreen, animationManager, "AnimationTab")
		{
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			AnimationManager.RestartAnimation();

			//add the animation dropdown
			AddAnimationDropdown(AnimationManager.SelectedAnimationContainer, AnimationManager.UndoStack, ToolStack);
			_animations.OnSelectedItemChange += OnAnimationSelected;

			AddBoneTree(true, AnimationManager.SelectedAnimationContainer, AnimationManager.UndoStack, ToolStack, 220f, AnimationManager.ShowAnchorBonesInAnimationTab);
			_bones.OnSelectedItemChange += OnBoneSelected;

			var imageDropdown = AddImageDropdown(ToolStack);
			_images.OnSelectedItemChange += OnImageSelected;

			//add flip checkbox
			var flip = AddFlipCheckbox();

			//add ragdoll checkbox
			var ragdoll = AddRagdollCheckbox();

			//add reset translation button
			var translation = AddResetTranslationButton();

			var row = new RelativeLayout()
			{
				Size = new Vector2(360f, 32f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Top
			};
			var leftSide = new StackLayout()
			{
				Alignment = StackAlignment.Left,
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center
			};
			leftSide.AddItem(flip);
			leftSide.AddItem(new Shim() { Size = new Vector2(16f, 16f) });
			leftSide.AddItem(ragdoll);
			row.AddItem(leftSide);
			row.AddItem(translation);
			ToolStack.AddItem(row);
			AddShim(ToolStack);

			//add layer (relative) control
			AddLayerControls("Layer (relative): ", out _relativeLayer);

			//add layer (abs) control
			AddLayerControls("Layer (absolute): ", out _absLayer);

			//add keyframe checkbox
			AddKeyframeCheckbox();
			_keyframeCheckbox.OnClick += OnKeyframeChecked;

			//add animation length control

			//add time control
			AddTimeControl();
			_timeSlider.OnDrag += OnTimeChange;
			_timeSlider.OnClick += OnTimeChange;

			AddItem(ToolStack);

			PopulateControls();

			FindSelectedBone(AnimationManager.HackBone);
			AnimationManager.HackBone = null;
		}

		private IScreenItem AddFlipCheckbox()
		{
			var layout = new StackLayout(StackAlignment.Left)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
			};
			layout.AddItem(new Label("Flip: ", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			});
			_flipCheckbox = new Checkbox(false)
			{
				Size = new Vector2(16f, 16f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			};
			layout.AddItem(_flipCheckbox);
			_flipCheckbox.OnClick += OnFlipChecked;

			return layout;
		}

		private IScreenItem AddRagdollCheckbox()
		{
			var layout = new StackLayout(StackAlignment.Left)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
			};
			layout.AddItem(new Label("Ragdoll: ", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			});
			_ragdollCheckbox = new Checkbox(false)
			{
				Size = new Vector2(16f, 16f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			};
			layout.AddItem(_ragdollCheckbox);
			_ragdollCheckbox.OnClick += OnRagdollChecked;

			return layout;
		}

		private IScreenItem AddResetTranslationButton()
		{
			var translationButton = new RelativeLayoutButton()
			{
				Size = new Vector2(180f, 32f),
				Horizontal = HorizontalAlignment.Right,
				Vertical = VerticalAlignment.Top,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
				HasOutline = true
			};
			translationButton.AddItem(new Label("Reset Translation", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
				Highlightable = false
			});
			translationButton.OnClick += OnResetTranslationClicked;

			return translationButton;
		}

		private void AddLayerControls(string text, out Label layerLabel)
		{
			ToolStack.AddItem(new Label(text, Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Top,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
				Highlightable = false
			});

			//this layout will hold the whole thing
			var layout = new RelativeLayout()
			{
				Size = new Vector2(360f, 32f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				HasOutline = true,
			};

			{
				//this stack will hold the down buttons
				var downButtons = new StackLayout()
				{
					Alignment = StackAlignment.Left,
					Horizontal = HorizontalAlignment.Left,
					Vertical = VerticalAlignment.Top,
				};

				{
					//the down100 button
					var down100 = new RelativeLayoutButton()
					{
						Size = new Vector2(32f, 32f),
						Horizontal = HorizontalAlignment.Left,
						Vertical = VerticalAlignment.Top,
						TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
					};
					down100.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>("down100"))
					{
						Size = new Vector2(32f, 32f),
						FillRect = true,
						Horizontal = HorizontalAlignment.Left,
						Vertical = VerticalAlignment.Top,
					});
					down100.OnClick += OnLayerDown100Clicked;
					downButtons.AddItem(down100);
				}

				{
					//the down10 button
					var down10 = new RelativeLayoutButton()
					{
						Size = new Vector2(32f, 32f),
						Horizontal = HorizontalAlignment.Left,
						Vertical = VerticalAlignment.Top,
						TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
					};
					down10.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>("down10"))
					{
						Size = new Vector2(32f, 32f),
						FillRect = true,
						Horizontal = HorizontalAlignment.Left,
						Vertical = VerticalAlignment.Top,
					});
					down10.OnClick += OnLayerDown10Clicked;
					downButtons.AddItem(down10);
				}

				{
					//the down1 button
					var down1 = new RelativeLayoutButton()
					{
						Size = new Vector2(32f, 32f),
						Horizontal = HorizontalAlignment.Left,
						Vertical = VerticalAlignment.Top,
						TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
					};
					down1.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>("down1"))
					{
						Size = new Vector2(32f, 32f),
						FillRect = true,
						Horizontal = HorizontalAlignment.Left,
						Vertical = VerticalAlignment.Top,
					});
					downButtons.AddItem(down1);
					down1.OnClick += OnLayerDown1Clicked;
				}
				layout.AddItem(downButtons);
			}

			//add the current layer
			layerLabel = new Label("0", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			};
			layout.AddItem(layerLabel);

			{
				//this stack will hold the up buttons
				var upButtons = new StackLayout()
				{
					Alignment = StackAlignment.Right,
					Horizontal = HorizontalAlignment.Right,
					Vertical = VerticalAlignment.Top,
				};

				{
					//the up10 button
					var up100 = new RelativeLayoutButton()
					{
						Size = new Vector2(32f, 32f),
						Horizontal = HorizontalAlignment.Right,
						Vertical = VerticalAlignment.Top,
						TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
					};
					up100.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>("up100"))
					{
						Size = new Vector2(32f, 32f),
						FillRect = true,
						Horizontal = HorizontalAlignment.Right,
						Vertical = VerticalAlignment.Top,
					});
					upButtons.AddItem(up100);
					up100.OnClick += OnLayerUp100Clicked;
				}

				{
					//the up10 button
					var up10 = new RelativeLayoutButton()
					{
						Size = new Vector2(32f, 32f),
						Horizontal = HorizontalAlignment.Right,
						Vertical = VerticalAlignment.Top,
						TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
					};
					up10.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>("up10"))
					{
						Size = new Vector2(32f, 32f),
						FillRect = true,
						Horizontal = HorizontalAlignment.Right,
						Vertical = VerticalAlignment.Top,
					});
					upButtons.AddItem(up10);
					up10.OnClick += OnLayerUp10Clicked;
				}

				{
					//the up1 button
					var up1 = new RelativeLayoutButton()
					{
						Size = new Vector2(32f, 32f),
						Horizontal = HorizontalAlignment.Right,
						Vertical = VerticalAlignment.Top,
						TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
					};
					up1.AddItem(new MenuBuddy.Image(Content.Load<Texture2D>("up1"))
					{
						Size = new Vector2(32f, 32f),
						FillRect = true,
						Horizontal = HorizontalAlignment.Right,
						Vertical = VerticalAlignment.Top,
					});
					upButtons.AddItem(up1);
					up1.OnClick += OnLayerUp1Clicked;
				}
				layout.AddItem(upButtons);
			}

			ToolStack.AddItem(layout);

			AddShim(ToolStack);
		}

		private void AddKeyframeCheckbox()
		{
			var row = new RelativeLayout()
			{
				Size = new Vector2(360f, 32f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Top
			};
			var layout = new StackLayout(StackAlignment.Left)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
			};

			//add the keyframe checkbox
			layout.AddItem(new Label("Keyframe: ", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Right,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			});
			_keyframeCheckbox = new Checkbox(false)
			{
				Size = new Vector2(16f, 16f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			};
			layout.AddItem(_keyframeCheckbox);
			_keyframeCheckbox.OnClick += OnKeyframeChecked;

			layout.AddItem(new Shim(16, 0));

			//add the +360 checkbox
			layout.AddItem(new Label("+360: ", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			});
			_add360Checkbox = new Checkbox(false)
			{
				Size = new Vector2(16f, 16f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			};
			layout.AddItem(_add360Checkbox);
			_add360Checkbox.OnClick += OnAdd360Checked;

			layout.AddItem(new Shim(16, 0));

			//add the -360 checkbox
			layout.AddItem(new Label("-360: ", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			});
			_subtract360Checkbox = new Checkbox(false)
			{
				Size = new Vector2(16f, 16f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			};
			layout.AddItem(_subtract360Checkbox);
			_subtract360Checkbox.OnClick += OnSubtract360Checked;

			row.AddItem(layout);
			ToolStack.AddItem(row);

			//AddShim(ToolStack);
		}

		private void AddTimeControl()
		{
			var layout = new StackLayout(StackAlignment.Left)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
			};
			layout.AddItem(new Label("Time: ", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			});
			_currentTime = new Label("0 ", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Top,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
				Highlightable = false
			};
			layout.AddItem(_currentTime);
			ToolStack.AddItem(layout);

			_timeSlider = new IntSlider()
			{
				Min = 0,
				Max = 60,
				SliderPosition = 0,
				HandleSize = new Vector2(8, 32),
				Size = new Vector2(360f, 32f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Top,
				TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
			};
			ToolStack.AddItem(_timeSlider);
			AddShim(ToolStack);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);
		}

		#endregion //Init

		#region Event Handlers

		private void OnBoneSelected(object obj, SelectionChangeEventArgs<Bone> e)
		{
			_selectedBone = e.SelectedItem;
			_images.AddData(e.SelectedItem);
			_boneLabel.Text = e.SelectedItem.Name;

			//populate all those other controls
			PopulateControls();
		}

		private void OnAnimationSelected(object obj, SelectionChangeEventArgs<Animation> e)
		{
			AnimationManager.SelectedAnimationContainer.SetAnimation(e.SelectedItem.Name, EPlayback.Forwards);
			AnimationManager.RestartAnimation();

			//populate the time slider
			_timeSlider.Max = (int)(AnimationManager.SelectedAnimationContainer.CurrentAnimation.Length * 60f);
			_timeSlider.SliderPosition = 0;

			//populate all those other controls
			PopulateControls();
		}

		private void OnImageSelected(object obj, SelectionChangeEventArgs<Image> e)
		{
			if (null != _selectedBone &&
				null != _animations.SelectedItem)
			{
				//get its current keyelement
				AnimationManager.UndoStack.Add(new SetKeyElementImage(CurrentAnimation(), _selectedBone, _timeSlider.SliderPosition, _selectedBone.GetImageIndex(e.SelectedItem)));
			}

			PopulateControls();
		}

		private void OnFlipChecked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone &&
				null != _animations.SelectedItem)
			{
				//get its current keyelement
				AnimationManager.UndoStack.Add(new SetKeyElementFlip(CurrentAnimation(), _selectedBone, _timeSlider.SliderPosition, _flipCheckbox.IsChecked));
			}

			PopulateControls();
		}

		private void OnRagdollChecked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone &&
				null != _animations.SelectedItem)
			{
				//get its current keyelement
				AnimationManager.UndoStack.Add(new SetKeyElementRagdoll(CurrentAnimation(), _selectedBone, _timeSlider.SliderPosition, _ragdollCheckbox.IsChecked));
			}

			PopulateControls();
		}

		private void OnResetTranslationClicked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone &&
				null != _animations.SelectedItem)
			{
				//get its current keyelement
				AnimationManager.UndoStack.Add(new SetKeyElementTranslation(CurrentAnimation(), _selectedBone, _timeSlider.SliderPosition, Vector2.Zero));
			}

			PopulateControls();
		}

		#region layer changes

		private void OnLayerDown1Clicked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone)
			{
				ChangeLayer(_selectedBone.AnchorJoint.CurrentKeyElement, -1);
			}
		}

		private void OnLayerDown10Clicked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone)
			{
				ChangeLayer( _selectedBone.AnchorJoint.CurrentKeyElement, -10);
			}
		}

		private void OnLayerDown100Clicked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone)
			{
				ChangeLayer(_selectedBone.AnchorJoint.CurrentKeyElement, -100);
			}
		}

		private void OnLayerUp1Clicked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone)
			{
				ChangeLayer(_selectedBone.AnchorJoint.CurrentKeyElement, 1);
			}
		}

		private void OnLayerUp10Clicked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone)
			{
				ChangeLayer(_selectedBone.AnchorJoint.CurrentKeyElement, 10);
			}
		}

		private void OnLayerUp100Clicked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone)
			{
				ChangeLayer(_selectedBone.AnchorJoint.CurrentKeyElement, 100);
			}
		}

		private void ChangeLayer(KeyElement key, int layerChange)
		{
			if (null != _selectedBone &&
				null != _animations.SelectedItem)
			{
				AnimationManager.UndoStack.Add(new SetKeyElementLayer(
					CurrentAnimation(), 
					_selectedBone, 
					_timeSlider.SliderPosition, key.Layer + layerChange));
			}

			PopulateControls();
		}

		#endregion //layer changes

		private void OnKeyframeChecked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone)
			{
				//get teh currently selected animation
				var myAnimation = CurrentAnimation();
				
				//get its current keyelement
				var currentKeyElement = _selectedBone.AnchorJoint.CurrentKeyElement;

				//do a Addkeyframe/RemoveKeyFrame undo/redo action
				ICommand action = null;
				if (!_keyframeCheckbox.IsChecked)
				{
					//remove a keyframe
					action = new RemoveKeyElement(myAnimation, currentKeyElement);
				}
				else
				{
					//adda keyframe

					//create the new keyframe
					KeyElement key = new KeyElement(_selectedBone);

					//copy from the current keyframe
					key.Copy(currentKeyElement);

					//set the keyframe property
					key.KeyFrame = _keyframeCheckbox.IsChecked;

					//add to the animation
					action = new SetKeyElement(myAnimation, currentKeyElement, key);
				}

				AnimationManager.UndoStack.Add(action);
			}

			PopulateControls();
		}

		private void OnAdd360Checked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone &&
				null != _animations.SelectedItem)
			{
				var key = _selectedBone.AnchorJoint.CurrentKeyElement;

				float limit = 0f;
				if (_add360Checkbox.IsChecked)
				{
					//get teh vector to the thing
					limit = key.Rotation + MathHelper.TwoPi;
				}
				else
				{
					limit = key.Rotation - MathHelper.TwoPi;
				}

				AnimationManager.UndoStack.Add(new SetKeyElementRotation(
						CurrentAnimation(), _selectedBone, key.Time, limit));

				PopulateControls();
			}
		}

		private void OnSubtract360Checked(object obj, ClickEventArgs e)
		{
			if (null != _selectedBone &&
				null != _animations.SelectedItem)
			{
				var key = _selectedBone.AnchorJoint.CurrentKeyElement;

				float limit = 0f;
				if (!_subtract360Checkbox.IsChecked)
				{
					//get teh vector to the thing
					limit = key.Rotation + MathHelper.TwoPi;
				}
				else
				{
					limit = key.Rotation - MathHelper.TwoPi;
				}

				AnimationManager.UndoStack.Add(new SetKeyElementRotation(
						CurrentAnimation(), _selectedBone, key.Time, limit));

				PopulateControls();
			}
		}

		private void OnTimeChange(object obj, DragEventArgs e)
		{
			OnTimeChange();
		}

		private void OnTimeChange(object obj, ClickEventArgs e)
		{
			OnTimeChange();
		}

		private void OnTimeChange()
		{
			AnimationManager.Time = _timeSlider.SliderPosition / 60f;
			PopulateControls();
		}

		#endregion //Event Handlers

		private void AddAnimationEditScreen()
		{
			ToolsScreen.ClearEditScreens();
			ScreenManager.AddScreen(new EditAnimationScreen(AnimationManager, _selectedBone, _timeSlider.SliderPosition));
		}

		private void PopulateControls()
		{
			//run an update on the animation, just to make sure it is all set up
			AnimationManager.Update(true, EPlayback.Forwards);

			//set all the controls of the animation tab
			if ((null == _selectedBone) ||
				(null == _selectedBone.AnchorJoint.CurrentKeyElement) ||
				(null == AnimationManager.SelectedAnimationContainer.CurrentAnimation))
			{
				//nothing is selected, disable all the controls
				_flipCheckbox.IsChecked = false;
				_keyframeCheckbox.IsChecked = false;
				_ragdollCheckbox.IsChecked= false;
				_relativeLayer.Text = "n/a";
				_absLayer.Text = "n/a";
				_keyframeCheckbox.IsChecked = false;
				_add360Checkbox.IsChecked = false;
				_subtract360Checkbox.IsChecked = false;
				_timeSlider.Enabled = false;
				_currentTime.Text = "n/a";
			}
			else
			{
				var currentKeyElement = _selectedBone.AnchorJoint.CurrentKeyElement;

				//set the flip checkbox
				_flipCheckbox.IsChecked = currentKeyElement.Flip;

				//set teh keyframe checkbox
				_keyframeCheckbox.IsChecked = currentKeyElement.KeyFrame;

				//set the ragdoll checkbox
				_ragdollCheckbox.IsChecked = currentKeyElement.Ragdoll;

				////enable the "reset translation" button
				//bool bEnableResetButton = ((0.0f != currentKeyElement.Translation.X) || (0.0f != currentKeyElement.Translation.Y));
				//m_ResetTranslation.Enabled = bEnableResetButton;

				////set the rotation edit box
				//m_editRotation.Text = MathHelper.ToDegrees(currentKeyElement.Rotation).ToString();

				_add360Checkbox.IsChecked = currentKeyElement.Rotation >= MathHelper.Pi;
				_subtract360Checkbox.IsChecked = currentKeyElement.Rotation <= -MathHelper.Pi;

				//set the layer edit box
				_relativeLayer.Text = currentKeyElement.Layer.ToString();

				//set the abs layer edit box
				_absLayer.Text = _selectedBone.CurrentLayer.ToString();

				//set the image combo box
				_images.SelectedIndex = currentKeyElement.ImageIndex + 1;

				_timeSlider.Enabled = true;
				_timeSlider.Max = (int)(AnimationManager.SelectedAnimationContainer.CurrentAnimation.Length * 60f);

				//add all the marks to the time slider
				_timeSlider.Marks.Clear();
				if (null != _animations.SelectedItem)
				{
					var keyJoint = GetKeyJoint(_selectedBone, _selectedBone.Name);
					var keys = keyJoint.Elements;
					foreach (var key in keys)
					{
						_timeSlider.Marks.Add(key.Time);
					}
				}

				_currentTime.Text = _timeSlider.SliderPosition.ToString();

				//find the EditAnimationScreen and update the controls
				AddAnimationEditScreen();
			}
		}

		private KeyJoint GetKeyJoint(Bone currentBone, string boneName)
		{
			KeyJoint keyJoint = null;
			if (currentBone.IsPartOfGarment)
			{
				if (currentBone is GarmentBone garmentBone)
				{
					//this joint must be in a garment
					keyJoint = garmentBone.GarmentAnimationContainer.CurrentAnimation.GetKeyJoint(boneName);
				}
				else
				{
					//Go back up into the parent until we find the garmentBone
					keyJoint = GetKeyJoint(currentBone.ParentBone, boneName);
				}
			}
			else
			{
				keyJoint = AnimationManager.SelectedAnimationContainer.CurrentAnimation.GetKeyJoint(boneName);
			}

			return keyJoint;
		}

		public override void Copy()
		{
			if (null == _selectedBone)
			{
				return;
			}

			//get the currently selected bone
			_copyBone = _selectedBone.Name;

			//get teh currently selected animation
			_copyAnimation = _animations.SelectedItem;

			//get the current time
			_copyTime = _timeSlider.SliderPosition;
		}

		public override void Paste()
		{
			Paste(false);
		}

		public override void PasteSpecial()
		{
			Paste(true);
		}

		public void Paste(bool selectivePaste)
		{
			//get the currently selected animation
			var targetAnimation = _animations.SelectedItem;

			//get the previously selected animation
			var sourceAnimation = _copyAnimation;
			if ((null == sourceAnimation) || (null == targetAnimation))
			{
				return;
			}

			//get the bone we are copying
			var copyBone = AnimationManager.SelectedAnimationContainer.Skeleton.GetBone(_copyBone);
			if (null == copyBone)
			{
				return;
			}

			//get the currently selected time
			int currentTime = _timeSlider.SliderPosition;

			//create the actino to copy the selected part of the animation!
			var myAction = new CommandStack();
			sourceAnimation.Copy(myAction, copyBone, targetAnimation, _copyTime, currentTime, selectivePaste);
			AnimationManager.UndoStack.Add(myAction);

			PopulateControls();
		}

		public override void Mirror()
		{
			//get teh currently selected animation
			Animation myAnimation = _animations.SelectedItem;
			if (null == myAnimation)
			{
				return;
			}

			if (null == _selectedBone)
			{
				return;
			}

			//get the currently selected bone
			var selectedKeyBone = myAnimation.GetKeyBone(_selectedBone.Name);
			if (null == selectedKeyBone)
			{
				return;
			}

			//get the currently selected time
			int currentTime = _timeSlider.SliderPosition;

			//create the action to mirror the right bits to the left bits
			var myAction = new CommandStack();
			selectedKeyBone.MirrorRightToLeft(selectedKeyBone, myAction, currentTime, myAnimation);
			AnimationManager.UndoStack.Add(myAction);

			PopulateControls();
		}

		public override void UnKey()
		{
			//get the currently selected animation
			Animation myAnimation = _animations.SelectedItem;
			if (null == myAnimation)
			{
				return;
			}

			//get the bone we are copying
			if (null == _selectedBone)
			{
				return;
			}

			//get the currently selected time
			int currentTime = _timeSlider.SliderPosition;

			//create the actino to copy the selected part of the animation!
			var myAction = new CommandStack();
			myAnimation.RemoveKeyframe(myAction, _selectedBone, currentTime);
			AnimationManager.UndoStack.Add(myAction);

			PopulateControls();
		}

		public override void ExitScreen()
		{
			//set the hack bone first
			if (null != _bones.SelectedTreeItem)
			{
				var hackBone = _bones.SelectedItem;
				AnimationManager.HackBone = hackBone;
			}

			base.ExitScreen();
		}

		private Animation CurrentAnimation()
		{
			return GetCurrentAnimation(_selectedBone, _animations.SelectedItem);
		}

		private Animation GetCurrentAnimation(Bone bone, Animation defaultAnimation)
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
					animation = GetCurrentAnimation(bone.ParentBone, defaultAnimation);
				}
			}
			else
			{
				animation = defaultAnimation;
			}

			return animation;
		}

		#endregion //Methods
	}
}
