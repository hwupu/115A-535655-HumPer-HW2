using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// Onboarding slides before the practice: one full-canvas picture per slide (English and Chinese sets, 2000 x 1400 with an empty
    /// 200 px strip at the bottom) and a back / forward button in that strip. ExperimentFlow drives the navigation (<see cref="Onboarding"/>).
    /// </summary>
    public class OnboardingPanel : MonoBehaviour
    {
        public Image slideImage;
        public Button backButton, forwardButton;
        public Text backLabel, forwardLabel;
        public Sprite[] englishSlides = new Sprite[Onboarding.SlideCount];
        public Sprite[] chineseSlides = new Sprite[Onboarding.SlideCount];

        public void Show(int slide)
        {
            var set = Localization.Current == Language.ChineseTaiwan ? chineseSlides : englishSlides;
            slideImage.sprite = slide < set.Length ? set[slide] : null;
            var font = LocalizedText.FontFor(Localization.Current);
            backLabel.font = forwardLabel.font = font;
            backLabel.text = Onboarding.LeftLabel(slide);
            forwardLabel.text = Onboarding.RightLabel(slide);
            backButton.gameObject.SetActive(Onboarding.HasLeft(slide));
        }
    }
}
