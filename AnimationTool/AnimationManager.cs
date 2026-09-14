using AnimationLib;
using FilenameBuddy;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using RenderBuddy;
using ResolutionBuddy;
using System.Collections.Generic;
using System.Linq;
using UndoRedoBuddy;

namespace AnimationTool
{
    /// <summary>
    /// This is the class that gets passed around to manage the animation being worked out
    /// </summary>
    public class AnimationManager
    {
        #region Properties

        private readonly Game _game;

        public Renderer Renderer { get; set; }

        private AnimationsLoader AnimationLoader { get; set; }

        public AnimationContainer Animations => AnimationLoader.Animations;

        public List<Garment> Garments => AnimationLoader.Garments;

        private AnimationContainer _selectedAnimationContainer;
        public AnimationContainer SelectedAnimationContainer
        {
            get
            {
                return _selectedAnimationContainer;
            }
            set
            {
                _selectedAnimationContainer = value;
                ResetHack();
            }
        }

        public UndoRedoStack UndoStack { get; private set; }

        ContentManager _content;

        public static bool ShowAnchorBonesInAnimationTab { get; set; }

        public Filename ModelFile => AnimationLoader.ModelFile;

        #region Hack Items

        /// <summary>
        /// The time to draw the animation at
        /// </summary>
        public float Time { get; set; }

        /// <summary>
        /// The rotation to draw the selected bone at
        /// </summary>
        public float HackRotation { get; set; }

        /// <summary>
        /// The translation to draw the selected bone at
        /// </summary>
        public Vector2 HackTranslation { get; set; }

        /// <summary>
        /// The image index to draw of the bone
        /// </summary>
        public int HackImageIndex { get; set; }

        /// <summary>
        /// The bone that is being hacked
        /// </summary>
        private Bone _hackBone;
        public Bone HackBone
        {
            get
            {
                return _hackBone;
            }
            set
            {
                //start at the current rotation of the bone
                _hackBone = value;

                if (null == _hackBone)
                {
                    ResetHack();
                }
                else
                {
                    HackRotation = _hackBone.Rotation;
                    HackTranslation = _hackBone.AnchorJoint.CurrentKeyElement.Translation;
                    HackImageIndex = _hackBone.AnchorJoint.CurrentKeyElement.ImageIndex;
                }
            }
        }

        #endregion //Hack Items

        #endregion //Properties

        #region Methods

        public AnimationManager(Game game)
        {
            _game = game;
            UndoStack = new UndoRedoStack();
        }

        public void LoadContent()
        {
            _content = new ContentManager(_game.Services, "Content");
            Renderer = new Renderer(_game, _content);
            Renderer.AmbientColor = new Color(.2f, .2f, .2f);
            Renderer.ClearLights();
            Renderer.AddDirectionalLight(new Vector3(0f, 1f, .1f), new Color(.2f, 0f, .3f));
            Renderer.AddDirectionalLight(new Vector3(-.5f, -1f, -.1f), new Color(1f, .7f, 0f, 0.75f));
            Renderer.AddDirectionalLight(new Vector3(.5f, -1f, .6f), new Color(1f, 1f, .75f));

            Renderer.TextureLoader = new TextureFileLoader();

            Renderer.LoadContent(_game.GraphicsDevice);

            ShowAnchorBonesInAnimationTab = true;

            AnimationLoader = new AnimationsLoader(Renderer, 1f);
            NewModel();
        }

        public void RestartAnimation()
        {
            SelectedAnimationContainer.RestartAnimation();
            Time = 0;
        }

        public void Update(bool updateHack, EPlayback playback)
        {
            //Get the center of the thing
            var position = new Vector2(Resolution.TitleSafeArea.Center.X * .7f, Resolution.TitleSafeArea.Center.Y);

            //set the clock
            SelectedAnimationContainer.StopWatch.CurrentTime = Time;

            //update the model thing
            SelectedAnimationContainer.Update(new GameClock(), position, false, 0.0f, true);

            //is the correct stuff set?
            if (null == HackBone)
            {
                return;
            }

            //get the flip status
            bool currentFlip = HackBone.Flipped;
            bool anchorFlip = HackBone.AnchorJoint.CurrentKeyElement.Flip;
            bool parentFlip = (currentFlip && !anchorFlip); //bone is flipped but not set in anchor

            if (updateHack)
            {
                //do the update hack
                HackBone.HackUpdate(
                    HackBone.AnchorJoint.CurrentKeyElement.Layer,
                    HackImageIndex,
                    HackRotation,
                    parentFlip,
                    HackTranslation,
                    playback);
            }
        }

        public void Update(GameClock clock, Vector2 position)
        {
            if (null == SelectedAnimationContainer.Skeleton.RootBone)
            {
                return;
            }

            //update the model thing
            SelectedAnimationContainer.Update(clock, position, false, 0.0f, false);
            SelectedAnimationContainer.UpdateRagdoll();
        }

        public void ResetHack()
        {
            ClearHack();
            _hackBone = null;
        }

        public void ClearHack()
        {
            HackRotation = 0.0f;
            HackTranslation = Vector2.Zero;
            HackImageIndex = 0;
        }

