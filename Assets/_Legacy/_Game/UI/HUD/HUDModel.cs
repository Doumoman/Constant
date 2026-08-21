#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Interaction.HandSlot;
using StarNight.Stage.Data;
using UnityEngine;

namespace StarNight.UI.HUD
{
    public enum InputDisplayDevice
    {
        Keyboard,
        Gamepad,
    }

    public enum HUDVisibility
    {
        Hidden,
        Exploration,
        Dimmed,
        Boss,
    }

    public readonly struct HUDMapRoomModel
    {
        public HUDMapRoomModel(string roomId, Vector2 center, bool current, bool exit)
        {
            RoomId = roomId;
            Center = center;
            IsCurrent = current;
            IsExit = exit;
        }

        public string RoomId { get; }
        public Vector2 Center { get; }
        public bool IsCurrent { get; }
        public bool IsExit { get; }
    }

    public readonly struct HUDMapConnectionModel
    {
        public HUDMapConnectionModel(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; }
        public string To { get; }
    }

    public sealed class HUDModel
    {
        private readonly List<HUDMapRoomModel> rooms = new List<HUDMapRoomModel>();
        private readonly List<HUDMapConnectionModel> connections = new List<HUDMapConnectionModel>();
        private readonly List<EquipmentInventoryHudEntry> equipment = new List<EquipmentInventoryHudEntry>();
        private readonly ReadOnlyCollection<HUDMapRoomModel> readOnlyRooms;
        private readonly ReadOnlyCollection<HUDMapConnectionModel> readOnlyConnections;
        private readonly ReadOnlyCollection<EquipmentInventoryHudEntry> readOnlyEquipment;

        public HUDModel()
        {
            readOnlyRooms = rooms.AsReadOnly();
            readOnlyConnections = connections.AsReadOnly();
            readOnlyEquipment = equipment.AsReadOnly();
        }

        internal int revision;
        internal HUDVisibility visibility;
        internal int health = 4;
        internal int maxHealth = 4;
        internal bool lanternAvailable = true;
        internal int moneyWon;
        internal int moneyDelta;
        internal int ropes = 4;
        internal int bombs = 4;
        internal string handToolId = string.Empty;
        internal bool handSlotOccupied;
        internal string handDisplayName = string.Empty;
        internal Sprite handIcon;
        internal bool handResourceVisible;
        internal int handResourceCurrent;
        internal int handResourceMaximum;
        internal string handPrimaryActionLabel = string.Empty;
        internal BellPhase bellPhase;
        internal bool exitGuidanceValid;
        internal bool exitInCurrentRoom;
        internal Vector2Int exitDirection;
        internal bool exitDiscovered;
        internal bool showActionPrompt;
        internal string actionLabel = string.Empty;
        internal float actionProgress;
        internal InputDisplayDevice inputDevice;
        internal string primaryGlyph = "X";
        internal string downPrimaryGlyph = "↓+X";
        internal string mapGlyph = "TAB";
        internal bool mapOpen;
        internal string currentRoomId = string.Empty;
        internal string stageName = string.Empty;
        internal bool stageNameVisible;
        internal bool bossActive;
        internal int bossStarKnots = 3;
        internal float fadeOpacity;
        internal int mapVersion;
        internal bool maruChasing;
        internal Vector2Int maruApproachDirection;
        internal int maruRemainingSeconds;
        internal bool showMaruTimer;
        internal bool visualBellAlert;
        internal bool maruEscapeActive;
        internal float maruEscapeProgress;
        internal float maruEscapeRemainingSeconds;
        internal string equipmentFeedbackMessage = string.Empty;
        internal int equipmentFeedbackRevision;
        internal bool equipmentFeedbackVisible;

        public int Revision => revision;
        public HUDVisibility Visibility => visibility;
        public int Health => health;
        public int MaxHealth => maxHealth;
        public bool LanternAvailable => lanternAvailable;
        public int MoneyWon => moneyWon;
        public int MoneyDelta => moneyDelta;
        public int Ropes => ropes;
        public int Bombs => bombs;
        public string HandToolId => handToolId;
        public bool HandSlotOccupied => handSlotOccupied;
        public string HandDisplayName => handDisplayName;
        public Sprite HandIcon => handIcon;
        public bool HandResourceVisible => handResourceVisible;
        public int HandResourceCurrent => handResourceCurrent;
        public int HandResourceMaximum => handResourceMaximum;
        public string HandPrimaryActionLabel => handPrimaryActionLabel;
        public BellPhase BellPhase => bellPhase;
        public bool ExitGuidanceValid => exitGuidanceValid;
        public bool ExitInCurrentRoom => exitInCurrentRoom;
        public Vector2Int ExitDirection => exitDirection;
        public bool ExitDiscovered => exitDiscovered;
        public bool ShowActionPrompt => showActionPrompt;
        public string ActionLabel => actionLabel;
        public float ActionProgress => actionProgress;
        public InputDisplayDevice InputDevice => inputDevice;
        public string PrimaryGlyph => primaryGlyph;
        public string DownPrimaryGlyph => downPrimaryGlyph;
        public string MapGlyph => mapGlyph;
        public bool MapOpen => mapOpen;
        public string CurrentRoomId => currentRoomId;
        public string StageName => stageName;
        public bool StageNameVisible => stageNameVisible;
        public bool BossActive => bossActive;
        public int BossStarKnots => bossStarKnots;
        public float FadeOpacity => fadeOpacity;
        public int MapVersion => mapVersion;
        public bool MaruChasing => maruChasing;
        public Vector2Int MaruApproachDirection => maruApproachDirection;
        public int MaruRemainingSeconds => maruRemainingSeconds;
        public bool ShowMaruTimer => showMaruTimer;
        public bool VisualBellAlert => visualBellAlert;
        public bool MaruEscapeActive => maruEscapeActive;
        public float MaruEscapeProgress => maruEscapeProgress;
        public float MaruEscapeRemainingSeconds => maruEscapeRemainingSeconds;
        public string EquipmentFeedbackMessage => equipmentFeedbackMessage;
        public int EquipmentFeedbackRevision => equipmentFeedbackRevision;
        public bool EquipmentFeedbackVisible => equipmentFeedbackVisible;
        public IReadOnlyList<HUDMapRoomModel> Rooms => readOnlyRooms;
        public IReadOnlyList<HUDMapConnectionModel> Connections => readOnlyConnections;
        public IReadOnlyList<EquipmentInventoryHudEntry> Equipment => readOnlyEquipment;

        internal List<HUDMapRoomModel> MutableRooms => rooms;
        internal List<HUDMapConnectionModel> MutableConnections => connections;
        internal List<EquipmentInventoryHudEntry> MutableEquipment => equipment;
    }
}

#endif
