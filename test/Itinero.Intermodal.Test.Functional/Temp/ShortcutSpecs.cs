using Itinero.Attributes;
using Itinero.LocalGeo;
using Itinero.Profiles;

namespace Itinero.Intermodal.Test.Functional.Temp
{
    /// <summary>
    /// Represents shortcut specifications.
    /// </summary>
    public class ShortcutSpecs
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the locations.
        /// </summary>
        public Coordinate[] Locations { get; set; }

        /// <summary>
        /// Gets or sets the location meta-data.
        /// </summary>
        public IAttributeCollection[] LocationsMeta { get; set; }

        /// <summary>
        /// The minimum travel time. Below this no shortcuts will be added.
        /// </summary>
        public int MinTravelTime { get; set; }

        /// <summary>
        /// Gets or sets the time to start/stop a shortcut. For bike sharing system a measure on the time it takes to rent/leave bikes.
        /// </summary>
        public float TransferTime { get; set; }

        /// <summary>
        /// Gets or sets the max shortcut duration.
        /// </summary>
        public float MaxShortcutDuration { get; set; }

        /// <summary>
        /// Gets or sets the shortcut profile.
        /// </summary>
        public Profile Profile { get; set; }

        /// <summary>
        /// Gets or sets the transition profiles, the profiles that can be transferred to/from.
        /// </summary>
        public Profile[] TransitionProfiles { get; set; }
    }
}