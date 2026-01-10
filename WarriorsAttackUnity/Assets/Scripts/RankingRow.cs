using UnityEngine;
using TMPro;

public class RankingRow : MonoBehaviour
{
    // Referencias a los textos de la interfaz
    public TMP_Text nameText;
    public TMP_Text scoreText;

    // Ponemos los datos en pantalla
    public void SetData(string name, string score)
    {
        nameText.text = name;
        scoreText.text = score;
    }
}