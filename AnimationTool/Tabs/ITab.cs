using MenuBuddy;

namespace AnimationTool.Tabs
{
	interface ITab : IScreen
	{
		void Copy();

		void Paste();

		void PasteSpecial();

		void Mirror();

		void UnKey();
	}
}
