using UnityEngine;
using UnityEngine.EventSystems;

namespace FunkyThursday.UI
{
    /// <summary>Forwards mouse hover and click on one MenuList row back to its list.</summary>
    public sealed class MenuListItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        MenuList _list;
        int _index;

        public void Bind(MenuList list, int index)
        {
            _list = list;
            _index = index;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_list != null) _list.PointerEntered(_index);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_list != null) _list.PointerClicked(_index);
        }
    }
}
