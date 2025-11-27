using AnimationLib;
using AnimationLib.Commands;
using AnimationLibWidgets;
using AnimationTool.EditScreens;
using AnimationTool.Screens;
using MenuBuddy;
using Microsoft.Xna.Framework;
using System;
using System.Threading.Tasks;
using UndoRedoBuddy;
using Image = AnimationLib.Image;

namespace AnimationTool.Tabs
{
    public class RagdollTab : AnimationToolTab
    {
        #region Fields

        Bone _selectedBone;

        RagdollTypeDropdown _ragdollType;

        NumEdit _springSlider;
        NumEdit _gravitySlider;
        NumEdit _weightTransfer;

        #endregion //Fields

        #region Methods

        public RagdollTab(ToolsScreen toolsScreen, AnimationManager animationManager) : base(toolsScreen, animationManager, "RagdollTab")
        {
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            AddBoneTree(true, AnimationManager.SelectedAnimationContainer, AnimationManager.UndoStack, ToolStack, 256f, AnimationManager.ShowAnchorBonesInAnimationTab);
            _bones.OnSelectedItemChange += OnBoneSelected;

            AddImageDropdown(ToolStack);
            _images.OnSelectedItemChange += OnImageSelected;

            //add the "ragdoll float" checkbox
            ToolStack.AddItem(new Label("Ragdoll Type: ", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
                Highlightable = false
            });
            _ragdollType = new RagdollTypeDropdown(this)
            {
                Size = new Vector2(360f, 32f),
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
                HasOutline = true
            };
            _ragdollType.OnSelectedItemChange += OnRagdollType;
            ToolStack.AddItem(_ragdollType);
            AddShim(ToolStack);

            AddSpringControl();

            AddGravityControl();

            AddWeightTransfer();

            AddItem(ToolStack);

            PopulateControls();

            FindSelectedBone(AnimationManager.HackBone);
        }

        private void AddSpringControl()
        {
            ToolStack.AddItem(new Label("Spring: ", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Center,
                Highlightable = false,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
            });

            _springSlider = new NumEdit(Content, FontSize.Small)
            {
                Min = 0f,
                Max = 10f,
                Number = 1f,
                //HandleSize = new Vector2(8, 32),
                Size = new Vector2(360f, 32f),
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
                HasOutline = true,
            };
            ToolStack.AddItem(_springSlider);
            _springSlider.OnNumberEdited += OnSpringChanged;
            AddShim(ToolStack);
        }

        private void AddGravityControl()
        {
            ToolStack.AddItem(new Label("Gravity: ", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Center,
                Highlightable = false,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
            });

            _gravitySlider = new NumEdit(Content, FontSize.Small)
            {
                Min = 0f,
                Max = 5000f,
                Number = 1000f,
                //HandleSize = new Vector2(8, 32),
                Size = new Vector2(360f, 32f),
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
                HasOutline = true,
            };
            ToolStack.AddItem(_gravitySlider);
            _gravitySlider.OnNumberEdited += OnGravityChanged;
            AddShim(ToolStack);
        }

        private void AddWeightTransfer()
        {
            ToolStack.AddItem(new Label("Weight Transfer: ", Content, FontSize.Small)
            {
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Center,
                Highlightable = false,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
            });

            _weightTransfer = new NumEdit(Content, FontSize.Small)
            {
                Min = 0f,
                Max = 1f,
                Number = 0.5f,
                Size = new Vector2(360f, 32f),
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopRight),
                HasOutline = true,
            };
            ToolStack.AddItem(_weightTransfer);
            _weightTransfer.OnNumberEdited += OnWeightTransferChanged;
            AddShim(ToolStack);
        }

        private void OnBoneSelected(object obj, SelectionChangeEventArgs<Bone> e)
        {
            _selectedBone = e.SelectedItem;
            _images.AddData(e.SelectedItem);
            _boneLabel.Text = e.SelectedItem.Name;

            PopulateControls();

            AnimationManager.HackBone = _selectedBone;

            AddRagdollEditScreen();
        }

        private void OnImageSelected(object obj, SelectionChangeEventArgs<Image> e)
        {
            AnimationManager.HackImageIndex = _selectedBone.GetImageIndex(e.SelectedItem);

            PopulateControls();

            AddRagdollEditScreen();
        }

        private void OnGravityChanged(object obj, NumChangeEventArgs e)
        {
            AnimationManager.UndoStack.Add(new SetRagdollGravity(_images.SelectedItem, _gravitySlider.Number));
            AddRagdollEditScreen();
        }

        private void OnWeightTransferChanged(object obj, NumChangeEventArgs e)
        {
            if (null != _selectedBone)
            {
                _selectedBone.RagdollWeightRatio = _weightTransfer.Number;
            }
        }

        private void OnRagdollType(object obj, SelectionChangeEventArgs<RagdollType> e)
        {
            if (null != _selectedBone)
            {
                AnimationManager.UndoStack.Add(new SetRagdollType(_selectedBone.AnchorJoint.Data, _ragdollType.SelectedItem));
                AddRagdollEditScreen();
            }
        }

        private void OnSpringChanged(object obj, NumChangeEventArgs e)
        {
            AnimationManager.UndoStack.Add(new SetRagdollSpring(_images.SelectedItem, _springSlider.Number));
            AddRagdollEditScreen();
        }

        private void PopulateControls()
        {
            //run an update on the animation, just to make sure it is all set up
            AnimationManager.Update(true, EPlayback.Forwards);

            //set all the controls of the animation tab
            if ((null == _selectedBone) ||
                (null == _selectedBone.AnchorJoint.CurrentKeyElement) ||
                (null == AnimationManager.SelectedAnimationContainer.CurrentAnimation) ||
                (null == _images.SelectedItem))
            {
                //nothing is selected, disable all the controls
                _ragdollType.SelectedItem = RagdollType.None;

            }
            else
            {
                _ragdollType.AddData(_selectedBone);
                _springSlider.Number = _images.SelectedItem.RagdollSpring;
                _gravitySlider.Number = _images.SelectedItem.RagdollGravity.Y;
                _weightTransfer.Number = _selectedBone.RagdollWeightRatio;
            }
        }

        private void AddRagdollEditScreen()
        {
            ToolsScreen.ClearEditScreens();

            if (_selectedBone != null)
            {
                var data = _selectedBone.AnchorJoint.Data;
                switch (data.RagdollType)
                {
                    case RagdollType.Float:
                        {
                            AddRagdollFloatEditScreen();
                        }
                        break;
                    case RagdollType.Limit:
                        {
                            AddRagdollLimitEditScreen();
                        }
                        break;
                }
            }
        }

        private void AddRagdollFloatEditScreen()
        {
            ScreenManager.AddScreen(new EditFloatingRagdollScreen(AnimationManager, _selectedBone));
        }

        private void AddRagdollLimitEditScreen()
        {
            ScreenManager.AddScreen(new EditLimitRagdollScreen(AnimationManager, _selectedBone));
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

        public override void Draw(GameTime gameTime)
        {
            try
            {
                base.Draw(gameTime);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion //Methods
    }
}
