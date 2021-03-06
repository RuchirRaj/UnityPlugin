using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    [RequireComponent(typeof(UserView))]
    [RequireComponent(typeof(SlideToggle))]
    public class UserLoginSlideToggle : MonoBehaviour
    {
        private UserView view { get { return this.gameObject.GetComponent<UserView>(); } }
        private SlideToggle slider
        { get { return this.gameObject.GetComponent<SlideToggle>(); } }

        // ---------[ EVENTS ]---------
        public void OnUserClicked()
        {
            if(slider.isAnimating) { return; }

            if(view.data.profile.userId > 0)
            {
                slider.isOn = true;
            }
            else
            {
                view.NotifyClicked();
            }
        }

        public void OnLogoutClicked()
        {
            if(slider.isAnimating) { return; }

            view.NotifyClicked();
            slider.isOn = false;
        }
    }
}
