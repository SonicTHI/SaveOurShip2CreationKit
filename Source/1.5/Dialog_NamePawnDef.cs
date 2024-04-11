using RimWorld;
using Verse;

namespace SaveOurShip2
{
	public class Dialog_NamePawnDef : Dialog_RenameShip
	{
		private CompNameMe comp;

		public Dialog_NamePawnDef(CompNameMe comp)
		{
			this.comp = comp;
			curName = comp.pawnKindDef;
		}

		protected override void SetName(string name)
		{
			if (name == comp.pawnKindDef || string.IsNullOrEmpty(name))
				return;

			comp.pawnKindDef = name;
		}
	}
}