        public void NewModel()
        {
            AnimationLoader.LoadContent();
            AnimationLoader.ModelFile = new Filename();
            SelectedAnimationContainer = Animations;
        }

        public void OpenModel()
        {
            var path = FileDialogs.OpenFile("xml", "Open Model");
            if (null == path)
            {
                return;
            }

            NewModel();

            var modelFile = new Filename { File = path };
            AnimationLoader.ModelFile = modelFile;
            AnimationLoader.Animations.ReadSkeletonXml(modelFile, Renderer);
        }

        public void OpenAnimation()
        {
            var path = FileDialogs.OpenFile("xml", "Open Animation");
            if (null == path)
            {
                return;
            }

            if (null == AnimationLoader.Animations.Skeleton.RootBone)
            {
                return;
            }

            var animationFile = new Filename { File = path };
            AnimationLoader.Animations.ReadAnimationXml(animationFile);

            var animation = AnimationLoader.Animations.Animations.First().Key;
            AnimationLoader.Animations.SetAnimation(animation, EPlayback.Forwards);
        }

        public void NewGarment()
        {
            var garment = new Garment(AnimationLoader.Animations.Scale);
            garment.AddToSkeleton();
            AnimationLoader.Garments.Add(garment);
        }

        public void OpenGarment()
        {
            var path = FileDialogs.OpenFile("xml", "Open Garment");
            if (null == path)
            {
                return;
            }

            if (null == AnimationLoader.Animations.Skeleton.RootBone)
            {
                return;
            }

            var garmentFile = new Filename { File = path };
            var garment = new Garment(garmentFile, AnimationLoader.Animations.Skeleton, Renderer);
            garment.AddToSkeleton();
            AnimationLoader.Garments.Add(garment);
        }

        public void Save()
        {
            if (!HasPath(AnimationLoader.Animations.SkeletonFile) || !HasPath(AnimationLoader.Animations.AnimationFile))
            {
                SaveAs();
                return;
            }

            AnimationLoader.Animations.WriteXml();
            SaveGarments();
        }

        public void SaveAs()
        {
            var skeletonPath = FileDialogs.SaveFile("xml", "Model.xml", "Save Model As");
            if (null == skeletonPath)
            {
                return;
            }

            var animationPath = FileDialogs.SaveFile("xml", "Animations.xml", "Save Animation As");
            if (null == animationPath)
            {
                return;
            }

            AnimationLoader.Animations.WriteSkeletonXml(new Filename { File = skeletonPath });
            AnimationLoader.Animations.WriteAnimationXml(new Filename { File = animationPath });
            AnimationLoader.ModelFile = AnimationLoader.Animations.SkeletonFile;

            SaveGarments();
        }

        public void SaveAsJson()
        {
            var skeletonPath = FileDialogs.SaveFile("json", "Model.json", "Save Model As JSON");
            if (null == skeletonPath)
            {
                return;
            }

            var animationPath = FileDialogs.SaveFile("json", "Animations.json", "Save Animation As JSON");
            if (null == animationPath)
            {
                return;
            }

            // WriteSkeletonJson/WriteAnimationJson permanently reassign SkeletonFile/AnimationFile
            // to the JSON path, so snapshot and restore them afterward: SaveAsJson is meant to be
            // an "export" that doesn't disturb the model's actual (XML) save location.
            var originalSkeletonFile = AnimationLoader.Animations.SkeletonFile;
            var originalAnimationFile = AnimationLoader.Animations.AnimationFile;

            AnimationLoader.Animations.WriteSkeletonJson(new Filename { File = skeletonPath });
            AnimationLoader.Animations.WriteAnimationJson(new Filename { File = animationPath });

            AnimationLoader.Animations.SkeletonFile = originalSkeletonFile;
            AnimationLoader.Animations.AnimationFile = originalAnimationFile;

            foreach (var garment in AnimationLoader.Garments)
            {
                var defaultName = HasPath(garment.GarmentFile) ? garment.GarmentFile.GetFileNoExt() : "Garment";
                var path = FileDialogs.SaveFile("json", $"{defaultName}.json", "Save Garment As JSON");
                if (null == path)
                {
                    continue;
                }

                // Unlike the skeleton/animation above, Garment.GarmentFile's setter is private
                // (in the AnimationLib project), so it can't be snapshotted/restored here — this
                // call DOES permanently retarget each garment's save path to its JSON export path.
                garment.WriteJsonFile(new Filename { File = path });
            }
        }

        private void SaveGarments()
        {
            foreach (var garment in AnimationLoader.Garments)
            {
                if (HasPath(garment.GarmentFile))
                {
                    garment.WriteXml();
                }
                else
                {
                    var defaultName = string.IsNullOrEmpty(garment.Name) ? "Garment" : garment.Name;
                    var path = FileDialogs.SaveFile("xml", $"{defaultName}.xml", "Save Garment As");
                    if (null == path)
                    {
                        continue;
                    }

                    garment.WriteXmlFile(new Filename { File = path });
                }
            }
        }

        private static bool HasPath(Filename filename)
        {
            return null != filename && filename.HasFilename;
        }

        #endregion //Methods
    }
}
