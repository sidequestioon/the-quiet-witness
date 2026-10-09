using QuietWitness.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuietWitness.UI
{
    // One small article on the front page. Clicking circles it in red;
    // if the article hides a clue, the clue is found.
    [RequireComponent(typeof(Button))]
    public class NewspaperArticleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [Tooltip("Red outline shown when the article is circled.")]
        [SerializeField] private GameObject circle;

        private NewspaperData.Article article;

        public void Setup(NewspaperData.Article source)
        {
            article = source;
            title.text = article.title;
            body.text = article.body;
            circle.SetActive(article.clue != null && GameState.Instance != null && GameState.Instance.HasClue(article.clue));

            var button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Circle);
        }

        private void Circle()
        {
            circle.SetActive(true);
            if (article.clue != null && GameState.Instance != null) GameState.Instance.FindClue(article.clue);
        }
    }
}
