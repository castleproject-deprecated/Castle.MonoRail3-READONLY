namespace Castle.MonoRail.Mvc.ViewEngines
{
	public class PartialResolutionContext
	{
		public PartialResolutionContext(string areaName, string directory, string partialName, bool lookupSharedAreas)
		{
			AreaName = areaName;
			Directory = directory;
			PartialName = partialName;
			LookupSharedAreas = lookupSharedAreas;
		}

		public PartialResolutionContext(BaseMvcContext copy) :
			this(copy.AreaName, copy.ControllerName, copy.ActionName, true)
		{
		}

		public string AreaName { get; set; }

		public string Directory { get; set; }

		public string PartialName { get; set; }

		public bool LookupSharedAreas { get; set; }
	}
}