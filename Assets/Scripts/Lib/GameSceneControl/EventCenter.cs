
using UnityEngine.Events;

namespace AVG
{
    public static class EventCenter
    {
        public static UnityAction OnGameStateChanged;

        public static UnityAction OnInteractionStarted;

        public static UnityAction OnInteractionFinished;

        public static UnityAction OnSaveOperationFinished;

        public static UnityAction OnLoadOperationFinished;
    }

}
