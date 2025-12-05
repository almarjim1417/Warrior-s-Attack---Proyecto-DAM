using UnityEngine;
using TMPro;

public class RankingRow : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text scoreText;

    public void SetData(string name, string score)
    {
        nameText.text = name;
        scoreText.text = score;
    }
}