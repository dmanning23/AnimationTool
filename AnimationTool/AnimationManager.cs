using AnimationLib;
using AnimationLoader;
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
            AnimationLoader.LoadContent();

            //AnimationLoader.LoadMai();
            //AnimationLoader.LoadMaiJson();

            //AnimationLoader.LoadKnight();
            //AnimationLoader.LoadDragon();

            //AnimationLoader.LoadWeddingDan();
            //AnimationLoader.LoadWeddingTabby();
            //AnimationLoader.LoadWeddingCarrie();
            //AnimationLoader.LoadWeddingBestMen();
            //AnimationLoader.LoadWeddingVenue();

            //AnimationLoader.LoadRoboJetValkyrie();

            //AnimationLoader.LoadBeachBlocksCharacter();

            //AnimationLoader.LoadGrimoireCharacter();
            //AnimationLoader.LoadGrimoireWarrior();
            //AnimationLoader.LoadGrimoireArcher();
            //AnimationLoader.LoadGrimoireBroom();
            AnimationLoader.LoadGrimoireDragon();
            //AnimationLoader.LoadGrimoireDragonFireball();
            //AnimationLoader.LoadGrimoireGoblin();
            //AnimationLoader.LoadGrimoireGoblinAx();
            //AnimationLoader.LoadGrimoireArcherArrow();
            //AnimationLoader.LoadGrimoireSkeleton();
            //AnimationLoader.LoadGrimoireMummy();
            //AnimationLoader.LoadGrimoireTree();
            //AnimationLoader.LoadGrimoireWolf();
            //AnimationLoader.LoadGrimoireWizard();
            //AnimationLoader.LoadGrimoirePumpkin();

            //AnimationLoader.LoadTassleCarrie();

            //AnimationLoader.LoadPajamoramaApple();

            var animation = Animations.Animations.First().Key;
            Animations.SetAnimation(animation, EPlayback.Forwards);

            SelectedAnimationContainer = Animations;
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

        public void Save()
        {
            AnimationLoader.Save();
        }

        public void SaveJson()
        {
            AnimationLoader.SaveJson();
        }

        #endregion //Methods
    }
}
