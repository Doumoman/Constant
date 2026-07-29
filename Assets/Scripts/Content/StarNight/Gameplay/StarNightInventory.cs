using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightInventory : MonoBehaviour
    {
        [SerializeField] private int generalCapacity = 6;
        [SerializeField] private int residentCapacity = 2;
        private readonly List<FableObject> generalItems = new();
        private readonly List<FableObject> residentItems = new();
        private int selectedIndex;

        public IReadOnlyList<FableObject> GeneralItems => generalItems;
        public IReadOnlyList<FableObject> ResidentItems => residentItems;
        public int SelectedIndex => selectedIndex;
        public event Action Changed;

        public bool TryStore(FableObject item)
        {
            if (item == null || !item.HasTrait(FableTraits.Carryable) || item.IsStored)
            {
                return false;
            }

            List<FableObject> destination = item.ItemKind == StarItemKind.ResidentProperty ? residentItems : generalItems;
            int capacity = item.ItemKind == StarItemKind.ResidentProperty ? residentCapacity : generalCapacity;
            if (destination.Count >= capacity)
            {
                StarNightHUD.Instance?.Toast(item.ItemKind == StarItemKind.ResidentProperty
                    ? "빌린 물건 칸이 가득 찼다."
                    : "가방이 가득 찼다.");
                return false;
            }

            destination.Add(item);
            item.SetStored(true);
            item.transform.SetParent(transform, true);
            StarNightRunState.Instance?.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ObjectTaken,
                actorId = "Player",
                targetId = item.ObjectId,
                detail = $"{item.DisplayName}을 가방에 넣었다",
                witnessed = item.ItemKind == StarItemKind.ResidentProperty
            });
            Changed?.Invoke();
            return true;
        }

        public void Select(int index)
        {
            if (generalItems.Count == 0)
            {
                selectedIndex = 0;
            }
            else
            {
                selectedIndex = Mathf.Clamp(index, 0, generalItems.Count - 1);
            }
            Changed?.Invoke();
        }

        public FableObject DropSelected(Vector3 position, Vector2 impulse)
        {
            if (generalItems.Count == 0)
            {
                return null;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, generalItems.Count - 1);
            FableObject item = generalItems[selectedIndex];
            generalItems.RemoveAt(selectedIndex);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, generalItems.Count - 1));
            Release(item, position, impulse);
            Changed?.Invoke();
            return item;
        }

        public FableObject TakeFirstMatching(Func<FableObject, bool> predicate)
        {
            int index = generalItems.FindIndex(item => item != null && predicate(item));
            if (index < 0)
            {
                return null;
            }

            FableObject item = generalItems[index];
            generalItems.RemoveAt(index);
            Changed?.Invoke();
            return item;
        }

        public FableObject PeekFirstMatching(Func<FableObject, bool> predicate)
        {
            return generalItems.Find(item => item != null && predicate(item));
        }

        private static void Release(FableObject item, Vector3 position, Vector2 impulse)
        {
            item.transform.SetParent(null, true);
            item.transform.position = position;
            item.SetStored(false);
            if (item.TryGetComponent(out Rigidbody2D body))
            {
                body.AddForce(impulse, ForceMode2D.Impulse);
            }

            StarNightRunState.Instance?.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.DroppedItem,
                actorId = "Player",
                targetId = item.ObjectId,
                detail = $"{item.DisplayName}을 세계에 남겨 두었다"
            });
        }
    }
}
