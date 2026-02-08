using System.Collections.Generic;
using TDS.View;

namespace TDS.Net
{
    public class PickupRegistry
    {
        public readonly List<PickupView> ServerPickups = new List<PickupView>();

        public void Register(PickupView view)
        {
            if (!ServerPickups.Contains(view))
                ServerPickups.Add(view);
        }

        public void Unregister(PickupView view)
        {
            ServerPickups.Remove(view);
        }
    }
}
