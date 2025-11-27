using AnimationLib;
using MatrixExtensions;
using Microsoft.Xna.Framework;

namespace AnimationTool.EditScreens
{
	public class EditRagdollScreen : BaseEditScreen
	{
		public EditRagdollScreen(AnimationManager animationManager, Bone bone, string name) : base(animationManager, bone, name)
		{
		}

		protected override void SetRotationHack(Vector2 pos)
		{
			if (RagdollType.Float != _bone.AnchorJoint.Data.RagdollType)
			{
				//only do the hack rotation if not a floating ragdoll
				base.SetRotationHack(pos);

				//don't rotate past the limits
				Bone parentBone = AnimationManager.SelectedAnimationContainer.Skeleton.RootBone.GetParentBone(_bone.Name);
				if (null != parentBone)
				{
					float firstLimit = _bone.AnchorJoint.FirstLimit;
					float secondLimit = _bone.AnchorJoint.SecondLimit;

					if (_bone.Flipped)
					{
						float temp = firstLimit;
						firstLimit = secondLimit * -1.0f;
						secondLimit = temp * -1.0f;
					}

					firstLimit += parentBone.Rotation;
					secondLimit += parentBone.Rotation;

					if (AnimationManager.HackRotation < firstLimit)
					{
						AnimationManager.HackRotation = firstLimit;
					}
					if (AnimationManager.HackRotation > secondLimit)
					{
						AnimationManager.HackRotation = secondLimit;
					}
				}
			}
			else
			{
				//if in float ragdoll mode, float around the edge of the radius
				var myMatrix = MatrixExt.Orientation(_bone.GetAngle(pos));
				Vector2 limit = new Vector2(_bone.AnchorJoint.Data.FloatRadius, 0.0f);
				AnimationManager.HackTranslation = myMatrix.Multiply(limit);
			}
		}
	}
}
