using UnityEngine;

namespace FunkyThursday.UI.Menus
{
    /// <summary>One full-screen page of the main menu. Builds its own layout from code.</summary>
    public abstract class MenuScreen : MonoBehaviour
    {
        protected MenuController Menu { get; private set; }
        protected RectTransform Root { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        public void Initialize(MenuController menu)
        {
            Menu = menu;
            Root = (RectTransform)transform;
            Build();
            gameObject.SetActive(false);
        }

        protected abstract void Build();

        public virtual void Open() => gameObject.SetActive(true);

        public virtual void Close() => gameObject.SetActive(false);
    }
}
