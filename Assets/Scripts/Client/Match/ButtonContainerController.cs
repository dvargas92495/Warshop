using UnityEngine;
using UnityEngine.Events;

public class ButtonContainerController : Controller 
{
    public MenuItemController[] menuItems;

    private MenuItemController selectedMenuItem;

    public void EachMenuItem(UnityAction<MenuItemController> a)
    {
        Util.ToList(menuItems).ForEach(a);
    }

    public void EachMenuItemSet(UnityAction<MenuItemController> a)
    {
        EachMenuItem(m => m.SetCallback(() => a(m)));
    }

    public void SetButtons(bool b)
    {
        EachMenuItem(m => m.SetActive(b));
    }

    public void SetSelected(MenuItemController menuItem)
    {
        if (selectedMenuItem != null) selectedMenuItem.Activate();
        selectedMenuItem = menuItem;
    }

    public void ClearSprites()
    {
        EachMenuItem(m => m.ClearSprite());
    }

    public void AddRobotButton(MenuItemController robotButton)
    {
        robotButton.transform.localPosition = Vector3.right * (menuItems.Length%4 * 3 - 4.5f);
        menuItems = Util.Add(menuItems, robotButton);
    }

    public MenuItemController Get(int index)
    {
        return menuItems[index];
    }

    public MenuItemController GetByName(string name)
    {
        return Util.ToList(menuItems).Find(m => m.name.Equals(name));
    }
}
